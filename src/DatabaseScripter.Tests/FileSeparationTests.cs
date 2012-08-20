using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace DatabaseScripter.Tests
{
	[TestFixture]
	public class FileSeparationTests
	{
		[Test]
		public void Basic()
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

			var process = RunProgram("DatabaseScripter.exe", new string[0]);
			Assert.That(process.ExitCode, Is.EqualTo(0));
			
			var blahTable = Path.Combine(Directory.GetCurrentDirectory(), "blah.sql");
			var blah2Table = Path.Combine(Directory.GetCurrentDirectory(), "blah2.sql");

			Assert.That(File.Exists(blahTable), Is.True);
			Assert.That(File.Exists(blah2Table), Is.True);

			Assert.That(File.ReadAllText(blahTable).Trim(), Is.EqualTo(@"CREATE TABLE [dbo].[blah] (
				[ID] [int] IDENTITY(1,1) NOT NULL,
				[ContributorID] [int] NOT NULL
				)".Trim()));

			Assert.That(File.ReadAllText(blah2Table).Trim(), Is.EqualTo(@"CREATE TABLE blah2 (
				[ID] [int] IDENTITY(1,1) NOT NULL,
				[ContributorID] [int] NOT NULL
				)"));
		}

		[Test]
		public void Should_separate_out_create_table_statements()
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

			var separateCreateTableStatements = SqlStatementSeparator.SeparateCreateTableStatements(coupleOfCreateTableInstructions).ToList();

			Assert.That(separateCreateTableStatements[0].Trim(), Is.EqualTo(@"
				CREATE TABLE [dbo].[blah] (
				[ID] [int] IDENTITY(1,1) NOT NULL,
				[ContributorID] [int] NOT NULL
				)".Trim()));

			Assert.That(separateCreateTableStatements[1].Trim(), Is.EqualTo(@"
				CREATE TABLE blah2 (
				[ID] [int] IDENTITY(1,1) NOT NULL,
				[ContributorID] [int] NOT NULL
				)".Trim()));
		}

		[Test]
		public void Should_split_out_create_table_statements_into_separate_files()
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

			CreateTableWriter.SeparateCreateTableStatementsToSeparateFiles(coupleOfCreateTableInstructions);

			var blahTable = Path.Combine(Directory.GetCurrentDirectory(), "blah.sql");
			var blah2Table = Path.Combine(Directory.GetCurrentDirectory(), "blah2.sql");
			Assert.That(File.Exists(blahTable), Is.True);
			Assert.That(File.Exists(blah2Table), Is.True);

			Assert.That(File.ReadAllText(blahTable).Trim(), Is.EqualTo(@"CREATE TABLE [dbo].[blah] (
				[ID] [int] IDENTITY(1,1) NOT NULL,
				[ContributorID] [int] NOT NULL
				)".Trim()));
		}

		static Process RunProgram(string exe, params string[] args)
		{
			ManualResetEvent mreProcessExit = new ManualResetEvent(false);
			ManualResetEvent mreOutputDone = new ManualResetEvent(false);
			ManualResetEvent mreErrorDone = new ManualResetEvent(false);

			ProcessStartInfo psi = new ProcessStartInfo(exe, String.Join(" ", args));
			psi.WorkingDirectory = Environment.CurrentDirectory;

			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			psi.ErrorDialog = true;

			Process process = new Process();
			process.StartInfo = psi;

			process.Exited += delegate(object o, EventArgs e)
			{
				Console.WriteLine("Exited.");
				mreProcessExit.Set();
			};
			process.OutputDataReceived += delegate(object o, DataReceivedEventArgs e)
			{
				if (e.Data != null)
					Console.WriteLine("Output: {0}", e.Data);
				else
					mreOutputDone.Set();
			};
			process.ErrorDataReceived += delegate(object o, DataReceivedEventArgs e)
			{
				if (e.Data != null)
					Console.Error.WriteLine("Error: {0}", e.Data);
				else
					mreErrorDone.Set();
			};

			process.EnableRaisingEvents = true;
			Console.WriteLine("Start: {0}", process.StartInfo.FileName);
			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();

			if (process.HasExited)
				mreProcessExit.Set();

			while (!WaitHandle.WaitAll(new WaitHandle[] { mreErrorDone, mreOutputDone, mreProcessExit }, 100))
				continue;
			return process;
		}
	}
}

