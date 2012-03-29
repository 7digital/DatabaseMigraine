using System.Collections.Generic;
using System.Configuration;
using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class DisposableDbTestBase
	{
		[TestFixtureSetUp]
		public void SetupDisposableDb()
		{
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var mappings = MapDatabaseNamesToConnectionStringName.Mappings;

			foreach (var mapping in mappings)
			{
				var disposableDbConnString = CreateDisposableDb(mapping.Key);

				foreach (var connectionString in mapping.Value)
				{
					ReplaceConnString(config, connectionString, disposableDbConnString);
				}
			}

			config.Save();

			ConfigurationManager.RefreshSection("appSettings");
			ConfigurationManager.RefreshSection("connectionStrings");
		}

		protected abstract string ApplicationNameForConnectionString { get; }
		protected abstract DisposableDbMapping MapDatabaseNamesToConnectionStringName { get; }

		protected class DisposableDbMapping
		{
			private readonly IDictionary<string, string[]> _mappings = new Dictionary<string, string[]>();
			public IDictionary<string, string[]> Mappings
			{
				get { return _mappings; }
			}

			private string _databaseName;

			public DisposableDbMapping UseDisposableDbNamed(string databaseName)
			{
				_databaseName = databaseName;
				return this;
			}

			public DisposableDbMapping ForConnectionStringKeys(params string[] connectionStringKeys)
			{
				Mappings.Add(_databaseName, connectionStringKeys);
				return this;
			}
		}

		private string CreateDisposableDb(string databaseName)
		{
			var restOfConnString = ConfigurationManager.AppSettings["DisposableDbConnString"] + ";Application Name=" + ApplicationNameForConnectionString + ";";
			var dbhostname = ConfigurationManager.AppSettings["DisposableDbHostname"];
			var dbCreationPath = ConfigurationManager.AppSettings["DbCreationPath"];

			var server = ConnectionHelper.Connect(dbhostname, restOfConnString);

			var disposableDbManager = new DisposableDbManager(dbCreationPath, server, databaseName);
			var disposableDbName = disposableDbManager.CreateCompleteDisposableDb();

			return string.Format("Initial Catalog={0};Data Source={1};{2}", disposableDbName, dbhostname, restOfConnString);
		}

		private static void ReplaceConnString(Configuration config, string connstringName, string connStringValue)
		{
			config.AppSettings.Settings.Remove(connstringName);
			config.AppSettings.Settings.Add(connstringName, connStringValue);

			config.ConnectionStrings.ConnectionStrings.Remove(connstringName);
			config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings(connstringName, connStringValue));
		}
	}
}