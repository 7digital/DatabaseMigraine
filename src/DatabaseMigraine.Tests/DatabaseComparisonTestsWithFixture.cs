using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DatabaseComparisonTestsWithFixture : DatabaseComparisonTests
	{
		[TestFixtureSetUp]
		public override void SetupConnections()
		{
			base.SetupConnections();
		}
	}
}
