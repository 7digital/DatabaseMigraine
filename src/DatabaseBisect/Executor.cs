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

		public void BisectDatabase(IDataBase db)
		{
			var tableToBisect = _analyst.ChooseTableToBisect(null);
			if (tableToBisect != null) {
				_bisector.BisectTableOnce(null, tableToBisect, null);
			}
		}
	}
}
