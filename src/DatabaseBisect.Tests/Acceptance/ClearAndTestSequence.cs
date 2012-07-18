
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

		private void ThenTheTableChosenForSecondClearAndTestOperationIsNotTheSame(IDataBase db, Table firstTableBisected)
		{
			Assert.That(Analyst.ChooseTableToBisect(db), Is.Not.EqualTo(firstTableBisected));
		}

		private Table AndIPerformTheClearAndTestOperationWithATestThatFails(IDataBase db)
		{
			var firstTableToBisect = Analyst.ChooseTableToBisect(db);
			Bisector.BisectTableOnce(db, firstTableToBisect, TestOperationThatFails());
			return firstTableToBisect;
		}
	}
}
