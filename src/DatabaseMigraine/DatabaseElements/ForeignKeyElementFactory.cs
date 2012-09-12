using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class ForeignKeyElementFactory : IScriptableWrapperFactory<ForeignKeyElement>
	{
		public IEnumerable<ForeignKeyElement> Scan(Database db)
		{
			foreach (Table table in db.Tables)
			{
				if (table.IsSystemObject)
				{
					continue;
				}

				foreach (ForeignKey fk in table.ForeignKeys)
				{
					yield return new ForeignKeyElement(fk);
				}
			}
			yield break;
		}
	}
}