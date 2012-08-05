
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Unit.TestLaunch
{
	[TestFixture]
	public class NUnitFinding
	{
		class SomeNUnitDirsExist : IDirectory
		{
			public bool Exists(string path)
			{
				throw new NotImplementedException(); //doesn't apply for this test
			}

			public string[] GetFileSystemEntries(string path)
			{
				if ("Program Files" == path)
					return new[]
						{
							"C:\\Program Files\\NUnit 2.5.10",
							"C:\\Program Files\\NUnit 2.4.10",
							"C:\\Program Files\\Whatever"
						};
				return new string[0];
			}
		}

		[Test]
		public void FindAllNUnitDirs ()
		{
			var dir = GivenThereAreTwoNUnitDirsInProgramFiles();
			var nunitFinder = new NUnitFinder(dir);
			Assert.That(nunitFinder.GetNUnitDirs().Count(), Is.EqualTo(2));
		}

		private IDirectory GivenThereAreTwoNUnitDirsInProgramFiles()
		{
			return new SomeNUnitDirsExist();
		}
	}
}
