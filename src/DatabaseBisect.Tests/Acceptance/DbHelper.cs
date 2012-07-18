using System;
using System.Linq;
using System.Threading;
using DatabaseMigraine;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	public abstract class DbHelper
	{
		protected abstract string TestDbName { get; }

		protected Analyst Analyst = new Analyst();

		internal IDataBase GivenADisposableDbCreatedForTesting()
		{
			var server = GivenADbServerICanConnectTo();
			return AndICreateAnEmptyDbOnIt(server);
		}

		private IDataBase AndICreateAnEmptyDbOnIt(Server server)
		{
			var disposableDbManager = new DisposableDbManager("C:\\SqlData", server, TestDbName);
			disposableDbManager.AllowCreatingSameDb = true;

			//prevent name collisions; should be fixed in DisposableDbManager (FIXME)
			Thread.Sleep(TimeSpan.FromSeconds(1));

			var disposableDbName = disposableDbManager.CreateCompleteDisposableDb();
			server.Refresh();
			return new DataBase (server.Databases[disposableDbName]);
		}

		internal static Server GivenADbServerICanConnectTo()
		{
			return ConnectionHelper.Connect("(local)", "Trusted_Connection=true;Min Pool Size=5;Max Pool Size=200;");
		}

		internal static void ThenOneTableIsClearedAndABackupOfItIsDone(IDataBase db, DbState previousState)
		{
			var afterState = new DbState(db);
			Assert.That(afterState, Is.Not.EqualTo(previousState), "DB state is the same as the previous one, so bisect operation didn't do anything");

			Assert.That(afterState.Keys.Count, Is.EqualTo(previousState.Keys.Count + 1));

			var newTable = GetNewTable(previousState, afterState);
			Assert.That(DatabaseBisect.Analyst.IsBackUpTable(newTable));

			BackupTableShouldHaveSameNumberOfRowsAsOriginalTable(previousState, newTable, afterState);

			Assert.That(afterState[DatabaseBisect.Analyst.GetOriginalTable(newTable)], Is.EqualTo(0));
		}

		private static void BackupTableShouldHaveSameNumberOfRowsAsOriginalTable(DbState previousState, string newTable, DbState afterState)
		{
			Assert.That(afterState[newTable], Is.EqualTo(previousState[DatabaseBisect.Analyst.GetOriginalTable(newTable)]));
		}

		private static string GetNewTable(DbState previousState, DbState afterState)
		{
			if (previousState.Equals(afterState))
			{
				throw new InvalidArgumentException("Both DB states are equal");
			}
			foreach (string table in afterState.Keys)
			{
				if (!previousState.ContainsKey(table))
				{
					return table;
				}
			}
			throw new InvalidArgumentException("Both DB states seem to be equal after all?");
		}


		internal static DbState AndTheDbHasAtLeast2TablesAndSecondOneIsNotEmpty(IDataBase db)
		{
			Assert.That(db.Tables.Count, Is.GreaterThan(1));
			var state = new DbState(db);
			Assert.That(state[state.Keys.ElementAt(1)], Is.GreaterThan(0));
			return state;
		}

		internal static DbState AndTheDbHasAtLeast2TablesAndTheFirstOneIsEmptyAndSecondOneIsNotEmpty(IDataBase db)
		{
			Assert.That(db.Tables.Count, Is.GreaterThan(1));
			var state = new DbState(db);
			Assert.That(state[state.Keys.First()], Is.EqualTo(0));
			Assert.That(state[state.Keys.ElementAt(1)], Is.GreaterThan(0));
			return state;
		}

		internal void WhenIPerformTheClearAndTestOperationWithATestThatFails(IDataBase db, Table table)
		{
			Bisector.BisectTableOnce(db, table, TestOperationThatFails());
		}

		internal void WhenIPerformTheClearAndTestOperationWithATestThatPasses(IDataBase db, Table table)
		{
			Bisector.BisectTableOnce(db, table, TestOperationThatSucceeds());
		}

		internal static void ThenTheClearIsRevertedSoTheDbIsInTheSameStateForNonBackupTables(IDataBase db, DbState previousState)
		{
			Assert.That(new DbState(db).EqualsOriginal(previousState));
		}

		protected virtual Func<IDataBase, bool> TestOperationThatFails()
		{
			return db => false;
		}

		protected virtual Func<IDataBase,bool> TestOperationThatSucceeds()
		{
			return db => true;
		}
	}
}