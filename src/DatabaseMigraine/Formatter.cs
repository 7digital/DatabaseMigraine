using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DatabaseMigraine
{
	public class Formatter
	{
		// the reason to create this method in the API is that certain encodings are not
		// grep-able under certain tools that we use heavily, like MINGW32's grep in this case
		// (not sure if more tools are affected)
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
