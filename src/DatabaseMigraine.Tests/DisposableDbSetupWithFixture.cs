using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DisposableDbSetupWithFixture : DisposableDbSetUp
	{
		[TestFixtureSetUp]
		public override void SetupDisposableDb()
		{
			base.SetupDisposableDb();
		}
	}
}