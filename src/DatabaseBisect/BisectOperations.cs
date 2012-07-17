using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseMigraine.DatabaseElements;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public static class BisectOperations
	{
		public static void BisectTableOnce(Database db, Table table, Func<bool> verification)
		{
			string backupTableName = CreateBackupTable(db, table);

			db.Tables.Refresh();
			Table backupTable = db.Tables[backupTableName];

			MoveData(db, table, backupTable);

			if (!verification.Invoke())
			{
				MoveData(db, backupTable, table);
				backupTable.Drop();
			}
		}

		public static Table ChooseTableToBisect (Database db)
		{
			var state = new DbState(db);

			var tablesWithForeignKeysLinkingToThem = new HashSet<string>();
			foreach (Table table in db.Tables)
			{
				foreach (ForeignKey fk in table.ForeignKeys)
				{
					tablesWithForeignKeysLinkingToThem.Add(fk.ReferencedTable);
				}
			}


			KeyValuePair<string, int>? highest = null;
			foreach (var tableToRowCount in state)
			{
				if (tableToRowCount.Value > 0)
				{
					if ((highest == null ||
						 highest.Value.Value < tableToRowCount.Value) && 
						!tablesWithForeignKeysLinkingToThem.Contains(tableToRowCount.Key))
					{
						highest = tableToRowCount;
					}
				}
			}
			if (highest == null)
				throw new ArgumentException("This DB doesn't need to be bisected! All tables are empty.", "db");

			return db.Tables[highest.Value.Key];
		}

		static void MoveData(Database db, Table source, Table destination)
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
			db.ExecuteNonQuery(String.Format("DELETE FROM {0}", source.Name));
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

		private static string CreateBackupTable(Database db, Table table)
		{
			var script = Base.JoinScriptFragments(table.Script(new ScriptingOptions {DriPrimaryKey = false, Statistics = false}));
			var scriptForBackupTable = TransformCreationScriptForBackup(script, table.Name);
			db.ExecuteNonQuery(scriptForBackupTable);
			return GetBackupTableName(table.Name);
		}

		public static string TransformCreationScriptForBackup(string afterScript, string tableName)
		{
			const string anyButCommaOrParenRegex = @"[^,\(\)]";
			string tableNameRegex = String.Format("({0}*)", anyButCommaOrParenRegex);
			var createTableRegex = new Regex(String.Format(@"CREATE\s+TABLE\s+{0}\s*\(", tableNameRegex), RegexOptions.IgnoreCase);
			return createTableRegex.Replace(afterScript, "CREATE TABLE " + GetBackupTableName (tableName) + "(");
		}

		public const string BackupSuffix = "_Backup";

		public static bool IsBackUpTable(string tableName)
		{
			return tableName.EndsWith(BackupSuffix);
		}

		public static string GetOriginalTable(string tableName)
		{
			if (!IsBackUpTable(tableName))
			{
				throw new InvalidArgumentException("Table is not a backup table: " + tableName);
			}
			return tableName.Substring(0, tableName.IndexOf(BackupSuffix));
		}

		public static string GetBackupTableName(string tableName)
		{
			if (IsBackUpTable(tableName))
			{
				throw new InvalidArgumentException("Table is already a backup!: " + tableName);
			}
			return tableName + BackupSuffix;
		}
	}
}