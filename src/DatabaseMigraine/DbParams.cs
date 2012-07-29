using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DatabaseMigraine
{
	public class DbParams
	{
		public enum ExitStatus
		{
			Ok = 0,
			ParametersNotSupplied = 1,
			SomeParameterNotSupplied = 2,
			ParameterNotValid = 3,
			ExecutionException = 4
		}

		public string DbHostName { get; private set; }
		public string DbConnString { get; private set; }
		public string DbName { get; private set; }
		public DirectoryInfo DbPath { get; private set; }

		public DbParams(IEnumerable<string> args)
		{
			if (!args.Any())
			{
				Console.WriteLine("Possible parameters are:");
				Console.WriteLine("--dbhostname:some_host");
				Console.WriteLine("--dbconnstring:\"User Id=someUserName;Password=somePassWord;Min Pool Size=5;");
				Console.WriteLine("--dbname:some_db_name");
				Console.WriteLine("--dbpath:/some/path/to/where/your/sql/files/are");
				throw new NoParamsException();
			}

			foreach (string arg in args)
			{
				if (arg.StartsWith("--dbhostname:"))
				{
					DbHostName = arg.Substring("--dbhostname:".Length).Trim();
				}

				if (arg.StartsWith("--dbconnstring:"))
				{
					DbConnString = arg.Substring("--dbconnstring:".Length).Trim();
				}

				if (arg.StartsWith("--dbname:"))
				{
					DbName = arg.Substring("--dbname:".Length).Trim();
				}

				if (arg.StartsWith("--dbpath:"))
				{
					DbPath = new DirectoryInfo(arg.Substring("--dbpath:".Length).Trim());
				}
			}

			var missingParams = new List<string>();

			if (String.IsNullOrEmpty(DbHostName))
			{
				missingParams.Add("--dbhostname");
			}

			if (String.IsNullOrEmpty(DbName))
			{
				missingParams.Add("--dbname");
			}

			if (DbPath == null)
			{
				missingParams.Add("--dbpath");
			}

			if (!String.IsNullOrEmpty(DbConnString)) {
				if (DbConnString.ToLower().Contains("catalog"))
				{
					throw new InvalidParamException(
						"Don't include 'Initial Catalog' in the connection string, it is the --dbname parameter");
				}

				if (DbConnString.ToLower().Contains("source"))
				{
					throw new InvalidParamException(
						"Don't include 'Data Source' in the connection string, it is the --dbhostname parameter");
				}
			}

			if (missingParams.Count > 0)
			{
				throw new SomeParamsMissingException(missingParams.ToArray());
			}
		}
	}

	public class SomeParamsMissingException : Exception
	{
		public string[] ParamNames { get; private set; }
		public SomeParamsMissingException(string paramName)
		{
			ParamNames = new [] { paramName };
		}

		public SomeParamsMissingException(string[] paramNames)
		{
			ParamNames = paramNames;
		}
	}

    [Serializable]
	public class NoParamsException : Exception
	{
	}

    [Serializable]
    public class InvalidParamException : Exception
	{
		public InvalidParamException(string message): base (message)
		{
		}
	}
}
