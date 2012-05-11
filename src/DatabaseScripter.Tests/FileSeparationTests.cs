using System;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace DatabaseScripter.Tests
{
	[TestFixture]
	public class FileSeparationTests
	{
		[Test]
		public void Basic ()
		{
			string coupleOfCreateTableInstructions = @"
CREATE TABLE [dbo].[blah] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ContributorID] [int] NOT NULL
)

CREATE TABLE blah2 (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ContributorID] [int] NOT NULL
)";
			
			File.WriteAllText (Path.Combine (Directory.GetCurrentDirectory(), "somefilename.sql"), coupleOfCreateTableInstructions);
			
			var process = new Process ();
			process.StartInfo = new ProcessStartInfo();
			process.StartInfo.FileName = "mono DatabaseScripter.exe";
			process.Start ();
			process.WaitForExit ();
			
			Assert.That (File.Exists(Path.Combine (Directory.GetCurrentDirectory (), "blah.sql")), Is.True);
			Assert.That (File.Exists(Path.Combine (Directory.GetCurrentDirectory (), "blah2.sql")), Is.True);
		}
	}
}

