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
				if (table.IsSystemObject) 
                {
					continue;
				}
				yield return new TableElement(table);
			}
		}
	}
}