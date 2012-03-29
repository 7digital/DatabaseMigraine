using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class FunctionElement : Base, IScriptableDatabaseElementWithName
	{
		internal FunctionElement(UserDefinedFunction function) : base(function, function)
		{
		}
	}
}
