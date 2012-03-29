using System;
using System.Text.RegularExpressions;

namespace Formatter
{
	class WithHandler
	{
		private static readonly Regex _withRegex = null;

		static WithHandler () {
			const string withTerm = @"[^,\(\)]*";
			_withRegex = new Regex(String.Format(
				@"\n(\t+)(.*)\s+WITH\s*\(({0})\s*(,\s*{0})*\)",
				withTerm));
		}

		internal static string Treat(string contents)
		{
			//Formatter.DebugRegex(contents, _withRegex);

			contents = _withRegex.Replace(contents, Beautify);
			return contents;
		}

		static string Beautify(Match match)
		{
			string tabs = match.Groups[1].Value;
			string textBeforeWith = match.Groups[2].Value;
			string firstElement = match.Groups[3].Value;

			string result = Environment.NewLine + tabs + textBeforeWith;
			result += Environment.NewLine + tabs + "\t" + "WITH (";
			result += Environment.NewLine + tabs + "\t\t" + firstElement.Trim();

			CaptureCollection lastElements = match.Groups[4].Captures;
			for (int i = 0; i < lastElements.Count; i++)
			{
				result += Environment.NewLine + tabs + "\t\t" +
					lastElements[i].Value.Substring(1).Trim();
				if (i != lastElements.Count - 1)
					result += ",";
			}
			result += Environment.NewLine + tabs + "\t)";
			return result;
		}
	}
}
