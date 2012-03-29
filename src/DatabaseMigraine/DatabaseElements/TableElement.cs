
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class TableElement : Base, IScriptableDatabaseElementWithName
	{
		public TableElement(Table table) : base (table, table)
		{
		}
	}
}
