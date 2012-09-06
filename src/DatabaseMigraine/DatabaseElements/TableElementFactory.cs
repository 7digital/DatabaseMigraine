using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class TableElementFactory : IScriptableWrapperFactory<TableElement>
	{
		public IEnumerable<TableElement> Scan(Database db)
		{
			foreach(Table table in db.Tables)
			{
				if (IsSystemObject (table))
					continue;

				yield return new TableElement(table);
			}
			yield break;
		}

		internal static bool IsSystemObject(Table table)
		{
			try
			{
				if (table.IsSystemObject)
				{
					return true;
				}
			}
			catch (UnknownPropertyException)
			{
			}
			return false;
		}
	}
}