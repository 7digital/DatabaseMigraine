using System;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public class Executor
	{
		private readonly IAnalyst _analyst;
		private readonly IBisector _bisector;

		public Executor(IAnalyst analyst, IBisector bisector)
		{
			_analyst = analyst;
			_bisector = bisector;
		}

		public void BisectDatabase(IDataBase db, Func<IDataBase, bool> testOperation)
		{
			Table tableToBisect;
			while ((tableToBisect = _analyst.ChooseTableToBisect(db)) != null)
			{
				_bisector.BisectTableOnce(db, tableToBisect, testOperation);
			}
		}
	}
}
