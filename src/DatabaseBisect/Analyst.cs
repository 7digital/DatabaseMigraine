
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public class Analyst : IAnalyst
	{
		public Table ChooseTableToBisect(IDataBase db)
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
				return null;

			return db.Tables[highest.Value.Key];
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
