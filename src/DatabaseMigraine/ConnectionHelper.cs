using System;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine
{
	public class ConnectionHelper
	{
		public static Server Connect(string dbhostname, string restOfConnString)
		{
			if (String.IsNullOrEmpty(dbhostname))
			{
				throw new ArgumentNullException("dbhostname");
			}
			if (String.IsNullOrEmpty(restOfConnString))
			{
				throw new ArgumentNullException("restOfConnString");
			}

			if (!restOfConnString.EndsWith(";"))
			{
				throw new ArgumentException(
					String.Format("Please end the connection string with a semicolon ';': <{0}>", restOfConnString),
					"restOfConnString");
			}

			var connectionString = String.Format("{0}Server={1};", restOfConnString, dbhostname);
			var sqlConnection = new SqlConnection(connectionString);
			var serverConnection = new ServerConnection(sqlConnection);
			return new Server(serverConnection);
		}
	}
}
