using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Formatter
{
	class IfExistsHandler
	{
		private static readonly Regex _ifExistsRegex = null;

		static IfExistsHandler () {
			_ifExistsRegex = new Regex(@"\s*IF\s+EXISTS\s*\(\s*(.*)\s*\)\s+(.*)\s+GO\s*");
		}

		internal static string Treat(string contents)
		{
			//Formatter.DebugRegex(contents, ifExistsRegex);

			contents = _ifExistsRegex.Replace(contents, Beautify);
			return contents;
		}

		static string Beautify(Match match)
		{
			string result = Environment.NewLine + "IF EXISTS(" + match.Groups[1].Value + ")";
			result += Environment.NewLine + "\t" + match.Groups[2].Value.Trim() + Environment.NewLine;
			result += "GO" + Environment.NewLine + Environment.NewLine;

			return result;
		}
	}
}
