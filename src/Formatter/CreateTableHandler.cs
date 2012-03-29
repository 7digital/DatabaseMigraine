
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Formatter
{
	class CreateTableHandler
	{
		private static readonly Regex _createTableRegex = null;

		static CreateTableHandler () {
			const string anyButCommaOrParen = @"[^,\(\)]";
			const string anyButParen = @"[^\(\)]";

			//  "... varchar (200) not null, ..." , "not null identity ( 1 , 1 ), ..."
			// to catch from ^     to     ^ ,                 and from ^  to   ^
			string subtype = String.Format(@"\(\s*{0}+\s*,*\s*{0}*\s*\){0}*", anyButCommaOrParen);

			const string getDate = @"GETDATE\s*\(\s*\)";
			const string anySqlString = "'.*'";
			const string anySqlNumber = @"\(\s*[0-9]+\s*\)";
			const string possibleDefaultValues = "(" + anySqlString + "|" +
													   getDate + "|" +
													   anySqlNumber + ")";
			string defaultClause = String.Format(@"(\s*DEFAULT\s*\(\s*{0}\s*\))?", possibleDefaultValues);

			const string constraint = @"(\s+CONSTRAINT.*)?";
			string with = String.Format(@"(\s*WITH\s*\({0}*\)\s*{0}*)?", anyButParen);
			string type = String.Format(@"({0}*({1})?)?", anyButCommaOrParen, subtype);
			string columnRegex = String.Format(@"{0}{1}{2}{3}",
				type, constraint, with, defaultClause);
			string tableName = String.Format("({0}*)", anyButCommaOrParen);

			_createTableRegex = new Regex(String.Format(
				@"\s*CREATE\s+TABLE\s+{0}\s*\(\s*({1})\s*(,\s*{1}\s*)*\)\s*",
				tableName, columnRegex));
		}

		internal static string Treat(string contents)
		{
			if (Formatter.Debug)
				Formatter.DebugRegex(contents, _createTableRegex, Beautify);

			contents = _createTableRegex.Replace(contents, Beautify);
			return contents;
		}

		static string Beautify(Match match)
		{
			string result = Environment.NewLine + Environment.NewLine + "CREATE TABLE ";
			string tableName = match.Groups[1].Value;
			string firstColumn = match.Groups[2].Value;

			var columns = new List<string> { firstColumn };
			for (int i = 3; i < match.Groups.Count; i++)
			{
				foreach (Capture cap in match.Groups[i].Captures)
				{
					bool alreadyAdded = false;
					foreach (string column in columns.ToArray())
					{
						if (Formatter.TrimCommas(column) == Formatter.TrimCommas(cap.Value)
							|| Formatter.TrimCommas(column).Contains(Formatter.TrimCommas(cap.Value)))
							alreadyAdded = true;
					}
					if (!alreadyAdded)
						columns.Add(cap.Value);
				}
			}
			columns.Remove(firstColumn);

			result += tableName + "(" + Environment.NewLine;
			result += "\t" + firstColumn + "," + Environment.NewLine;

			for (int i = 0; i < columns.Count; i++)
			{
				string capture = Formatter.TrimCommas(columns[i]);
				result += "\t" + capture;
				if (i != columns.Count - 1)
					result += "," + Environment.NewLine;
			}

			result += Environment.NewLine + ")" + Environment.NewLine;

			return result;
		}
	}
}
