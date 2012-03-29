using System;
using System.Text.RegularExpressions;

namespace Formatter
{
	//FIXME: don't know yet why this doesn't work
	class AlterTableHandler
	{
		private static readonly Regex _alterTableRegex = null;

		static AlterTableHandler () {
			const string tableName = @"([^\s]*)";
			_alterTableRegex = new Regex(String.Format(
				@"\s*ALTER\s+TABLE\s+({0})\s+(.*)GO",
				tableName));
		}

		internal static string Treat(string contents)
		{
			contents = _alterTableRegex.Replace(contents, Beautify);
			return contents;
		}

		static string Beautify(Match match)
		{
			string result = Environment.NewLine + "ALTER TABLE ";

			string tableName = match.Groups[1].Value;
			string alterations = match.Groups[2].Value;

			result += tableName + Environment.NewLine;
			result += "\t" + alterations + Environment.NewLine;

			result += "GO" + Environment.NewLine;

			return result;
		}
	}
}
