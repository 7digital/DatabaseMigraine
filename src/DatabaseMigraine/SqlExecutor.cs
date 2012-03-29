using System;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine
{
	public class SqlExecutor
	{
		private readonly Server _disposableDbServer;

		public SqlExecutor(Server disposableDbServer)
		{
			_disposableDbServer = disposableDbServer;
		}

		internal void ExecuteNonQuery(string script)
		{
			ExecuteNonQueryInternal(script, "master");
		}

		public void ExecuteNonQuery(string script, string dbNameToUse) {
			if (String.IsNullOrEmpty(dbNameToUse))
				throw new ArgumentNullException("dbNameToUse");

			if (dbNameToUse.ToLower() == "master")
				throw new InvalidArgumentException("Don't use the optional second argument if you don't need to (instead of using 'master')");

			script = ReplaceDbnameInScript(script, dbNameToUse);

			ExecuteNonQueryInternal(script, dbNameToUse);
		}

		internal static string ReplaceDbnameInScript(string scriptContent, string dbNameToUse)
		{
			//workaround to the problem that there are params named "dbname"
			scriptContent = scriptContent.Replace("@dbname", "@dbparamname");

			scriptContent = scriptContent.Replace("dbname", dbNameToUse);

			//restore (look at workaround above)
			scriptContent = scriptContent.Replace("@dbparamname", "@dbname");
			return scriptContent;
		}

		private void ExecuteNonQueryInternal(string script, string dbNameToUse)
		{
			var scriptWithUseDb = string.Format(@"use {0}{2}GO{2}{1}", dbNameToUse, script, Environment.NewLine);
			try
			{
				_disposableDbServer.ConnectionContext.ExecuteNonQuery(scriptWithUseDb);
			}
			catch (Exception)
			{
				Console.WriteLine("Error while executing:{0}{1}", Environment.NewLine, scriptWithUseDb);
				throw;
			}
		}

		internal SqlDataReader ExecuteQuery(string script, string dbNameToUse)
		{
			if (String.IsNullOrEmpty(dbNameToUse))
				throw new ArgumentNullException("dbNameToUse");

			if (dbNameToUse.ToLower() == "master")
				throw new InvalidArgumentException("Don't use the optional second argument if you don't need to (instead of using 'master')");

			return ExecuteQueryInternal(script, dbNameToUse);
		}

		private SqlDataReader ExecuteQueryInternal(string script, string dbNameToUse)
		{
			var scriptWithUseDb = string.Format(@"use {0}{2}{1}", dbNameToUse, script, Environment.NewLine);
			try
			{
				return _disposableDbServer.ConnectionContext.ExecuteReader(scriptWithUseDb);
			}
			catch (Exception)
			{
				Console.WriteLine("Error while executing {1}'{0}'", scriptWithUseDb, Environment.NewLine);
				throw;
			}
		}
	}
}