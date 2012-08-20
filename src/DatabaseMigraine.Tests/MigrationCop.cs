using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;
using System.Linq;
using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class MigrationCop
	{
		public abstract string DbName { get; }

		private string _dbCreationPath;
		private Server _disposableDbServer;
		private DirectoryInfo _dbPath;

		[Test]
		public void CheckThatDisposableDbCreationStillWorksAfterApplyingEachMigration()
		{
			_dbCreationPath = ConfigurationManager.AppSettings["DbCreationPath"];
			_disposableDbServer = DatabaseCreationTests.GetDisposableDbServer();

			var creator = new DisposableDbManager(_dbCreationPath, _disposableDbServer, DbName);
			creator.AllowCreatingSameDb = true;

			var baseDbWithNoMigrations = creator.CreateCompleteDisposableDbWithMigrations(new string[0]);

			_dbPath = DisposableDbManager.FindDatabaseScriptsPath(DbName);
			IEnumerable<FileInfo> migrations = new MigrationManager().GetSqlSriptsIn(_dbPath.FullName);

			var migrationsAlreadyTested = new List<string>();

			foreach (var migration in migrations)
			{
				TestMigration(migration, migrationsAlreadyTested, baseDbWithNoMigrations);
				migrationsAlreadyTested.Add(Path.GetFileNameWithoutExtension(migration.FullName));
			}
		}

		private void TestMigration(FileInfo migration, ICollection<string> migrationsAlreadyTested, string baseDbWithNoMigrations)
		{
			var tempPath = Path.GetTempPath();
			var randomDirName = "mig" + MigrationManager.GetMigrationId(Path.GetFileNameWithoutExtension(migration.FullName)) + "-" +
				new Random().Next(10000);
			var tempDir = Directory.CreateDirectory(Path.Combine(tempPath, randomDirName));
			var dbTempDirPath = Path.Combine(tempDir.FullName, DbName);

			bool? success = null;
			try
			{
				DatabaseChangeSet changesBetweenMigrationAppliedVersusMerged = 
					CreateDbWithMigration(migration, migrationsAlreadyTested, baseDbWithNoMigrations, dbTempDirPath);

				success = changesBetweenMigrationAppliedVersusMerged.IsEmpty;
				if (!success.Value)
				{
					ExplainFailure(changesBetweenMigrationAppliedVersusMerged, migration.Name);
				}
			}
			finally
			{
				if (success == null || success.Value)
					Directory.Delete(tempDir.FullName, true);
			}
		}

		private DatabaseChangeSet TryMergingAndCompare(FileInfo migration, string baseDbWithNoMigrations, ICollection<string> migrationsToRun, string dbTempDirPath)
		{
			string dbPlusMigration = null, dbWithMigrationMerged = null;
			DatabaseChangeSet changes = null;
			try {
				if (Directory.Exists(dbTempDirPath))
				{
					Directory.Delete(dbTempDirPath, true);
				}
				var dbTempDir = Directory.CreateDirectory(dbTempDirPath);
				Folders.CopyDirectory(_dbPath.FullName, dbTempDir.FullName);
				Console.WriteLine(dbTempDir.FullName);

				var lonelyCreator = new DisposableDbManager(_dbCreationPath, _disposableDbServer, DbName, dbTempDir.FullName);
				lonelyCreator.AllowCreatingSameDb = true;

				dbPlusMigration = lonelyCreator.CreateCompleteDisposableDbWithMigrations(migrationsToRun);
				DatabaseChangeSet changesFromMigration =
					DbComparer.CompareDatabases(_disposableDbServer.Databases[baseDbWithNoMigrations],
												_disposableDbServer.Databases[dbPlusMigration]);

				DbScriptFolderManager.UpdateScripts(dbTempDir.FullName, changesFromMigration, migration);

				dbWithMigrationMerged = lonelyCreator.CreateCompleteDisposableDbWithMigrations(new string[0]);
				changes = DbComparer.CompareDatabases(_disposableDbServer.Databases[dbPlusMigration], _disposableDbServer.Databases[dbWithMigrationMerged],
													  DatabaseComparisonTests.DefaultSanitizeForComparison);
			} finally {
				Cleanup(dbPlusMigration, dbWithMigrationMerged);
			}

			return changes;
		}

		private void Cleanup (params string [] dbs)
		{
			foreach (var db in dbs)
			{
				if (!String.IsNullOrEmpty(db))
				{
					DisposableDbManager.KillDb(_disposableDbServer, db);
				}
			}
		}

		private static void ExplainFailure(DatabaseChangeSet changes, string migration)
		{
			string context = String.Format(
				"The migration {0} failed the sanity check of merging its effects in SQL files and having no differences over a DB with the migration applied on top of it." +
				Environment.NewLine + "The changes are:" + Environment.NewLine + "{1}",
				Path.GetFileName(migration),
				changes);

			if (changes.HasModifications)
			{
				string nameOfElement, after, before;
				changes.GetFirstDifference(out nameOfElement, out after, out before);

				Assert.AreEqual(after, before, context);
			}
			else
			{
				Assert.Fail(context);
			}
		}

		private DatabaseChangeSet CreateDbWithMigration(FileInfo migration, ICollection<string> previousMigrations, string baseDbWithNoMigrations, string dbTempDirPath)
		{
			var previousMigrationsStack = new Stack<string>(previousMigrations);
			var migrationsToRun = new List<string>(new [] { Path.GetFileNameWithoutExtension(migration.FullName) });

			//we do this because a migration may depend on a previous one
			while (true){
				try
				{
					return TryMergingAndCompare(migration, baseDbWithNoMigrations, migrationsToRun, dbTempDirPath);
				} catch (InvalidOperationException e) {

					Console.WriteLine(
						"Caught {0} when checking the sanity of migration {1}, migrations included in the set: {2}",
						e, migration, String.Join(", ", migrationsToRun.ToArray()));
					Console.WriteLine("Migrations not yet included in the set:" + String.Join(", ", previousMigrationsStack.ToArray()));

					if (previousMigrationsStack.Count == 0)
					{
						throw;
					}
					migrationsToRun.Add(previousMigrationsStack.Pop());
				}
			}
		}

		//TODO: change this to be a regexp, i.e. to have "http://tickets/issues/\d+"
		private readonly string[] _migrationStringContainingPolicies = new []
		{
			//because we want devs to be checking with DBAs about their migration ASAP:
			"http://some.address.of.a.ticketing.system/",

			//because DBAs can do migrations without approval:
			"some.email.address.of.a.dba??@foo.corp",
		};

		private readonly string[] _migrationPolicies = new[]
		{
			"Either specify the URL of the ticket filed with DBAs in a SQL comment, or",
			"If you're a DBA yourself just add your email in a SQL comment."
		};

		[Test]
		[Ignore("We haven't figured out yet what will be the process for the migrations to be reviewed")]
		public void CheckMigrationPolicies()
		{
			_dbPath = DisposableDbManager.FindDatabaseScriptsPath(DbName);

			var undocumentedMigrations = new List<string>();
			var migrations = new MigrationManager().GetSqlSriptsIn(_dbPath.FullName);
			foreach (var migration in migrations)
			{
				var migrationContents = File.ReadAllText(migration.FullName).ToLower();

				bool found = false;
				foreach (var migrationPolicyText in _migrationStringContainingPolicies)
				{
					if (migrationContents.Contains(migrationPolicyText.ToLower()))
					{
						found = true;
					}
				}

				if (!found) {
					undocumentedMigrations.Add(migration.Name);
				}
			}

			if (undocumentedMigrations.Count > 0)
			{
				throw new InvalidOperationException(
					String.Format("The following migrations don't comply with the policies: {0}" + Environment.NewLine +
						"Policies are: {1}",
					Environment.NewLine + string.Join("," + Environment.NewLine, undocumentedMigrations.ToArray()),
					Environment.NewLine + string.Join("," + Environment.NewLine, _migrationPolicies.ToArray())));
			}
		}

		[Test]
		public void CheckStuckMigrations()
		{
			_disposableDbServer = DatabaseCreationTests.GetDisposableDbServer();

			TimeSpan maxPeriod = TimeSpan.FromDays(15);

			DirectoryInfo dbpath = DisposableDbManager.FindDatabaseScriptsPath(DbName);

			var stuckMigrations = new List<MigrationManager.Migration>();
			var migrationManager = new MigrationManager();

			IEnumerable<string> migrationSqlFiles =
				migrationManager.GetSqlSriptsIn(dbpath.FullName).Select(file => Path.GetFileNameWithoutExtension(file.Name));

			foreach (var migration in migrationManager.GetMigrationsRunInDb(dbpath, _disposableDbServer, DbName))
			{
				if (migrationSqlFiles.Contains(migration.FileNameWithoutExtension) &&
					migration.AppliedDate.Add(maxPeriod) < DateTime.Now)
					stuckMigrations.Add(migration);
			}
			if (stuckMigrations.Count > 0)
			{
				throw new InvalidOperationException(
					String.Format(
						"The following migrations have been more than {0} days in staging phase " + 
						"(they need to be merged to the History/ subforlder to not be considered in 'staging'): {1}",
						maxPeriod.Days, Environment.NewLine + string.Join("," + Environment.NewLine, stuckMigrations.Select(m => m.FileNameWithoutExtension).ToArray())));
			}
		}
	}
}
