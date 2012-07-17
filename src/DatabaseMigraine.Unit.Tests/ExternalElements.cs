using NUnit.Framework;
using DatabaseMigraine.Managers;
using System.Collections.Generic;

namespace DatabaseMigraine.Unit.Tests
{
	[TestFixture]
	public class ExternalElements
	{
		[Test]
		public void ExternalElementsContents()
		{
			var externalElementManagers = ChiefExecutive.GetExposedElementManagers();

			var expectedExternalElementManagers = new HashSet<DbScriptFolderManager> {
				TableManager.Instance,
				ViewManager.Instance,
				FunctionManager.Instance,
				StoredProcedureManager.Instance,
			};

			Assert.That(externalElementManagers.Count, Is.EqualTo(expectedExternalElementManagers.Count));

			foreach (var manager in expectedExternalElementManagers)
			{
				Assert.That(externalElementManagers, Contains.Item(manager));
			}
		}
	}
}

