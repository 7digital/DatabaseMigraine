using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using DatabaseMigraine.DatabaseElements;
using DatabaseMigraine.Managers;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	//TODO: check that there are no creation of logins in Database.sql (or anywhere) but in Logins.sql
	//      check that every table name begins with some predefined keyword ("tbl", "lku", ...)
	//      check to never use @@IDENTITY , according to http://tickets.7digital.local/issues/1346
	//      check for NVARCHAR vs VARCHAR (Rob says the former because of Asian chars, but: http://blog.brianhartsock.com/2008/12/14/nvarchar-vs-varchar-in-sql-server-beware/ )
	//      compare Logins, indexes, constraints, static data...

	public abstract class DatabaseComparisonTests : DatabaseCreationTests
	{
		//FIXME: rename these databases at some point, please, to get rid of this property
		protected virtual string DifferentDbNameInReferenceEnvironment { get { return DbName; } }

		[Ignore("It's already part of DbCreationTests")]
		public override void All_old_test_databases_are_dropped()
		{
			base.All_old_test_databases_are_dropped();
		}

		[Ignore("It's already part of DbCreationTests")]
		public override void Create_test_dbs()
		{
			base.Create_test_dbs();
		}

		private string _referenceDbHostname;
		protected string ReferenceConnectionString;

		[TestFixtureSetUp]
		public override void SetupConnections()
		{
			base.SetupConnections();

			string setting = String.Format("ReferenceDbHostname+{0}", DbName);

			_referenceDbHostname = ConfigurationManager.AppSettings[setting];
			if (String.IsNullOrEmpty(_referenceDbHostname))
			{
				if ("N/A".Equals(ConfigurationManager.AppSettings["ReferenceDb+" + DbName]))
					Assert.Ignore(String.Format("Apparently there is no {0} instance in this environment", DbName));

				throw new Exception(String.Format("Please specify in the app.config file a '{0}' setting", setting));
			}

			Console.WriteLine("Reference server to compare scripts is: " + _referenceDbHostname);

			setting = String.Format("ReferenceDbConnString+{0}", DbName);
			string referenceDbConnString = ConfigurationManager.AppSettings[setting];
			if (String.IsNullOrEmpty(referenceDbConnString))
				throw new Exception(String.Format(
					"Please specify in the app.config file '{0}' setting for the connection string to connect to the reference database(s) in server '{1}'",
					setting, _referenceDbHostname));

			ReferenceConnectionString = String.Format("{0}Server={1};", referenceDbConnString, _referenceDbHostname);

			_referenceServer = new Server(new ServerConnection(new SqlConnection(ReferenceConnectionString)));
			_scriptsPath = DisposableDbManager.FindDatabaseScriptsPath(DbName);
			_migrationsInStagingForReferenceEnvironment =
				new MigrationManager().GetNonRetiredMigrationsRunInDb(_scriptsPath, _referenceServer, DifferentDbNameInReferenceEnvironment).ToArray();
			_migrationsForReferenceEnvironment =
				new MigrationManager().GetMigrationsRunInDb(_scriptsPath, _referenceServer, DifferentDbNameInReferenceEnvironment).ToArray();
		}

		private Server _referenceServer;

		//FIXME: this should be private really:
		protected Database ReferenceDb;

		private DirectoryInfo _scriptsPath;
		private MigrationManager.Migration[] _migrationsInStagingForReferenceEnvironment;
		private MigrationManager.Migration[] _migrationsForReferenceEnvironment;

		[Test]
		public void Scripts_for_01_Tables_are_the_same_as_the_running_database()
		{
			Console.WriteLine("Reference server to compare scripts is: " + _referenceDbHostname);
			CompareScripts<TableElement, TableElementFactory>();
		}

		[Test]
		public void Scripts_for_02_ForeignKeys_are_the_same_as_the_running_database()
		{
			CompareScripts<ForeignKeyElement, ForeignKeyElementFactory>();
		}

		[Test]
		public virtual void Scripts_for_03_Triggers_are_the_same_as_the_running_database()
		{
			CompareScripts<DatabaseTriggerElement, DatabaseTriggerElementFactory>();

			CompareScripts<TableTriggerElement,TableTriggerElementFactory>();
		}


		[Test]
		public void Scripts_for_04_Functions_are_the_same_as_the_running_database()
		{
			CompareScripts<FunctionElement,FunctionElementFactory>();
		}

		[Test]
		public void Scripts_for_05_Views_are_the_same_as_the_running_database()
		{
			CompareScripts<ViewElement,ViewElementFactory>();
		}

		[Test]
		public virtual void Scripts_for_06_StoredProcedures_are_the_same_as_the_running_database()
		{
			CompareScripts<StoredProcedureElement,StoredProcedureElementFactory>();
		}

		[Test]
		public virtual void Additional_Dbs_In_Same_Reference_Environment_Have_Same_Amount_Of_Migrations_Applied ()
		{
			CheckAdditionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied(true);
		}

		private void CheckAdditionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied(bool throwIfFails)
		{
			foreach (Database db in GetListOfAdditionalDbsInRefEnv(_referenceServer))
			{
				List<string> migrationsApplied;
				try
				{
					migrationsApplied = new List<string>(
						new MigrationManager().GetMigrationsRunInDb(_scriptsPath, _referenceServer, db.Name).Select(m => m.FileNameWithoutExtension));
				}
				catch (ExecutionFailureException)
				{
					// do not care about additional DBs which we don't have access to
					continue;
				}

				var lackingMigrations = new List<MigrationManager.Migration>();
				foreach(var migration in _migrationsForReferenceEnvironment)
				{
					if (!migrationsApplied.Contains(migration.FileNameWithoutExtension))
					{
						if (!throwIfFails)
						{
							_additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied = false;
							return;
						}
						lackingMigrations.Add(migration);
					}
				}
				if (lackingMigrations.Count > 0) {
					Assert.Fail(String.Format("The following migrations have not been manually run in DB {0}", db.Name) +
						" (if you want to stop doing this manually, use disposable DBs in the tests that depend on this DB, so we can get rid of it):" + Environment.NewLine
						+ String.Join(Environment.NewLine, lackingMigrations.Select(m => m.FileNameWithoutExtension).ToArray()));
				}
			}

			_additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied = true;
		}

		private bool? _additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied = null;

		public void CompareScripts<T,F>()
			where T : IScriptableDatabaseElementWithName
			where F : IScriptableWrapperFactory<T>, new()
		{
			var referenceServer = new Server(new ServerConnection(new SqlConnection(ReferenceConnectionString)));

			string refDbName = DifferentDbNameInReferenceEnvironment;
			var referenceDb = referenceServer.Databases[refDbName];

			Assert.IsNotNull(referenceDb,
				String.Format("Database {0} not found in server {1}",
				              refDbName, _referenceDbHostname));

			ReferenceDb = referenceDb;
			CompareScriptsContent<T,F>();

			if (_additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied == null)
			{
				CheckAdditionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied(false);

				if (_additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied == null)
				{
					Assert.Fail("CheckAdditionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied function should set additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied value");
				}
			}

			if (_additionalDbsInSameReferenceEnvironmentHaveSameAmountOfMigrationsApplied.Value) {
				CompareAdditionalDbs<T,F>(referenceServer);
			}
		}

		private void CompareAdditionalDbs<T,F>(Server referenceServer)
			where T : IScriptableDatabaseElementWithName
			where F : IScriptableWrapperFactory<T>, new()
		{
			foreach (Database db in GetListOfAdditionalDbsInRefEnv(referenceServer)) {
				try
				{
					ReferenceDb = db;
					CompareScriptsContent<T,F>();
				}
				catch (ExecutionFailureException)
				{
					// do not care about additional DBs which we don't have access to
				}
			}
		}

		private IEnumerable<Database> GetListOfAdditionalDbsInRefEnv(Server referenceServer)
		{
			var list = referenceServer.Databases.Cast<Database>()
				.Where(db => !db.Name.Equals(DifferentDbNameInReferenceEnvironment, StringComparison.InvariantCultureIgnoreCase) &&
				       db.Name.StartsWith(DifferentDbNameInReferenceEnvironment, StringComparison.InvariantCultureIgnoreCase))
					.ToList();
			return list;
		}

		private void CompareScriptsContent<T,F>()
			where T : IScriptableDatabaseElementWithName
			where F : IScriptableWrapperFactory<T>, new()
		{
			var disposableDbCreated = _disposableDbManager.CreateCompleteDisposableDbWithMigrations(_migrationsInStagingForReferenceEnvironment.Select(m => m.FileNameWithoutExtension));

			var changeSet = DbComparer.CompareDatabases<T,F>(
				ReferenceDb, 
				DisposableDbServer.Databases[disposableDbCreated],
				GenericDiscard,
				SanitizeForComparison);

			if (!changeSet.IsEmpty)
			{
				AssertNonEmptyChangeSet<T>(changeSet, ReferenceDb.Name, disposableDbCreated);
			}
		}

		private bool GenericDiscard<T>(T dbElement) where T : IScriptableDatabaseElementWithName
		{
			return dbElement.Name.ToLower().StartsWith(DbScriptFolderManager.TempPrefix) ||
			       dbElement.Name.ToLower().StartsWith(DbScriptFolderManager.DbaPrefix)  ||
			       Discard(dbElement);
		}

		protected virtual bool Discard<T>(T dbElement) where T : IScriptableDatabaseElementWithName
		{
			return false;
		}

		internal static string DefaultSanitizeForComparison(string content)
		{
			return ScriptComparer.Sanitize(content);
		}

		protected virtual string SanitizeForComparison (string content)
		{
			return DefaultSanitizeForComparison(content);
		}

		private void AssertNonEmptyChangeSet<T>(ChangeSet<T> changeSet, string db1, string db2) where T : IScriptableDatabaseElementWithName
		{
			string context = String.Format("Comparison of {0}s returned differences between {1} and {2}.",
				typeof(T).Name, db1, db2) + Environment.NewLine;
			
			if (changeSet.Modified.Count > 0)
			{
				Assert.AreEqual(
					SanitizeForComparison(changeSet.Modified.Last().Value.After.ScriptContents), 
					SanitizeForComparison(changeSet.Modified.Last().Value.Before.ScriptContents),
					context + changeSet);
			} else {
				Assert.Fail(context + changeSet);
			}
		}
	}
}
