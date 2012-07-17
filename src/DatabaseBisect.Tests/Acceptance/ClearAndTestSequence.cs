
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	public class ClearAndTestSequence : DbHelper
	{
		protected override string TestDbName
		{
			get { return "foo"; }
		}

		[Test]
		public void ChooseTableASecondTimeDoesntChooseSameTableAgain ()
		{
			var db = GivenADisposableDbCreatedForTesting();
			AndTheDbHasAtLeast2TablesAndSecondOneIsNotEmpty(db);
			var firstTableBisected = AndIPerformTheClearAndTestOperationWithATestThatFails(db);
			ThenTheTableChosenForSecondClearAndTestOperationIsNotTheSame(db, firstTableBisected);
		}

		private void ThenTheTableChosenForSecondClearAndTestOperationIsNotTheSame(Database db, Table firstTableBisected)
		{
			Assert.That(BisectOperations.ChooseTableToBisect(db), Is.Not.EqualTo(firstTableBisected));
		}

		private Table AndIPerformTheClearAndTestOperationWithATestThatFails(Database db)
		{
			var firstTableToBisect = BisectOperations.ChooseTableToBisect(db);
			BisectOperations.BisectTableOnce(db, firstTableToBisect, TestOperationThatFails());
			return firstTableToBisect;
		}
	}
}
