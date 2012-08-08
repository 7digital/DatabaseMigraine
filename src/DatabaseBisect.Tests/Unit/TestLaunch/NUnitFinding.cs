
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Unit.TestLaunch
{
	[TestFixture]
	public class NUnitFinding
	{
		class SomeProgramFilesFinder : IProgramFilesFinder
		{
			internal static string programFiles = "C:\\Some Wierd Program Files Dir";
			public IEnumerable<string> GetPossibleLocations()
			{
				yield return programFiles;
				yield break;
			}
		}

		class SomeNUnitDirsExist : IDirectory
		{
			public bool Exists(string path)
			{
				throw new NotImplementedException(); //doesn't apply for this test
			}

			internal static string programFiles = "C:\\Program Files";
			internal static string Nunit24 = programFiles + "\\NUnit 2.4.10";
			internal static string Nunit25 = programFiles + "\\NUnit 2.5.10";

			public string[] GetFileSystemEntries(string path)
			{
				if (programFiles == path)
					return new[]
						{
							Nunit24,
							Nunit25,
							programFiles + "\\Whatever"
						};
				return new string[0];
			}
		}

		[Test]
		public void FindAllNUnitDirs ()
		{
			var dir = GivenThereAreTwoNUnitDirsInProgramFiles();
			var nunitFinder = new NUnitFinder(dir, null);
			var nunitDirs = nunitFinder.GetNUnitDirs();
			Assert.That(nunitDirs.Count(), Is.EqualTo(2));
			Assert.That(nunitDirs.ElementAt(0).FullName, Is.EqualTo(SomeNUnitDirsExist.Nunit24));
			Assert.That(nunitDirs.ElementAt(1).FullName, Is.EqualTo(SomeNUnitDirsExist.Nunit25));
		}

		[Test]
		[Ignore("WIP")]
		public void AllNUnitDirsHaveProgramFilesSuppliedByCollaborator()
		{
			var pgFinder = GivenThereIsAWeirdProgramFilesFolderInTheSystem();
			var someDirLayer = new SomeNUnitDirsExist();
			var nunitFinder = new NUnitFinder(someDirLayer, pgFinder);
			var nunitDirs = nunitFinder.GetNUnitDirs();
			foreach(var nunitDir in nunitDirs)
			{
				Assert.That(nunitDir.FullName, Is.StringStarting(pgFinder.GetPossibleLocations().ElementAt(0)));
			}
		}

		private IProgramFilesFinder GivenThereIsAWeirdProgramFilesFolderInTheSystem()
		{
			return new SomeProgramFilesFinder();
		}

		private IDirectory GivenThereAreTwoNUnitDirsInProgramFiles()
		{
			return new SomeNUnitDirsExist();
		}
	}
}
