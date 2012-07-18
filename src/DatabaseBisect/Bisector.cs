using System;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseMigraine.DatabaseElements;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public class Bisector
	{
		public static void BisectTableOnce(IDataBase db, Table table, Func<IDataBase,bool> verification)
		{
			string backupTableName = CreateBackupTable(db, table);

			db.Tables.Refresh();
			Table backupTable = db.Tables[backupTableName];

			MoveData(db, table, backupTable);

			if (!verification.Invoke(db))
			{
				DeleteContentsOfTable(db, table);
				MoveData(db, backupTable, table);

			} else {
				DeleteContentsOfTable(db, table);
			}
		}

		static void MoveData(IDataBase db, Table source, Table destination)
		{
			if (!AreTablesEquivalent(source, destination))
			{
				throw new InvalidOperationException("Both tables need to have same set of columns to move data between them");
			}
			string commaSeparatedColumns = String.Join(",", (from Column column in source.Columns select column.Name).ToArray());

			const string copyToTableSql = @"
				SET IDENTITY_INSERT {0} ON
				INSERT INTO {0}({1}) SELECT {1} FROM {2}
				SET IDENTITY_INSERT {0} OFF";
			db.ExecuteNonQuery(String.Format(copyToTableSql, destination, commaSeparatedColumns, source.Name));
			DeleteContentsOfTable(db, source);
		}

		private static void DeleteContentsOfTable(IDataBase db, Table table)
		{
			db.ExecuteNonQuery(String.Format("DELETE FROM {0}", table.Name));
		}

		private static bool AreTablesEquivalent(Table source, Table destination)
		{
			var numberOfColumns = source.Columns.Count;
			if (numberOfColumns != destination.Columns.Count) {
				return false;
			}

			for (int i = 0; i < numberOfColumns; i++ )
			{
				if (source.Columns[i].Name != destination.Columns[i].Name)
				{
					return false;
				}

				if (source.Columns[i].DataType.SqlDataType != destination.Columns[i].DataType.SqlDataType)
				{
					return false;
				}
			}
			return true;
		}

		private static string CreateBackupTable(IDataBase db, Table table)
		{
			var script = Base.JoinScriptFragments(table.Script(new ScriptingOptions {DriPrimaryKey = false, Statistics = false}));
			var scriptForBackupTable = TransformCreationScriptForBackup(script, table.Name);
			db.ExecuteNonQuery(scriptForBackupTable);
			return Analyst.GetBackupTableName(table.Name);
		}

		public static string TransformCreationScriptForBackup(string afterScript, string tableName)
		{
			const string anyButCommaOrParenRegex = @"[^,\(\)]";
			string tableNameRegex = String.Format("({0}*)", anyButCommaOrParenRegex);
			var createTableRegex = new Regex(String.Format(@"CREATE\s+TABLE\s+{0}\s*\(", tableNameRegex), RegexOptions.IgnoreCase);
			return createTableRegex.Replace(afterScript, "CREATE TABLE " + Analyst.GetBackupTableName (tableName) + "(");
		}
	}
}