using System;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public interface IBisector
	{
		void BisectTableOnce(IDataBase db, Table table, Func<IDataBase, bool> verification);
	}
}