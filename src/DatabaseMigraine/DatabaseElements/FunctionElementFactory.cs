using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class FunctionElementFactory : IScriptableWrapperFactory<FunctionElement>
	{
		public IEnumerable<FunctionElement> Scan(Database db)
		{
			foreach (UserDefinedFunction function in db.UserDefinedFunctions)
			{
				if (function.IsSystemObject)
				{
					continue;
				}
				yield return new FunctionElement(function);
			}
			yield break;
		}
	}
}