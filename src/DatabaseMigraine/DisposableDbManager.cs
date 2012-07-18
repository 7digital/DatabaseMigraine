using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine
{
	public class DisposableDbManager
	{
		private const string DELETEME_PREFIX = "DELETEME_UNITTEST_";
		private const string DATE_FORMAT = "yyyyMMdd_HHmmss";

		private readonly static Dictionary<string, string> _databasesCreated = new Dictionary<string, string>();

		private static string _dbCreationPath;
		private readonly Server _disposableDbServer;
		private readonly SqlExecutor _sqlExecutor;
		private readonly string _dbNameInVcs;
		private string _dbScriptsPath;

		public bool AllowCreatingSameDb { get; set; }

		public DisposableDbManager(string dbCreationPath, Server disposableDbServer, string dbNameInVcs)
		{
			_dbCreationPath = dbCreationPath;
			_disposableDbServer = disposableDbServer;
			_sqlExecutor = new SqlExecutor(_disposableDbServer);
			_dbNameInVcs = dbNameInVcs;
		}

		public DisposableDbManager(string dbCreationPath, Server disposableDbServer, string dbNameInVcs, string dbScriptsPath):
			this (dbCreationPath, disposableDbServer, dbNameInVcs)
		{
			if (!Directory.Exists(dbScriptsPath))
			{
				throw new ArgumentException("dbScriptsPath supplied does not exist", dbScriptsPath);
			}

			if (new DirectoryInfo(dbScriptsPath).Name != dbNameInVcs)
			{
				throw new ArgumentException("dbScriptsPath supplied needs to have the same name as the dbNameInVcs parameter supplied", dbNameInVcs);
			}

			_dbScriptsPath = dbScriptsPath;
		}

		public string CreateCompleteDisposableDb()
		{
			return CreateCompleteDisposableDb(string.Empty);
		}

		public string CreateCompleteDisposableDbWithMigrations(IEnumerable<string> migrationFileNames)
		{
			return CreateDisposableDb(true, migrationFileNames, string.Empty);
		}

		public static string GetCreatedDb(string dbNameInVcs)
		{
			if (_databasesCreated.Keys.Contains(dbNameInVcs))
			{
				return _databasesCreated[dbNameInVcs];
			}
			return null;
		}

		private string CreateDisposableDb(bool includeProgrammabilityAndViews, IEnumerable<string> migrationWhiteList, string prefix)
		{
			if (!AllowCreatingSameDb) {
				if (_databasesCreated.Keys.Contains(_dbNameInVcs)) {
					return _databasesCreated[_dbNameInVcs];
				}
			}

			Console.WriteLine("Attempting to create db {0}...", _dbNameInVcs);
			if (_dbScriptsPath == null)
				_dbScriptsPath = FindDatabaseScriptsPath(_dbNameInVcs).FullName;
			Console.WriteLine("Scripts found in {0}", _dbScriptsPath);
			string disposableDbName = CreateDb(_dbNameInVcs, _dbScriptsPath, true, prefix);
			Console.WriteLine("Successfully created {0} database with name {1}", _dbNameInVcs, disposableDbName);

			CreateSchema(_dbScriptsPath, disposableDbName);

			if (!AllowCreatingSameDb) {
				_databasesCreated[_dbNameInVcs] = disposableDbName;
			}

			if (includeProgrammabilityAndViews) {
				CreateEvilBusinessLogic(_dbNameInVcs, _dbScriptsPath, disposableDbName);
			}

			Console.WriteLine("Attempting to insert static data in {0}...", _dbNameInVcs);
			int tableCount = StaticDataManager.Instance
				.RunScripts(_disposableDbServer, _dbScriptsPath, disposableDbName, _dbNameInVcs);
			Console.WriteLine("Successfully created static data for {0} tables", tableCount);

			RunMigrations(disposableDbName, migrationWhiteList);

			return disposableDbName;
		}

		private void RunMigrations(string disposableDbName, IEnumerable<string> migrationWhiteList)
		{
			if (migrationWhiteList != null)
				Console.WriteLine("Attempting to run {0} migrations in {1}...", migrationWhiteList.Count(), _dbNameInVcs);
			else
				Console.WriteLine("Attempting to run all migrations in {0}...", _dbNameInVcs);
			int migrationCount = new MigrationManager()
				.RunScripts(_disposableDbServer, _dbScriptsPath, disposableDbName, _dbNameInVcs, migrationWhiteList);
			if (migrationWhiteList != null && migrationCount < migrationWhiteList.Count())
				throw new Exception(String.Format("There was some kind of problem and {0} migrations were applied instead of {1}. First migration requested was {2}.",
				                                  migrationCount, migrationWhiteList.Count(), migrationWhiteList.First()));
			Console.WriteLine("Successfully applied {0} migrations", migrationCount);
		}

		private void CreateSchema(string dbScriptsPath, string disposableDbName)
		{
			int tableCount = TableManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, _dbNameInVcs);
			Console.WriteLine("Successfully created {0} tables", tableCount);

			int foreignKeyScriptsCount = ForeignKeyManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, _dbNameInVcs);
			Console.WriteLine("Successfully created foreign keys of {0} tables", foreignKeyScriptsCount);
		}

		private void CreateEvilBusinessLogic(string dbNameInVcs, string dbScriptsPath, string disposableDbName)
		{
			int funcCount = FunctionManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, dbNameInVcs);
			Console.WriteLine("Successfully created {0} funcs", funcCount);

			int viewCount = ViewManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, dbNameInVcs);
			Console.WriteLine("Successfully created {0} views", viewCount);

			int sprocCount = StoredProcedureManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, dbNameInVcs);
			Console.WriteLine("Successfully created {0} sprocs", sprocCount);

			int triggerCount = TriggerManager.Instance
				.RunScripts(_disposableDbServer, dbScriptsPath, disposableDbName, dbNameInVcs);
			Console.WriteLine("Successfully created {0} triggers", triggerCount);
		}

		public static DirectoryInfo FindDatabaseScriptsPath(string dbNameInVcs)
		{
			string currentPath = Directory.GetCurrentDirectory();
			Console.WriteLine("Looking for path of scripts for database {0}, starting in {1}", dbNameInVcs, currentPath);

			while (true) {
				Console.WriteLine("Looking for path in " + currentPath);
				string dbScriptsPath = Path.Combine(currentPath, dbNameInVcs);
				if (Directory.Exists(dbScriptsPath))
					return new DirectoryInfo(dbScriptsPath);

				dbScriptsPath = Path.Combine(currentPath, "db");
				if (Directory.Exists(dbScriptsPath))
				{
					Console.WriteLine("Looking for path in " + Path.Combine(dbScriptsPath, dbNameInVcs));
					if (Directory.Exists(Path.Combine(dbScriptsPath, dbNameInVcs)))
					{
						return new DirectoryInfo(Path.Combine(dbScriptsPath, dbNameInVcs));
					}

					dbScriptsPath = Path.Combine(Path.Combine(dbScriptsPath, "shared"), dbNameInVcs);
					Console.WriteLine("Looking for path in " + dbScriptsPath);
					if (Directory.Exists(dbScriptsPath))
					{
						return new DirectoryInfo(dbScriptsPath);
					}
				}

				var directories = currentPath.Split(new[] { Path.DirectorySeparatorChar });
				var dotdotcount = directories.Count(dir => dir == "..");
				if (dotdotcount > (directories.Length / 2)) {
					throw new Exception(String.Format(
						"No *.sql files found, did you forget to populate your git submodule? (via `git submodule update --init`)" + 
						" Current directory is {0}, current path is {1} and dbScriptsPath is {2}",
						Directory.GetCurrentDirectory(), currentPath, dbScriptsPath));
				}

				currentPath = Path.Combine(currentPath, "..");
			}
		}

		private string CreateDb(string originalDbName, string dbScriptsPath, bool createLogins, string suffix) {
			string creationDbScriptPath = Path.Combine(dbScriptsPath, "00_Database");

			var databaseCreationScript = new FileInfo(Path.Combine(creationDbScriptPath, "Database.sql"));
			var loginsCreationScript = new FileInfo(Path.Combine(creationDbScriptPath, "Logins.sql"));

			DbScriptFolderManager.CheckEncodingConvention(databaseCreationScript);

			string script = File.ReadAllText(databaseCreationScript.FullName);

			if (script.Contains(originalDbName))
				throw new Exception(String.Format("The script {0} contains the database name hardcoded '{1}'",
				                    databaseCreationScript.Name, originalDbName));

			if (script.Contains(":\\"))
				throw new Exception(String.Format("The script {0} contains the hardcoded paths, replace them with 'dbpath'",
				                    databaseCreationScript.Name));

			string dbname = string.Format("{0}{1}_{2}", DELETEME_PREFIX, originalDbName, GetNormalizedDate());
			if (!string.IsNullOrEmpty(suffix))
			{
				dbname += "_" + suffix;
			}

			script = script.Replace("dbpath", _dbCreationPath);
			script = SqlExecutor.ReplaceDbnameInScript(script, dbname);

			_sqlExecutor.ExecuteNonQuery(script);

			if (createLogins && loginsCreationScript.Exists) {
				DbScriptFolderManager.CheckEncodingConvention(loginsCreationScript);
				script = File.ReadAllText(loginsCreationScript.FullName);
				_sqlExecutor.ExecuteNonQuery(script, dbname);
			}

			return dbname;
		}

		public static void KillDb(Server disposableDbServer, string dbName)
		{
			try {
				disposableDbServer.ConnectionContext.ExecuteNonQuery(String.Format("ALTER DATABASE {0} SET RESTRICTED_USER WITH ROLLBACK IMMEDIATE", dbName));
			} catch (Exception e) {
				Console.Error.WriteLine(e);
			}

			disposableDbServer.KillDatabase(dbName);
		}

		[Obsolete("Please use KillDb() in your TearDown instead of DropDisposableDatabases in your SetUp")]
        public void DropDisposableDatabases(uint maxDaysOld)
		{
			DropDbs(maxDaysOld);
		}

		public void DropDbs(uint maxDaysOld) {
            string lastDatabaseCreated = _databasesCreated.LastOrDefault().Value;

            var disposableDbsToDrop = new List<Database>();
			_disposableDbServer.Refresh();
            foreach(Database db in _disposableDbServer.Databases)
            {
				if(IsDbDisposable(db) && !db.Name.Equals(lastDatabaseCreated))
				{
					DateTime dateDt = GetDateFromDisposableDbName(db);
                    if(DateTime.Now.AddDays(0.0 - maxDaysOld) > dateDt)
                    {
                        disposableDbsToDrop.Add(db);
                    }
                }
            }

            foreach (Database db in disposableDbsToDrop)
            {
				Console.WriteLine("Killing DB '{0}'...", db.Name);
				KillDb(_disposableDbServer, db.Name);
			}
		}

		public static DateTime GetDateFromDisposableDbName(Database db)
		{
			if (!IsDbDisposable(db))
			{
				throw new ArgumentException("The db is not disposable", "db");
			}

			string dateRegexPattern = @"(\d\d\d\d\d\d\d\d)_(\d\d\d\d\d\d)";
			string date = new Regex(dateRegexPattern).Match(db.Name).Captures[0].Value;

			return DenormalizeDate(date);
		}

		public static string GetNameFromDisposableDb(Database db)
		{
			if (!IsDbDisposable(db))
			{
				throw new ArgumentException("The db is not disposable", "db");
			}

			string nameAndDate = db.Name.Substring(DELETEME_PREFIX.Length);
			return nameAndDate.Substring(0, nameAndDate.IndexOf("_"));
		}

		public static bool IsDbDisposable (Database db)
		{
			return db.Name.StartsWith(DELETEME_PREFIX);
		}

		static string GetNormalizedDate()
		{
			return DateTime.Now.ToString(DATE_FORMAT);
		}

		public static DateTime DenormalizeDate(string date)
		{
			return DateTime.ParseExact(date, DATE_FORMAT, null);
		}

		public string CreateCompleteDisposableDb(string optionalSuffix)
		{
			return CreateDisposableDb(true, null, optionalSuffix);
		}
	}
}