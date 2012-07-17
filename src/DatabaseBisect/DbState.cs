using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseMigraine.DatabaseElements;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public class DbState : Dictionary<string, int> //nameOfTable,numOfRows
	{
		private DbState(IDictionary<string,int> db) : base (db)
		{
		}

		public DbState(Database db)
		{
			db.Tables.Refresh();
			foreach (Table table in db.Tables)
			{
				var element = new TableElement(table);
				var countDataSet = db.ExecuteWithResults(String.Format("USE {0} SELECT COUNT(*) FROM {1}", db.Name, element.FullName));
				var count = (int)countDataSet.Tables[0].Rows[0][0];
				Add(element.FullName, count);
			}
		}

		public bool EqualsOriginal(DbState other)
		{
			var noBackUpTables = new DbState(this);
			var otherWithNoBackupTables = new DbState(other);

			foreach(var tableName in other.Keys)
			{
				if (BisectOperations.IsBackUpTable(tableName))
					otherWithNoBackupTables.Remove(tableName);
			}

			foreach (var tableName in this.Keys)
			{
				if (BisectOperations.IsBackUpTable(tableName))
					noBackUpTables.Remove(tableName);
			}

			return otherWithNoBackupTables.Equals(noBackUpTables);
		}

		public override bool Equals(object obj)
		{
			var otherState = obj as DbState;
			if (otherState == null)
			{
				return false;
			}

			if (otherState.Keys.Count != Keys.Count)
			{
				return false;
			}

			return otherState.Keys.All(otherKey => ContainsKey(otherKey) && this[otherKey] == otherState[otherKey]);
		}

		public override int GetHashCode()
		{
			return Keys.GetHashCode() ^ Values.GetHashCode();
		}
	}
}