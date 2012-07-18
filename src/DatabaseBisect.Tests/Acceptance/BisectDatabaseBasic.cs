using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	[Ignore("Does not work yet, WIP")]
	public class BisectDatabaseBasic : DbHelper
	{
		protected override string TestDbName
		{
			get { return "foo"; }
		}

		[Test]
		public void WholeBisectDatabaseOperationAppearsToHaveWorked()
		{
			var db = GivenADisposableDbCreatedForTesting();
			var originalState = AndTheDbHasAtLeast2TablesAndSecondOneIsNotEmpty(db);
			AndIPerformTheBisectOperation(db);
			ThenThereAreAsManyBackUpTablesAsOriginalTables(originalState, db);
		}

		private void ThenThereAreAsManyBackUpTablesAsOriginalTables(DbState originalDbState, IDataBase db)
		{
			Assert.That(new DbState(db).Keys.Count, Is.EqualTo(originalDbState.Keys.Count * 2));
		}

		private void AndIPerformTheBisectOperation(IDataBase db)
		{
			new Executor(new Analyst(), new Bisector()).BisectDatabase(db);
		}
	}
}
