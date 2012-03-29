
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class ViewElement : Base, IScriptableDatabaseElementWithName
	{
		public ViewElement(View view) : base(view, view)
		{
		}
	}
}
