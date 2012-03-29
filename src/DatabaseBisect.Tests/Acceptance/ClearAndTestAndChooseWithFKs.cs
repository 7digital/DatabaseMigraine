
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	[Ignore("WIP, doesn't work yet :(")]
	public class ClearAndTestAndChooseWithFKs : ClearAndTestBasic
	{
		protected override string TestDbName
		{
			get { return "fooForFk"; }
		}

		protected override Table AndTheTableIChooseIs(Database db)
		{
			return BisectOperations.ChooseTableToBisect(db);
		}
	}
}
