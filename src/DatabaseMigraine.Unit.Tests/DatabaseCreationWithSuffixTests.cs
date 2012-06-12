using System;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseMigraine.Unit.Tests
{
	[TestFixture]
	public class DatabaseCreationWithSuffixTests
	{
		protected static DisposableDbManager _disposableDbManager;
		private Server _disposableDbServer;
		const string REST_OF_CONNECTION_STRING = "Trusted_Connection=true;Min Pool Size=5;Max Pool Size=200;";
		const string DB_HOST_NAME = "(local)";
		private const string DB_NAME = "foo";


		[SetUp]
		public void SetUpDisposableServer()
		{
			const string dbCreationPath = @"C:\SQLData";
			_disposableDbServer = ConnectionHelper.Connect(DB_HOST_NAME, REST_OF_CONNECTION_STRING);
			_disposableDbManager = new DisposableDbManager(dbCreationPath, _disposableDbServer, DB_NAME);
		}

		[Test]
		public void Create_test_dbs_with_suffix()
		{
			string someSuffix = "some_suffix";
			
			string completeDisposableDb = _disposableDbManager.CreateCompleteDisposableDb(someSuffix);

			string expectedRegex = string.Format(@"^DELETEME_UNITTEST_{0}_(\d\d\d\d\d\d\d\d)_(\d\d\d\d\d\d)_{1}$", DB_NAME, someSuffix);

			var regex = new Regex(expectedRegex);

			Console.WriteLine(completeDisposableDb);
			Assert.That(regex.Match(completeDisposableDb).Captures.Count, Is.EqualTo(1));
		}

		[Test]
		public void Should_calculate_date_from_dbs_with_suffixes()
		{
			string dbNameWithSuffix = "DELETEME_UNITTEST_foo_20120227_114311_some_suffix";
			Database db = new Database(_disposableDbServer, dbNameWithSuffix);

			DateTime date = DisposableDbManager.GetDateFromDisposableDbName(db);

			Assert.That(date, Is.Not.Null);
		}
	}
}