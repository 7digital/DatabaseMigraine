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

		[TestFixtureSetUp]
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
			RetryXTimes(3, RefreshServer, DisposableDbServer);
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

		[TestFixtureTearDown]
		public void KillDisposableDbs()
		{
			DisposableDbManager.KillDb(DisposableDbServer, DisposableDbManager.GetCreatedDb(DbName));
		}




		protected void RetryXTimes<T>(int times, Action<T> method, T instance)
		{
			while (true)
			{
				try
				{
					method(instance);
					break;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					if (--times == 0)
					{
						throw;
					}
				}
			}
		}

		[Test]
		public void Retry_Calls_N_Times_If_Always_Fails()
		{
			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
				throw new Exception();
			};

			const int times = 3;
			Assert.Throws<Exception>(() => RetryXTimes(times, justThrow, 0));
			Assert.That(count, Is.EqualTo(times));
		}

		[Test]
		public void Retry_Calls_1_Time_If_It_Always_Works()
		{
			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
			};

			const int times = 3;
			RetryXTimes(times, justThrow, 0);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void Retry_Calls_N_Times_If_It_Works_At_The_Nth()
		{
			int n = 3;
			const int times = 5;
			Assert.That(n, Is.LessThan(times), "The premises of the tests are wrong, you broke the test!");

			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
				if (count < n)
				{
					throw new Exception();
				}
			};


			RetryXTimes(times, justThrow, 0);
			Assert.That(count, Is.EqualTo(n));
		}


	}
}
