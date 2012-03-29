using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatabaseMigraine;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseScriptRunner
{
	class Program
	{
		static void Main(string[] args)
		{
			DbParams dbParams = null;
			try
			{
				dbParams = new DbParams(args);
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

			if (!dbParams.DbPath.Exists)
			{
				Console.Error.WriteLine("--dbpath points to a directory that does not exist: " + dbParams.DbPath.FullName);
				Environment.Exit((int)DbParams.ExitStatus.ParameterNotValid);
			}

			var sqlFiles = Directory.GetFileSystemEntries(dbParams.DbPath.FullName, "*.sql");
			if (!sqlFiles.Any())
			{
				Console.Error.WriteLine("--dbpath points to a directory that does not contain any .sql files");
				Environment.Exit((int)DbParams.ExitStatus.ParameterNotValid);
			}

			try
			{
				RunScriptsInForDatabase(ConnectionHelper.Connect(dbParams.DbHostName, dbParams.DbConnString), dbParams, sqlFiles);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("An exception occurred: " + e);
				Environment.Exit((int)DbParams.ExitStatus.ExecutionException);
			}

			Environment.Exit((int)DbParams.ExitStatus.Ok);
		}

		private static void RunScriptsInForDatabase(Server server, DbParams dbParams, IEnumerable<string> sqlFiles)
		{
			var executor = new SqlExecutor(server);

			var sqlFilesSortedAlphabetically = Order(sqlFiles);

			foreach(var sqlFile in sqlFilesSortedAlphabetically) {
				Console.WriteLine("Going to run " + sqlFile);
				executor.ExecuteNonQuery(File.ReadAllText(sqlFile), dbParams.DbName);
			}
		}

		private static string[] Order(IEnumerable<string> fileSystemEntries)
		{
			try
			{
				// take in account that "2-someMigration" should go before "11-someOtherMigration"
				return fileSystemEntries.OrderBy(x => MigrationManager.GetMigrationId(Path.GetFileNameWithoutExtension(x))).ToArray();
			}
			catch (FormatException)
			{
				return fileSystemEntries.OrderBy(x => x).ToArray();
			}
		}
	}
}
