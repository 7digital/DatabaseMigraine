using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DatabaseScripter
{
	public static class SqlStatementSeparator
	{
		public static IEnumerable<string> SeparateCreateTableStatements(string contents)
		{
			
			if (!contents.Contains("CREATE TABLE"))
			{
				throw new NotSupportedException("No create table statements");
			}
			const string anyButCommaOrParen = @"[^,\(\)]";
			const string anyButParen = @"[^\(\)]";
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

			var regex = new Regex(String.Format(
				@"\s*CREATE\s+TABLE\s+{0}\s*\(\s*({1})\s*(,\s*{1}\s*)*\)\s*",
				tableName, columnRegex));

			foreach (var match in regex.Matches(contents))
			{
				yield return match.ToString();
			}
		} 
	}
}