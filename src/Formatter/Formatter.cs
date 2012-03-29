using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Formatter
{
	public class Formatter
	{
		static bool _debug = false;
		static internal bool Debug { get { return _debug;  } }

		private static readonly string[] _keywords = {
			"GO",
			"CREATE",
			"ALTER",
			"DROP",
			"SELECT",
			"FROM",
			"TABLE",
			"IF",
			"EXISTS",
			"ADD",
			"CONSTRAINT",
			"NOT",
			"NULL",
			"ON",
			"OFF",
			"IN",
			"OR",
			"AND",
			"LIKE",
			"WITH",
			"CLUSTERED",
			"DEFAULT",
			"FOR",
			"KEY",
			"WHERE",
			"PRIMARY",
			"FOREIGN",
			"NOLOCK",
			"REPLICATION",
			"INT",
			"BIT",

			//functions:
			"GETDATE",

			//types:
			"[DATETIME]",
			"[VARCHAR]",
			"[NVARCHAR]",
			"[INT]",
		};

		private static readonly string[] _connectors = {
			"(", ")", ",", "=", "[", "]"//TODO: remove square brackets
		};

		private static readonly string[][] _wrapperConnectors = {
			new []{ "'" }, new [] {"[", "]"}
		};

		static void Main(string[] args) {

			var processedArgs = new List<string>();
			foreach (string arg in args) {
				if (arg == "--debug")
					_debug = true;
				else
					processedArgs.Add (arg);
			}

			if (processedArgs.Count != 1)
			{
				Console.WriteLine("Usage: Formatter.exe myfile.sql");
				Console.WriteLine("Usage: Formatter.exe path/to/sql/files/");
				return;
			}

			string dir = processedArgs[0];
			string file = dir;
			if (!Directory.Exists(dir)) {
				dir = null;
			}

			if (dir == null && !File.Exists(file)) {
				Console.WriteLine("Directory or file not found: " + file);
				return;
			}

			if (dir == null)
				ConvertFile(file);
			else
				ConvertDir(dir);
		}

		// the reason to create this method in the API is that certain encodings are not
		// grep-able under certain tools that we use heavily, like MINGW32's grep in this case
		// (not sure if more tools are affected which I plan to use too, like NGrep or
		// MonoDevelop's grep library)
		public static bool IsEncodingGrepable(FileInfo file, out string charSet) {
			using (FileStream fs = File.OpenRead(file.FullName))
			{
				var cdet = new Ude.CharsetDetector();
				cdet.Feed(fs);
				cdet.DataEnd();

				charSet = cdet.Charset;

				if (cdet.Charset != null && cdet.Charset == "UTF-16LE")
				{
					return false;
				}
				return true;
			}
		}

		public static bool IsEncodingGrepable(FileInfo file)
		{
			string charSetVarIWontUse;
			return IsEncodingGrepable(file, out charSetVarIWontUse);
		}

		internal static void CheckEncoding(FileInfo file)
		{
			string charSet;
			if (!IsEncodingGrepable(file, out charSet))
			{
				throw new NotSupportedException(String.Format(
					"Encoding of file '{0}' not supported: {1}. (Path: {2})",
					file.Name, charSet, file.DirectoryName) +
					Environment.NewLine + "Run the Formatter on the file, or change its encoding manually.");
			}
		}

		private static int CompareFilesBySize(string fileA, string fileB) {
			long sizeA = new FileInfo(fileA).Length;
			long sizeB = new FileInfo(fileB).Length;
			if (sizeA == sizeB)
				return 0;
			if (sizeA > sizeB)
				return 1;
			return -1;
		}

		private static void ConvertDir(string dir) {
			var files = new List<string>(Directory.GetFileSystemEntries(dir, "*.sql"));

			//FIXME: when converting this solution to .NET 3.5, use this more efficient:
			//        var sort = from fn in fns
			//                   orderby new FileInfo(fn).Length descending
			//                   select fn;
			files.Sort(CompareFilesBySize);

			var notSupportedFiles = new List<string>();
			foreach (string filePath in files) {
				try {
					ConvertFile(filePath);
				} catch (NotSupportedException) {
					Console.Error.WriteLine("Error formatting the file." + Environment.NewLine);
					notSupportedFiles.Add(filePath);
				}
			}

			if (notSupportedFiles.Count > 0) {
				Console.WriteLine("The following files could not be formatted:");
				foreach (string file in notSupportedFiles)
					Console.WriteLine(file);
				Environment.Exit(-1);
			}
		}

		private static void ConvertFile(string filePath) {
			Console.WriteLine("Formatting file " + filePath);
			string originalContents = File.ReadAllText(filePath);
			string formattedContent = FormatContent(originalContents);
			File.WriteAllText(filePath, formattedContent);
		}

		private static string FormatContent(string originalContents) {
			string preformattedOriginal = PreFormat(originalContents);
			preformattedOriginal = IgnoreSubtleDifferences(preformattedOriginal);

			string formattedContents = Format(originalContents);
			string preformattedFormatted = PreFormat(formattedContents);

			if (preformattedOriginal != preformattedFormatted)
			{
				LogPreFormats(preformattedOriginal, preformattedFormatted);
				throw new NotSupportedException(
					"Not able to format this file. Please file a bug." + Environment.NewLine +
					"(For debugging, check the log subdirectory that was created and use the --debug flag .)");
			}

			return formattedContents;
		}

		public static bool IsFileFormatted (FileInfo file)
		{
			CheckEncoding(file);
			string originalContents = File.ReadAllText(file.FullName);
			string formattedContent = FormatContent(originalContents);
			return originalContents == formattedContent;
		}

		private static string IgnoreSubtleDifferences(string contents)
		{
			return contents.Replace(", )", ")");
		}

		private static void LogPreFormats(string original, string formatted) {
			string logDir = Path.Combine (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log");

			if (Directory.Exists(logDir)) {
				Directory.Delete(logDir, true);
			}
			Directory.CreateDirectory(logDir);

			string pathForOriginalContents = Path.Combine(logDir, "original.sql");
			string pathForFormattedContents = Path.Combine(logDir, "formatted.sql");

			File.WriteAllText(pathForOriginalContents, original);
			File.WriteAllText(pathForFormattedContents, formatted);
		}

		public static string Format (string contents) {
			contents = PreFormat(contents);

			contents = TreatPatterns(contents);

			contents = FinalTrim(contents);

			return contents;
		}

		private static string PreFormat(string contents) {
			contents = PutSpacesAroundConnectors(contents);

			contents = PutSpacesAroundWrapperConnectors(contents);

			contents = ReplaceEveryBlankWithASpace(contents);

			//to prevent Regexes not work because of EOFs, etc
			contents = Untrim(contents);

			contents = CapitalizeKeywords(contents);

			contents = NormalizeEndingGo(contents);

			return contents;
		}

		private static string FinalTrim(string contents) {
			return contents.Trim().
				Replace("( ", "(").
				Replace(" )", ")").
				Replace(" ,", ",").
				Replace("[ ", "[").
				Replace(" ]", "]").
				Replace(". [", ".[").
				Replace("] .", "].").
				Replace("' [", "'[").
				Replace("] '", "]'").

				//HACK!! this is because object_id (N'[dbo].[tblTrack]') is not the same as
				//                       object_id (N' [dbo].[tblTrack]')
				Replace("(N '", "(N'").
				Replace(" N '", " N'");
		}

		private static string TreatPatterns(string contents) {
			//always the first one! (other patterns depend on GO being put at the end)
			contents = TreatKeywordGo(contents);

			contents = CreateTableHandler.Treat(contents);

			contents = IfExistsHandler.Treat(contents);
			
			contents = AlterTableHandler.Treat (contents);

			contents = WithHandler.Treat(contents);

			return contents;
		}

		internal static void DebugRegex(string contents, Regex regex) {
			DebugRegex(contents, regex, null);
		}

		internal static void DebugRegex(string contents, Regex regex, MatchEvaluator eval)
		{
			MatchCollection matches = regex.Matches(contents);
			Console.WriteLine("matches:" + matches.Count);
			if (matches.Count > 0) {
				int matchCount = 0;
				
				
				foreach (Match match in matches)
				{
					int groupCount = 0;
					Console.WriteLine("<match n='{0}' value='{1}'>", matchCount, match.Value);
					foreach (Group g in match.Groups)
					{
						int capCount = 0;
						Console.WriteLine("\t<group n='{0}' value='{1}'>", groupCount, g.Value);
						foreach (Capture cap in g.Captures) {
							Console.WriteLine("\t\t<capture n='{0}' value='{1}' />", capCount, cap.Value);
							capCount++;
						}
						Console.WriteLine("\t</group>");
						groupCount++;
					}
					Console.WriteLine("</match>");
					matchCount++;
				}
			}

			if (eval != null) {
				contents = regex.Replace(contents, eval);
				Console.WriteLine("Result:" + contents);
			}
		}

		//TODO: convert to lambda when we use better .NET, this is not used by anyone else
		static string TreatWrappedConnector(Match match) {
			return Untrim(match.Value);
		}


		private static string TreatKeywordGo(string contents) {
			contents = contents.Replace(" GO ",
			                            Environment.NewLine + "GO" + 
			                            Environment.NewLine +
			                            Environment.NewLine);
			return contents;
		}

		private static string NormalizeEndingGo(string contents) {
			if (!contents.Trim().EndsWith("GO"))
				contents = contents.TrimEnd() + " GO ";
			return contents;
		}

		private static string CapitalizeKeywords(string contents) {
			foreach (string keyword in _keywords) {
				string escapedKeyword = keyword.ToLower().Replace("[", "\\[").Replace("]", "\\]");

				var keywordRegex = new Regex(Untrim(escapedKeyword), RegexOptions.IgnoreCase);
				contents = keywordRegex.Replace(contents, Untrim(keyword.ToUpper()));
			}
			return contents;
		}

		static string Untrim(string inStr) {
			return " " + inStr.Trim() + " ";
		}

		internal static string TrimCommas(string inStr) {
			inStr = inStr.Trim();
			if (inStr.StartsWith(","))
				inStr = inStr.Substring(1);
			if (inStr.EndsWith(","))
				inStr = inStr.Substring(0, inStr.Length - 1);
			return inStr.Trim();
		}

		static string PutSpacesAroundConnectors(string inStr) {
			foreach (string connector in _connectors) {
				inStr = inStr.Replace(connector, Untrim(connector));
			}
			return inStr;
		}

		static string PutSpacesAroundWrapperConnectors(string inStr)
		{
			foreach (string[] wrapperConnectors in _wrapperConnectors)
			{
				if (wrapperConnectors.Length == 2) {
					inStr = inStr.Replace(wrapperConnectors[0], " " + wrapperConnectors[0]);
					inStr = inStr.Replace(wrapperConnectors[1], wrapperConnectors[1] + " ");
				} else if (wrapperConnectors.Length == 1) {
					string connector = wrapperConnectors[0];
					string allExceptConnector = String.Format(@"[^{0}]*", connector);
					var individualWrappedConnector = 
						new Regex(String.Format(@"{0}{1}{0}", connector, allExceptConnector));
					//DebugRegex(inStr, individualWrappedConnector, TreatWrappedConnector);
					inStr = individualWrappedConnector.Replace(inStr, TreatWrappedConnector);
				} else {
					throw new NotSupportedException("A wrapped connector collection can only have 1 or 2 items");
				}
			}
			return inStr;
		}

		public static string ReplaceEveryBlankWithASpace(string inStr)
		{
			var r = new Regex(@"\s+");
			return r.Replace(inStr, " ");
		}

		public static string ContractEols(string inStr)
		{
			const string winEolRegexPattern = "\r\n";
			string unixEolRegexReplace = "\n";

			inStr = new Regex(winEolRegexPattern).Replace(inStr, unixEolRegexReplace);

			const string regexPattern = "{0}{0}[{0}]+";
			const string regexReplace = "{0}{0}";

			string unixEolRegexPattern = String.Format(regexPattern, unixEolRegexReplace);
			unixEolRegexReplace = String.Format(regexReplace, unixEolRegexReplace);

			inStr = new Regex(unixEolRegexPattern).Replace(inStr, unixEolRegexReplace);

			//nuke rubbish(blank elements) between EOLs
			inStr = new Regex("\n\\s+\n").Replace(inStr, "\n\n");

			return inStr;
		}
	}
}
