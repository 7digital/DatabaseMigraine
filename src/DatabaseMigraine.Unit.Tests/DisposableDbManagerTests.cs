using System.Data.SqlClient;
using NUnit.Framework;

namespace DatabaseMigraine.Unit.Tests
{
	[TestFixture]
	[Explicit("SQLServer is not installed in the build agents yet")]
	public class DisposableDbManagerTests
	{
		private const string DbCreationPath = @"C:\SQLdata";
		private const string CatalogName = "foo";
		private const string ConnectionString = "Server=(local);Trusted_Connection=true;Min Pool Size=5;Max Pool Size=200;";
		private string _disposableDbName;
		private string _connectionStringWithDbName;

		[TestFixtureSetUp]
		public void Setup()
		{
			var databaseManager = new DisposableDbManager(DbCreationPath, ConnectionString, CatalogName);
			_disposableDbName = databaseManager.CreateCompleteDisposableDb();

			var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString)
			                                 	{
			                                 		InitialCatalog = _disposableDbName
			                                 	};
			_connectionStringWithDbName = sqlConnectionStringBuilder.ToString();
		}

		[Test]
		public void Should_Be_Able_To_Create_DisposableDb_From_ConnectionString()
		{
			Assert.DoesNotThrow(() => new SqlConnection(_connectionStringWithDbName).Open());
		}
		
		[Test]
		public void Should_be_able_to_KillDB_from_ConnectionString()
		{
			DisposableDbManager.KillDb(_connectionStringWithDbName);
			Assert.That(CheckDatabaseExists(_disposableDbName), Is.False);
		}

		private static bool CheckDatabaseExists(string databaseName)
		{
			var conn = new SqlConnection(ConnectionString);

			string query = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", databaseName);

			using (conn)
			using (var sqlCmd = new SqlCommand(query, conn))
			{
				conn.Open();
				var databaseId = (int?) sqlCmd.ExecuteScalar();
				conn.Close();

				return databaseId.HasValue && databaseId.Value > 0;
			}
		}
	}
}