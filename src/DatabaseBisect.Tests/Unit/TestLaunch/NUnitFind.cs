
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
			var nunitFinder = new NUnitFinder(new FakeEverythingExists());
			var firstPossibleLocation = nunitFinder.GetNUnitPossibleLocations().FirstOrDefault();
			Assert.That(firstPossibleLocation, Is.Not.Null);
			Assert.That(firstPossibleLocation, Is.EqualTo(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
			Assert.That(Directory.Exists(firstPossibleLocation), Is.True);
		}

		class FakeNothingExists : IDirectory
		{
			public bool Exists(string path)
			{
				return false;
			}
		}


		[Test]
		public void AllPossibleNUnitLocationsMustExist()
		{
			var nunitFinder = new NUnitFinder(new FakeNothingExists());
			var nunitPossibleLocations = nunitFinder.GetNUnitPossibleLocations();
			Assert.That(nunitPossibleLocations.Count(), Is.EqualTo(0));
		}


		class FakeEverythingExists : IDirectory
		{
			public bool Exists(string path)
			{
				return true;
			}
		}

		[Test]
		public void PossibleNUnitLocationsAfterDefaultAreAllKnownArchs()
		{
			var nunitFinder = new NUnitFinder(new FakeEverythingExists());
			var nunitPossibleLocations = new List<string> (nunitFinder.GetNUnitPossibleLocations());
			Assert.That(nunitPossibleLocations.Count(), Is.GreaterThan(1));
			var nunitPossibleLocationsAfterDefault = RemoveFirstElement(nunitPossibleLocations);

			var possibleArchs = new List<string> { "x86", "x64" };
			Assert.That(nunitPossibleLocationsAfterDefault.Count(), Is.EqualTo(possibleArchs.Count));
			var expectedPossibleLocationsAfterDefault = new List<string>();
			var defaultProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			foreach(var arch in possibleArchs)
			{
				expectedPossibleLocationsAfterDefault.Add(String.Format("{0} ({1})", defaultProgramFiles, arch));
			}

			for(int i = 0; i < possibleArchs.Count; i++)
			{
				var expected = expectedPossibleLocationsAfterDefault[i];
				Assert.That(nunitPossibleLocationsAfterDefault.ElementAt(i), Is.EqualTo(expected),
				            String.Format("Incorrect possible nunit location at iteration number {0}", i + 1));
			}
		}

		private List<string> RemoveFirstElement(List<string> nunitPossibleLocations)
		{
			var newList = new List<string>(nunitPossibleLocations);
			newList.Remove(nunitPossibleLocations[0]);
			return newList;
		}
	}

	public interface IDirectory
	{
		bool Exists(string path);
	}

	public class NUnitFinder
	{
		private readonly IDirectory _directoryService;

		public NUnitFinder (IDirectory directoryService)
		{
			_directoryService = directoryService;
		}

		public IEnumerable<string> GetNUnitPossibleLocations()
		{
			var defaultProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (_directoryService.Exists(defaultProgramFiles))
				yield return defaultProgramFiles;

			var alternative = defaultProgramFiles + " (x86)";
			if (_directoryService.Exists(alternative))
				yield return alternative;

			alternative = defaultProgramFiles + " (x64)";
			if (_directoryService.Exists(alternative))
				yield return alternative;
		}
	}
}
