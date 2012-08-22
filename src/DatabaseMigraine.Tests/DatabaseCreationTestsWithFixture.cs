using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DatabaseCreationTestsWithFixture : DatabaseCreationTests
	{
		[TestFixtureSetUp]
		public override void SetupConnections()
		{
			base.SetupConnections();
		}

		[TestFixtureTearDown]
		public override void KillDisposableDbs()
		{
			base.KillDisposableDbs();
		}
	}
}
