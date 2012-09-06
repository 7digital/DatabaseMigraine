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
				if (IsSystemObject(trigger))
					continue;

				yield return new DatabaseTriggerElement(trigger);
			}
			yield break;
		}

		private static bool IsSystemObject(DatabaseDdlTrigger trigger)
		{
			try
			{
				if (trigger.IsSystemObject)
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