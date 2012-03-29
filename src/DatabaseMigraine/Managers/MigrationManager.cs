using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.Managers
{
	public class MigrationManager : DbScriptFolderManager
	{
		public override string FolderName { get { return "09_Migrations"; } }

		private const int TransactionNameMaxLength = 30;
		private const string SqlSuffix = ".sql";
		private const string DatabaseChangeLogTableName = "DatabaseChangeLog";

		internal const string DatabaseChangeLogScriptName = DatabaseChangeLogTableName + SqlSuffix;

		public class Migration
		{
			public string FileNameWithoutExtension { get; set; }
			public DateTime AppliedDate { get; set; }
		}

		private class GeneratedMigration
		{
			internal int Id { get; set; }
			internal string ScriptContent { get; set; }
			internal string TransactionName { get; set; }
		}

		public IEnumerable<Migration> GetMigrationsRunInDb(DirectoryInfo dbScriptsPath, Server dbServer, string dbname)
		{
			var migrationSqlFilesInVcs = GetSqlSriptsIn(dbScriptsPath.FullName);
			if (!migrationSqlFilesInVcs.Any())
				yield break;

			var sqlExecutor = new SqlExecutor(dbServer);
			CheckIfDatabaseChangeLogTableExists(sqlExecutor, dbname);

			//FIXME: use hashes, not filenames!
			using (SqlDataReader reader = sqlExecutor.ExecuteQuery(String.Format(
				"SELECT FileName,Applied FROM {0}", DatabaseChangeLogTableName), dbname)){
				while (reader.Read())
				{
					var migration = new Migration();
					var filename = (string) reader["FileName"];
					if (!filename.EndsWith(SqlSuffix))
						throw new Exception(
							String.Format("Expecting filename ending with '{0}' but found '{1}' in {2}'s {3} table",
								SqlSuffix, filename, dbServer.Name, DatabaseChangeLogTableName));
					migration.FileNameWithoutExtension = filename.Substring(0, filename.Length - SqlSuffix.Length);
					migration.AppliedDate = (DateTime) reader["Applied"];
					yield return migration;
				}
			}
		}

		public IEnumerable<Migration> GetNonRetiredMigrationsRunInDb(DirectoryInfo dbScriptsPath, Server dbServer, string dbname)
		{
			var migrations = GetMigrationsRunInDb(dbScriptsPath, dbServer, dbname);

			if (!migrations.Any())
			{
				yield break;
			}

			var migrationFolder = Path.Combine(dbScriptsPath.FullName, FolderName);

			foreach (Migration migration in migrations)
			{
				var possibleMigrationFile = Path.Combine(migrationFolder, migration.FileNameWithoutExtension + ".sql");
				if (File.Exists(possibleMigrationFile))
				{
					yield return migration;
				}
			}
		}

		public override int RunScripts(Server disposableDbServer,
		                               string dbScriptsPath,
		                               string dbname,
		                               string originalDbName,
		                               IEnumerable<string> scriptFileNameWhiteList)
		{
			SqlExecutor = new SqlExecutor(disposableDbServer);
			if (GetSqlSriptsIn(dbScriptsPath).Any())
				CheckIfDatabaseChangeLogTableExists(SqlExecutor, dbname);
			return base.RunScripts(disposableDbServer, dbScriptsPath, dbname, originalDbName, scriptFileNameWhiteList);
		}

		public override IEnumerable<FileInfo> GetSqlSriptsIn(string dbScriptsPath)
		{
			var scripts = base.GetSqlSriptsIn(dbScriptsPath);
			return scripts.OrderBy(file => GetMigrationId(file.FullName));
		}

		protected override IDictionary<string, string> RetrieveContentsWhileCheckConventions(string originalDbName, IEnumerable<FileInfo> scripts)
		{
			var elementNameToScriptContents = new Dictionary<string, string>();

			foreach (var script in scripts)
			{
				string scriptContents = File.ReadAllText(script.FullName);
				elementNameToScriptContents.Add(Path.GetFileNameWithoutExtension(script.FullName), scriptContents);
			}

			CheckContentViolationRules(elementNameToScriptContents, originalDbName);

			return elementNameToScriptContents;
		}

		protected override void RunScript(KeyValuePair<string, string> script, string dbname)
		{
			if (SqlExecutor == null)
			{
				throw new InvalidOperationException("SqlExecutor should not be null when heading to run the script");
			}

			var migration = GenerateMigration(script.Key, script.Value);
			base.RunScript(new KeyValuePair<string, string>(script.Key, migration.ScriptContent), dbname);
		}

		protected override void CheckContentViolationRules(Dictionary<string, string> elementNameToScriptContents, string originalDbName)
		{
			CheckDbNameHardcodingViolation(elementNameToScriptContents, originalDbName);
			CheckForRiskyBatchStatementGO(elementNameToScriptContents);
		}

		private void CheckForRiskyBatchStatementGO(IDictionary<string, string> scriptsWithContents)
		{
			var violators = new List<string>();
			var firstViolatorContent = string.Empty;
			foreach (var script in scriptsWithContents)
			{
				string contents = script.Value.ToLower();

				if (new Regex(@"\sgo\s").Matches(contents).Count > 0)
				{
					violators.Add(script.Key);
					if (String.IsNullOrEmpty(firstViolatorContent))
					{
						firstViolatorContent = contents;
					}
				}
			}
			if (violators.Count > 0)
			{
				throw new ContentViolationException(
					violators,
					"The following scripts contain the batch statement 'GO' which is risky for migrations because they are contained in transactions and thus can lock system tables:",
					firstViolatorContent);
			}
		}

		private static GeneratedMigration GenerateMigration(string scriptFileNameWithoutExtension, string scriptContents)
		{
			int migrationId = GetMigrationId(scriptFileNameWithoutExtension);
			string hash = GenerateHash(scriptContents);

			string transactionName = FileNameToTransactionName(scriptFileNameWithoutExtension);

			var migration = new GeneratedMigration
			{
				Id = migrationId,
				TransactionName = transactionName,
				ScriptContent = String.Format(@"
BEGIN TRY
  BEGIN TRANSACTION {0}
    EXEC('{1}')
    {2}
  COMMIT TRANSACTION {0}
END TRY
BEGIN CATCH
  IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION {0}
  
  -- Raise an error with the details of the exception
  DECLARE @ErrMsg nvarchar(4000), @ErrSeverity int
  SELECT @ErrMsg = ERROR_MESSAGE(), @ErrSeverity = ERROR_SEVERITY()
  RAISERROR(@ErrMsg, @ErrSeverity, 1)
END CATCH
",
				transactionName,
				EscapeSql(scriptContents),
				GetInsertStatementForMigration(migrationId, scriptFileNameWithoutExtension, hash))
			};

			return migration;
		}

		private static string GetInsertStatementForMigration(int migrationId, string scriptFileNameWithoutExtension, string hash)
		{
			return String.Format("INSERT INTO {0}(ChangeLogId,FileName,Hash) VALUES({1},'{2}{3}','{4}')",
				DatabaseChangeLogTableName, migrationId, scriptFileNameWithoutExtension, SqlSuffix, EscapeSql(hash));
		}

		private static string EscapeSql (string content)
		{
			return content.Replace("'", "''");
		}

		private static string FileNameToTransactionName (string scriptFileNameWithoutExtension)
		{
			string transactionName = scriptFileNameWithoutExtension.Substring(scriptFileNameWithoutExtension.IndexOf("-") + 1)
				.Replace("-", "_").Replace(' ', '_');
			if (transactionName.Length > TransactionNameMaxLength)
			{
				transactionName = transactionName.Substring(0, TransactionNameMaxLength);
			}
			return transactionName;
		}

		private static GeneratedMigration GenerateMigrationFor(SqlExecutor executor, string scriptFileNameWithoutExtension, string scriptContents, string dbname)
		{
			if (executor != null && MigrationAlreadyRun(executor, GenerateHash(scriptContents), dbname))
			{
				return null;
			}
			return GenerateMigration(scriptFileNameWithoutExtension, scriptContents);
		}

		internal static string GetInsertStatementForMigration(FileInfo script)
		{
			var migrationFileNameWithoutExtension = Path.GetFileNameWithoutExtension(script.FullName);
			return GetInsertStatementForMigration(GetMigrationId(migrationFileNameWithoutExtension), 
			                                      migrationFileNameWithoutExtension,
			                                      GenerateHash(File.ReadAllText(script.FullName)));
		}

		public int GenerateMigrationsContentsToDisk (IEnumerable<FileInfo> scripts, DirectoryInfo outPath, string dbname)
		{
			var migrationsGenerated = new HashSet<int>();
			foreach (var script in scripts)
			{
				var migrationName = Path.GetFileNameWithoutExtension(script.FullName);
				var migrationToRun = GenerateMigrationFor(SqlExecutor, migrationName, File.ReadAllText(script.FullName), dbname);
				if (migrationToRun == null || String.IsNullOrEmpty(migrationToRun.ScriptContent))
				{
					Console.WriteLine("Skipping migration {0} because it's already run in this environment", migrationName);
					continue;
				}

				//TODO: check if the ID is not used in the History/ subfolder either
				if (migrationsGenerated.Contains(migrationToRun.Id))
				{
					throw new InvalidOperationException("There are two or more migrations that have the same Id: " + migrationToRun.Id);
				}

				var destination = Path.Combine(outPath.FullName, script.Name);
				File.WriteAllText(destination, migrationToRun.ScriptContent);
				migrationsGenerated.Add(migrationToRun.Id);
			}
			return migrationsGenerated.Count;
		}

		public static int GetMigrationId(string scriptFileNameWithoutExtension)
		{
			var regex = new Regex(@"(\d+)-(.+)");
			var match = regex.Match(scriptFileNameWithoutExtension);
			if (!match.Success) {
				throw new FormatException("Filename should have this format: 0123-TheDescriptionOfYourMigration.sql");
			}

			return int.Parse(match.Groups[1].Value);
		}

		public int GenerateMigrations(DirectoryInfo dbScriptsPath, string dbname, Server server, DirectoryInfo outPath)
		{
			SqlExecutor = new SqlExecutor(server);
			CheckIfDatabaseChangeLogTableExists(SqlExecutor, dbname);

			Console.WriteLine("Looking for migrations in {0}", dbScriptsPath);

			IEnumerable<FileInfo> scripts = GetSqlSriptsIn(dbScriptsPath.FullName);
			
			Console.WriteLine("Found {0} migrations", scripts.Count());

			return GenerateMigrationsContentsToDisk(scripts, outPath, dbname);
		}

		private static void CheckIfDatabaseChangeLogTableExists(SqlExecutor sqlExecutor, string dbname) {

			//FIXME: use Smo API instead of sys.tables, to check this
			using (var result = sqlExecutor.ExecuteQuery(String.Format(
				"SELECT * FROM sys.tables WHERE name = '{0}'", DatabaseChangeLogTableName), dbname))
			{
				if (!result.HasRows)
				{
					string msg = String.Format("Table {0} not found in your database, please run this script on it:", DatabaseChangeLogTableName);
					string script = String.Format(@"
CREATE TABLE [dbo].[{0}](
	[ChangeLogId] [INT] NOT NULL,
	[FileName] [NVARCHAR](256) NOT NULL,
	[Hash] [NVARCHAR](50) NOT NULL,
	[Applied] [DATETIME] NOT NULL,
	[AppliedBy] [NVARCHAR](50) NOT NULL,
	CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED
	(
		[ChangeLogId] ASC
	) WITH (
		PAD_INDEX  = OFF,
		STATISTICS_NORECOMPUTE  = OFF,
		IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON,
		ALLOW_PAGE_LOCKS  = ON
	) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[{0}]
	ADD CONSTRAINT [DF_{0}_Applied] DEFAULT (GETDATE()) FOR [Applied]
GO

ALTER TABLE [dbo].[{0}]
	ADD CONSTRAINT [DF_{0}_AppliedBy] DEFAULT (SYSTEM_USER) FOR [AppliedBy]
GO
", DatabaseChangeLogTableName);

					Console.Error.WriteLine(msg);
					Console.Error.WriteLine(script);
					Console.Error.WriteLine();

					throw new InvalidOperationException(msg + script);
				}
			}
		}

		private static bool MigrationAlreadyRun(SqlExecutor executor, string hash, string dbname)
		{
			string sqlHashChecker = String.Format("SELECT 'x' FROM {0} WHERE Hash = '{1}'", DatabaseChangeLogTableName, hash.Replace("'", "''"));
			using (var result = executor.ExecuteQuery(sqlHashChecker, dbname))
			{
				return result.HasRows;
			}
		}

		private static string GenerateHash(string scriptContents)
		{
			byte[] hashBytes = new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(scriptContents));
			return Convert.ToBase64String(hashBytes);
		}
	}
}
