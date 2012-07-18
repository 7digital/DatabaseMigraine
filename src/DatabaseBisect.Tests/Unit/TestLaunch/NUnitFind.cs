
using System;
using System.Collections.Generic;
using System.IO;
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
			var nunitFinder = new NUnitFinder(null);
			var firstPossibleLocation = nunitFinder.GetNUnitPossibleLocations().First();
			Assert.That(firstPossibleLocation, Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
			Assert.That(Directory.Exists(firstPossibleLocation));
		}

		class FakeNothingExists : IDirectory
		{
			public bool Exists(string path)
			{
				return false;
			}
		}


		[Test]
		public void NUnitIsLookedForInThreePossibleProgramFiles()
		{
			var nunitFinder = new NUnitFinder(new FakeNothingExists());
			var nunitPossibleLocations = nunitFinder.GetNUnitPossibleLocations();
			Assert.That(nunitPossibleLocations.Count(), Is.EqualTo(1));
		}
	}

	public interface IDirectory
	{
		bool Exists(string path);
	}

	public class NUnitFinder
	{
		public NUnitFinder (IDirectory directoryService)
		{
			
		}
		public IEnumerable<string> GetNUnitPossibleLocations()
		{
			yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		}
	}
}
