using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class DatabaseTriggerElementFactory : IScriptableWrapperFactory<DatabaseTriggerElement>
	{
		public IEnumerable<DatabaseTriggerElement> Scan(Database db)
		{
			foreach (DatabaseDdlTrigger trigger in db.Triggers)
			{
				if (trigger.IsSystemObject)
				{
					continue;
				}

				yield return new DatabaseTriggerElement(trigger);
			}
		}
	}
}