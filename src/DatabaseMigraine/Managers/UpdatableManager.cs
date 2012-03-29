using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.Managers
{
	public abstract class UpdatableManager : DbScriptFolderManager
	{
		protected abstract string ReplaceCreateWithAlter(string scriptContents);

		public int RunScriptsToUpdate(Server disposableDbServer, string scriptsFolderPath, string dbname)
		{
			SqlExecutor = new SqlExecutor(disposableDbServer);
			if (!scriptsFolderPath.TrimEnd('/').TrimEnd('\\').EndsWith(FolderName))
			{
				var type = GetType();
				var suffixPos = type.Name.IndexOf("Manager");
				if (suffixPos < 1) {
					throw new InvalidOperationException("Derived classes of UpdatableManager should have the -Manager suffix as well");
				}
				var scriptType = type.Name.Substring(0, suffixPos);
				throw new ArgumentException(String.Format("scriptsFolderPath must contain {0}s", scriptType),
					"scriptsFolderPath");
			}

			int count = 0;
			IEnumerable<string> scriptPaths = Directory.GetFileSystemEntries(scriptsFolderPath, "*.sql");
			foreach (var scriptPath in scriptPaths)
			{
				var scriptFilename = Path.GetFileName(scriptPath);

				var scriptContents = File.ReadAllText(scriptPath);

				var scriptContentsWithAlter = ReplaceCreateWithAlter(scriptContents);

				try{
					Run(scriptContentsWithAlter, dbname, scriptFilename);
				} catch {
					//maybe ALTER failed because the element is still not there...
					Run(scriptContents, dbname, scriptFilename);
				}
				count++;
			}
			return count;
		}

		private void Run(string scriptContentsWithAlter, string dbname, string scriptFilename)
		{
			try
			{
				SqlExecutor.ExecuteNonQuery(scriptContentsWithAlter, dbname);
				Console.WriteLine("Successfully run " + scriptFilename);
			}
			catch (ExecutionFailureException e)
			{
				throw new Exception(String.Format("Error running the script with name '{0}'", scriptFilename), e);
			}
		}
	}
}
