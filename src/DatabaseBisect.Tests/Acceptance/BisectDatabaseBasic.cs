using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
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
			AndIPerformTheBisectDatabaseOperation(db);
			ThenMoreThanOneBisectTableOperationHasBeenPerformed(originalState, db);
		}

		private void ThenMoreThanOneBisectTableOperationHasBeenPerformed(DbState originalDbState, IDataBase db)
		{
			ThereIsMoreThanOneBackupTable(originalDbState, db);
		}

		private void ThereIsMoreThanOneBackupTable(DbState originalDbState, IDataBase db)
		{
			Assert.That(new DbState(db).Keys.Count, Is.GreaterThan(originalDbState.Keys.Count + 1));
		}

		private void AndIPerformTheBisectDatabaseOperation(IDataBase db)
		{
			new Executor(new Analyst(), new Bisector()).BisectDatabase(db, TestOperationThatSucceeds());
		}
	}
}
