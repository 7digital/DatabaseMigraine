using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DatabaseScripter
{
	public static class CreateTableWriter
	{
		public static void SeparateCreateTableStatementsToSeparateFiles(string sql)
		{
			var createTableStatements = SqlStatementSeparator.SeparateCreateTableStatements(sql).ToList();

			foreach (var tableStatement in createTableStatements)
			{
				var tableName = getTableNameOfCreateTableStatement(tableStatement);
				File.WriteAllText(tableName + ".sql", tableStatement);
			}
		}

		private static string getTableNameOfCreateTableStatement(string tableStatement)
		{
			var regex = new Regex(@"CREATE\s+TABLE\s+\[?\w+\]?\.?\[?(\w+)\]?");
			var groups = regex.Match(tableStatement).Groups;
			var match = groups[1].Value;
			return match;
		}
	}
}