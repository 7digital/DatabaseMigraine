using System;
using System.Configuration;
using System.IO;
using DatabaseMigraine;

namespace DatabaseCreator
{
	class Program
	{
		//TODO: change this to use DbParams instead of an app.config file:
		static readonly string DbCreationPath = ConfigurationManager.AppSettings["DbCreationPath"];
		static readonly string DisposableDbConnString = ConfigurationManager.AppSettings["DisposableDbConnString"];
		static readonly string DisposableDbHostname = ConfigurationManager.AppSettings["DisposableDbHostname"];

		static void Main(string[] args)
		{
			if(args.Length < 1 || args.Length > 2 || (args.Length == 2 && !args[1].Contains("--config:")))
			{
				Console.WriteLine("Please supply the name of database(s) you wish to create.");
				Console.WriteLine();
				Console.WriteLine("Optionally in the 2nd argument, specify a config file to sabotage with:");
				Console.WriteLine("--config:/path/to/config.file");
				Console.WriteLine("To Exit Press Any Key");
				Console.Read();
				Environment.Exit(0);
			}

			string configFile = null;
			if (args.Length > 1)
			{
				string path = args[1].Substring(args[1].IndexOf(":") + 1);
				if (!File.Exists(path))
				{
					throw new FileNotFoundException(path);
				}
				configFile = path;
			}

			string disposableDbName = CreateDatabase(args[0]);

			if (!String.IsNullOrEmpty(configFile))
			{
				ConfigFileSaboteur.Sabotage(configFile, args[0], disposableDbName);
			}
			Environment.Exit(0);
		}

		private static string CreateDatabase(string dbNameInVcs)
		{
			var disposableDbServer = ConnectionHelper.Connect(DisposableDbHostname, DisposableDbConnString);

			var disposableDbCreator = new DisposableDbManager(DbCreationPath, disposableDbServer, dbNameInVcs);

			string disposableDbName = disposableDbCreator.CreateCompleteDisposableDb();
			Console.WriteLine(disposableDbName);
			return disposableDbName;
		}
	}
}
