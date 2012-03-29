
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class StoredProcedureElement : Base, IScriptableDatabaseElementWithName
	{
		internal StoredProcedureElement(StoredProcedure procedure) : base(procedure, procedure)
		{
		}
	}
}
