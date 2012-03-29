
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public interface IScriptableWrapperFactory<T>
	{
		IEnumerable<T> Scan(Database db);
	}
}
