
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	public class ClearAndTestBasic : DbHelper
	{

		[Test]
		public void BisectRevertsIfVerificationFails ()
		{
			var db = GivenADisposableDbCreatedForTesting();
			DbState previousDbState = AndTheDbHasAtLeast2TablesAndSecondOneIsNotEmpty(db);
			WhenIPerformTheClearAndTestOperationWithATestThatFails(db, AndTheTableIChooseIs(db));
			ThenTheClearIsRevertedSoTheDbIsInTheSameState(db, previousDbState);
		}

		[Test]
		public void BisectMarksTableForDeletionIfVerificationPasses()
		{
			var db = GivenADisposableDbCreatedForTesting();
			DbState previousDbState = AndTheDbHasAtLeast2TablesAndSecondOneIsNotEmpty(db);
			WhenIPerformTheClearAndTestOperationWithATestThatPasses(db, AndTheTableIChooseIs(db));
			ThenOneTableIsClearedAndABackupOfItIsDone(db, previousDbState);
		}

		protected virtual Table AndTheTableIChooseIs(Database db)
		{
			return db.Tables[1];
		}

	}
}