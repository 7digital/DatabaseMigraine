using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class DatabaseTriggerElement : Base, IScriptableDatabaseElementWithName
	{
		internal DatabaseTriggerElement(DatabaseDdlTrigger trigger)
			: base(trigger, trigger)
		{
		}
	}
}