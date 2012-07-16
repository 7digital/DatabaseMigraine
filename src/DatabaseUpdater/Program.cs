using System;
using System.Collections.Generic;
using System.IO;
using DatabaseMigraine;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseUpdater
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
				Console.Error.WriteLine("--dbpath points to a directory that does not exist");
				Environment.Exit((int)DbParams.ExitStatus.ParameterNotValid);
			}

			try
			{
				var server = ConnectionHelper.Connect(dbParams.DbHostName, dbParams.DbConnString);
				UpdateDatabaseWithScriptsIn(server, dbParams);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("An exception occurred: " + e);
				Environment.Exit((int)DbParams.ExitStatus.ExecutionException);
			}

			Environment.Exit((int)DbParams.ExitStatus.Ok);
		}

		private static void UpdateDatabaseWithScriptsIn(Server server, DbParams dbParams)
		{
			Console.WriteLine("Attempting to update DB {0} with scripts in {1}", dbParams.DbName, dbParams.DbPath);
			int scriptCount = 0;
			var dbPathTrimmed = dbParams.DbPath.FullName.TrimEnd('/').TrimEnd('\\').ToLower();

			var managers = ChiefExecutive.GetAllUpdatableManagers();

			foreach(var manager in managers)
			{
				if (dbPathTrimmed.EndsWith(manager.FolderName.ToLower()))
				{
					scriptCount = manager.RunScriptsToUpdate(server, dbParams.DbPath.FullName, dbParams.DbName);
					break;
				}
			}
			if (scriptCount == 0)
			{
				foreach (var manager in managers)
				{
					string possibleDirIfSuppliedDbRootDir = Path.Combine(dbPathTrimmed, manager.FolderName);
					if (Directory.Exists(possibleDirIfSuppliedDbRootDir))
						scriptCount += manager.RunScriptsToUpdate(server, possibleDirIfSuppliedDbRootDir, dbParams.DbName);
				}
			}
			Console.WriteLine("Successfully executed {0} scripts", scriptCount);
		}
	}
}
