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
				if (TableElementFactory.IsSystemObject(table))
					continue;

				foreach (Trigger trigger in table.Triggers)
				{
					if (IsSystemObject(trigger))
						continue;

					yield return new TableTriggerElement(trigger);
				}
			}
			yield break;
		}

		internal static bool IsSystemObject(Trigger trigger)
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