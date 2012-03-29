using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using DatabaseMigraine;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseScriptUpdater
{
	class Program
	{
		static void Main(string[] args)
		{
			ParseParams(args);

			_migrationManager = new MigrationManager();

			string migrationFolder = CheckParams();

			Run (migrationFolder);

			Environment.Exit((int)DbParams.ExitStatus.Ok);
		}

		static private DbParams _paramsParsed;
		private static MigrationManager _migrationManager;

		private static void Run(string migrationFolder)
		{
			try
			{
				var server = ConnectionHelper.Connect(_paramsParsed.DbHostName, _paramsParsed.DbConnString);

				UpdateDatabaseScripts(server, migrationFolder);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("An exception occurred: " + e);
				Environment.Exit((int)DbParams.ExitStatus.ExecutionException);
			}
		}

		private static string CheckParams()
		{
			var migrationFolder = Path.Combine(_paramsParsed.DbPath.FullName, _migrationManager.FolderName);
			if (!Directory.Exists(migrationFolder))
			{
				Console.Error.WriteLine("There is no {0} folder in {1}.",
				                        _migrationManager.FolderName, _paramsParsed.DbPath);
				Environment.Exit((int)DbParams.ExitStatus.Ok);
			}

			if (!Directory.GetFileSystemEntries(migrationFolder, "*.sql").Any())
			{
				Console.WriteLine("There are no migrations in {0}", migrationFolder);
				Environment.Exit((int)DbParams.ExitStatus.Ok);
			}
			return migrationFolder;
		}

		private static void ParseParams(IEnumerable<string> args)
		{
			try
			{
				_paramsParsed = new DbParams(args);
			}
			catch (NoParamsException)
			{
				Environment.Exit((int)DbParams.ExitStatus.ParametersNotSupplied);
			}
			catch (SomeParamsMissingException e)
			{
				Console.WriteLine(e.ParamNames[0] + " is mandatory");
				Environment.Exit((int)DbParams.ExitStatus.SomeParameterNotSupplied);
			}

			if (!_paramsParsed.DbPath.Exists)
			{
				Console.Error.WriteLine("--dbpath points to a directory that does not exist");
				Environment.Exit((int)DbParams.ExitStatus.ParameterNotValid);
			}
		}

		private static void UpdateDatabaseScripts(Server server, string migrationsPath)
		{
			var migrations = _migrationManager.GetNonRetiredMigrationsRunInDb(_paramsParsed.DbPath, server, _paramsParsed.DbName);

			if (!migrations.Any())
			{
				Console.WriteLine("No migrations found in the database.");
				Environment.Exit((int)DbParams.ExitStatus.Ok);
			}

			var migrationFilePath = new FileInfo(Path.Combine(migrationsPath, migrations.First().FileNameWithoutExtension + ".sql"));
			UpdateDatabaseScriptsWithMigration(migrationFilePath);
		}

		static void UpdateDatabaseScriptsWithMigration(FileInfo migration)
		{
			string dbCreationPath;
			Server disposableDbServer = GetConfigSettings(out dbCreationPath);

			string dbScriptsPath = Path.Combine(migration.DirectoryName, "..");

			var disposableDbCreator = new DisposableDbManager(dbCreationPath, disposableDbServer, new DirectoryInfo(dbScriptsPath).Name, dbScriptsPath);

			string baseDb = disposableDbCreator.CreateCompleteDisposableDbWithMigrations(new string[0]);

			disposableDbCreator.AllowCreatingSameDb = true;
			string baseDbPlusMigration = disposableDbCreator.CreateCompleteDisposableDbWithMigrations(new[]
			{
				Path.GetFileNameWithoutExtension(migration.FullName)
			});

			var changes = DbComparer.CompareDatabases(disposableDbServer.Databases[baseDb],
			                                          disposableDbServer.Databases[baseDbPlusMigration]);

			bool migrationMerged = ManageChanges(migration, changes, dbScriptsPath);

			if (!migrationMerged) {
				Console.WriteLine("Migration merging operation found the need of making formatting changes to the scripts");
				Console.WriteLine("Modified scripts will now be tested");
			}

			string baseDbWithModifiedScripts = disposableDbCreator.CreateCompleteDisposableDbWithMigrations(new string[0]);

			if (migrationMerged) {
				VerifyChanges(migration, 
				              disposableDbServer.Databases[baseDbWithModifiedScripts],
				              disposableDbServer.Databases[baseDbPlusMigration]);
				Console.WriteLine("Changes from migration were merged into the scripts, you can commit then now, and run the program again for the next migration");
			} else {
				VerifyChanges(disposableDbServer.Databases[baseDbWithModifiedScripts],
				              disposableDbServer.Databases[baseDb]);
				Console.WriteLine("Modified scripts are safe, you can now commit your changes and run this program again");
			}
		}

		private static Server GetConfigSettings(out string dbCreationPath)
		{
			const string plsMsg = "Please configure the {0} app setting in your .config file.";

			const string dbCreationPathSetting = "DbCreationPath";
			dbCreationPath = ConfigurationManager.AppSettings[dbCreationPathSetting];
			if (String.IsNullOrEmpty(dbCreationPath))
			{
				throw new Exception(String.Format(plsMsg, dbCreationPathSetting));
			}

			const string disposableDbHostnameSetting = "DisposableDbHostname";
			string disposableDbHostname = ConfigurationManager.AppSettings[disposableDbHostnameSetting];
			if (String.IsNullOrEmpty(disposableDbHostname))
			{
				throw new Exception(String.Format(plsMsg, disposableDbHostnameSetting));
			}

			const string disposableDbConnStringSetting = "DisposableDbConnString";
			string disposableDbConnString = ConfigurationManager.AppSettings[disposableDbConnStringSetting];
			if (String.IsNullOrEmpty(disposableDbConnString))
			{
				throw new Exception(String.Format(plsMsg, disposableDbConnStringSetting));
			}
			return ConnectionHelper.Connect(disposableDbHostname, disposableDbConnString);
		}

		private static bool ManageChanges(FileInfo migrationFile, DatabaseChangeSet changes, string dbScriptsPath)
		{
			Console.WriteLine("Trying to save changes from migration {0} to local disk...", migrationFile.Name);

			bool preformatDone = DbScriptFolderManager.PreFormatScripts(dbScriptsPath, changes);
			if (preformatDone) {
				return false;
			}

			DbScriptFolderManager.UpdateScripts(dbScriptsPath, changes, migrationFile);
			Console.WriteLine("Migration merging succeeded");
			return true;
		}

		private static void VerifyChanges(FileInfo migrationFile, Database baseDbWithMigrationMergedBack, Database baseDbPlusMigration)
		{
			DatabaseChangeSet changes = DbComparer.CompareDatabases(baseDbPlusMigration, baseDbWithMigrationMergedBack, ScriptComparer.Sanitize);

			if (!changes.IsEmpty)
			{
				throw new InvalidProgramException(
					String.Format("The changes for migration {0} were merged back, but the result still contained changes!",
					migrationFile.Name));
			}

			string historyFolderPath = Path.Combine(migrationFile.DirectoryName, "History");
			var historyFolder = !Directory.Exists(historyFolderPath) ? Directory.CreateDirectory(historyFolderPath) : new DirectoryInfo(historyFolderPath);

			_migrationManager.GenerateMigrationsContentsToDisk(new[] { migrationFile }, historyFolder, _paramsParsed.DbName);
			migrationFile.Delete();
			Console.WriteLine("Migration {0} taken to the History subfolder", migrationFile.Name);
		}

		private static void VerifyChanges(Database baseDbWithFormattedScripts, Database baseDb)
		{
			DatabaseChangeSet changes = DbComparer.CompareDatabases(baseDb, baseDbWithFormattedScripts);

			if (!changes.IsEmpty)
			{
				throw new InvalidProgramException("The formatting changes done to be pushed before migration merging have generated differences!");
			}
		}
	}
}
