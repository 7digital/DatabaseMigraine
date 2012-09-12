using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class TableTriggerElementFactory : IScriptableWrapperFactory<TableTriggerElement>
	{
		public IEnumerable<TableTriggerElement> Scan(Database db)
		{
			foreach (Table table in db.Tables)
			{
				if (table.IsSystemObject)
				{
					continue;
				}

				foreach (Trigger trigger in table.Triggers)
				{
					if (trigger.IsSystemObject)
					{
						continue;
					}
					yield return new TableTriggerElement(trigger);
				}
			}
			yield break;
		}
	}
}