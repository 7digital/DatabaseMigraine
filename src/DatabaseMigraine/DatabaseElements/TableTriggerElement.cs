
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class TableTriggerElement : Base, IScriptableDatabaseElementWithName
	{
		private readonly Trigger _trigger;

		internal TableTriggerElement(Trigger trigger) : base(trigger, trigger)
		{
			_trigger = trigger;
		}

		public override string FullName
		{
			get { return ((Table)_trigger.Parent).Name + ParentToChildrenSeparatorInFullName + Name; }
		}
	}
}
