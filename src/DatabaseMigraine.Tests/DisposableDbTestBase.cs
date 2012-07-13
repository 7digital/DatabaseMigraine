using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DisposableDbTestBase : DisposableDbSetUp
	{
		[TestFixtureSetUp]
		public override void SetupDisposableDb()
		{
			base.SetupDisposableDb();
		}
	}
}