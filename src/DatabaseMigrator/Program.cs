using System;
using System.IO;
using System.Linq;
using DatabaseMigraine;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigrator
{
	class Program
	{
		static DirectoryInfo _outPath;

		static void Main(string[] args)
		{
			DbParams dbParams = null;
			try {
				dbParams = new DbParams(args);
			} catch (NoParamsException)
			{
				Console.WriteLine("--outpath:/some/path/where/your/migrations/will/be/generated/");
				Environment.Exit((int)DbParams.ExitStatus.ParametersNotSupplied);
			} catch (SomeParamsMissingException e)
			{
				Console.WriteLine(e.ParamNames[0] + " is mandatory");
				Environment.Exit((int)DbParams.ExitStatus.SomeParameterNotSupplied);
			}

			GetOutPutPathArg(args);

			if (!dbParams.DbPath.Exists)
			{
				Console.Error.WriteLine("--dbpath points to a directory that does not exist");
				Environment.Exit((int)DbParams.ExitStatus.ParameterNotValid);
			}

			try {
				GenerateMigrations(ConnectionHelper.Connect(dbParams.DbHostName, dbParams.DbConnString), dbParams);
			} catch (Exception e)
			{
				Console.Error.WriteLine("An exception occurred: " + e);
				Environment.Exit((int)DbParams.ExitStatus.ExecutionException);
			}

			Environment.Exit((int)DbParams.ExitStatus.Ok);
		}

		private static void GetOutPutPathArg(string[] args)
		{
			string outPathArg = args.Where(arg => arg.StartsWith("--outpath:")).FirstOrDefault();
			string outPath = outPathArg != null ? 
				outPathArg.Substring("--outpath:".Length) : Directory.GetCurrentDirectory();

			if (File.Exists(outPath) && !Directory.Exists(outPath))
			{
				throw new InvalidOperationException("Path specified in --outputpath: is a file, not a directory");
			}

			if (!Directory.Exists(outPath))
			{
				Console.WriteLine("Creating outputpath directory...");
				_outPath = Directory.CreateDirectory(outPath);
				Console.WriteLine("Done: " + _outPath.FullName);
			} else {
				_outPath = new DirectoryInfo(outPath);
			}
			Console.WriteLine("Migrations will be generated in: " + _outPath.FullName);
		}

		static void GenerateMigrations(Server server, DbParams dbParams)
		{
			Console.WriteLine("Attempting to generate migrations for {0}...", dbParams.DbName);
			int migrationCount = new MigrationManager()
				.GenerateMigrations(dbParams.DbPath, dbParams.DbName, server, _outPath);
			Console.WriteLine("Successfully generated {0} migrations", migrationCount);
		}
	}
}
