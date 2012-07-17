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
		public static void BisectTableOnce(Database db, Table table, Func<Database,bool> verification)
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

		public static Table ChooseTableToBisect (Database db)
		{
			var state = new DbState(db);

			var candidateTables = new HashSet<string>(state.Keys);

			foreach (Table table in db.Tables)
			{
				foreach (ForeignKey fk in table.ForeignKeys)
				{
					candidateTables.Remove(fk.ReferencedTable);
				}

				if (IsBackUpTable(table.Name) || state.Keys.Contains(GetBackupTableName(table.Name)))
				{
					candidateTables.Remove(table.Name);
				}
			}


			KeyValuePair<string, int>? highest = null;
			foreach (var table in candidateTables)
			{
				var rowCount = state[table];
				if (state[table] > 0)
				{
					if (highest == null ||
						highest.Value.Value < rowCount)
					{
						highest = new KeyValuePair<string, int>(table, rowCount);
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
			DeleteContentsOfTable(db, source);
		}

		private static void DeleteContentsOfTable(Database db, Table table)
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

		//FIXME: maybe I should use a different subschema?
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