using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class TableCopWithFixture : TableCop
	{
		[TestFixtureSetUp]
		public override void CreateDisposableDb()
		{
			base.CreateDisposableDb();
		}
	}
}
