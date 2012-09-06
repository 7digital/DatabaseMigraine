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
				if (IsSystemObject(function))
					continue;

				yield return new FunctionElement(function);
			}
			yield break;
		}

		private static bool IsSystemObject(UserDefinedFunction function)
		{
			try
			{
				if (function.IsSystemObject)
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