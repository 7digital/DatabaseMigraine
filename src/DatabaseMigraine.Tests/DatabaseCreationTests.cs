using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DatabaseCreationTests
	{
		public abstract string DbName { get; }

	    private static string _dbCreationPath;
		private const uint DB_MAX_DAYS_OLD = 1;

		private static ServerConnection _disposableDbServerConnection;
		protected static Server DisposableDbServer;
		protected static DisposableDbManager _disposableDbManager;

		internal static Server GetDisposableDbServer()
		{
			return new Server(GetDisposableServerConn());
		}

		public virtual void SetupConnections()
		{
			_dbCreationPath = ConfigurationManager.AppSettings["DbCreationPath"];
			Assert.That(_dbCreationPath, Is.Not.Null, "Please specify a DbCreationPath appsetting in your config file");
			Assert.That(_dbCreationPath, Is.Not.Empty, "Please specify a DbCreationPath appsetting in your config file that is not empty");

			_disposableDbServerConnection = GetDisposableServerConn();

			SetUpDisposableServer();
			_disposableDbManager = new DisposableDbManager(_dbCreationPath, DisposableDbServer, DbName);
		}

		private static ServerConnection GetDisposableServerConn()
		{
			string disposableDbConnString = ConfigurationManager.AppSettings["DisposableDbConnString"];
			Assert.That(disposableDbConnString, Is.Not.Null);
			Assert.That(disposableDbConnString, Is.Not.Empty);

			string disposableDbHostname = ConfigurationManager.AppSettings["DisposableDbHostname"];
			Assert.That(disposableDbHostname, Is.Not.Null);
			Assert.That(disposableDbHostname, Is.Not.Empty);

			var disposableDbConnectionString = String.Format("{0}Server={1};", disposableDbConnString, disposableDbHostname);
			var disposableDbSqlConnection = new SqlConnection(disposableDbConnectionString);
			return new ServerConnection(disposableDbSqlConnection);
		}

		[SetUp]
		public void SetUpDisposableServer()
		{
			DisposableDbServer = new Server(_disposableDbServerConnection);
			Retry.RetryXTimes(3, RefreshServer, DisposableDbServer);
		}

		private void RefreshServer(Server server)
		{
			server.Refresh();
		}

		[Test]
		public virtual void Create_test_dbs()
		{
			_disposableDbManager.CreateCompleteDisposableDb();
		}

		[Test]
		public virtual void All_old_test_databases_are_dropped()
		{
			_disposableDbManager.DropDbs(DB_MAX_DAYS_OLD);
		}

		public virtual void KillDisposableDbs()
		{
			DisposableDbManager.KillDb(DisposableDbServer, DisposableDbManager.GetCreatedDb(DbName));
		}

	}
}
