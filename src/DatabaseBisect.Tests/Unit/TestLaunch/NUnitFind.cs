
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Unit.TestLaunch
{
	[TestFixture]
	public class NUnitFind
	{
		[Test]
		public void NUnitIsLookedForInDefaultProgramFilesFolderByDefault ()
		{
			var nunitFinder = new NUnitFinder();
			Assert.That(nunitFinder.GetNUnitPossibleLocation().First(),
				Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
		}


	}

	public class NUnitFinder
	{
		public IEnumerable<string> GetNUnitPossibleLocation()
		{
			yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		}
	}
}
