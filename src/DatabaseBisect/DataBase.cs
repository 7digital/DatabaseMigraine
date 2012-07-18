using System;
using System.Data;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public class DataBase : IDataBase
	{
		private readonly Database _db;
		public DataBase(Database database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			_db = database;
		}

		public string Name
		{
			get { return _db.Name; }
		}

		public TableCollection Tables
		{
			get { return _db.Tables; }
		}

		public DataSet ExecuteWithResults(string sqlCommand)
		{
			return _db.ExecuteWithResults(sqlCommand);
		}

		public void ExecuteNonQuery(string sqlCommand)
		{
			_db.ExecuteNonQuery(sqlCommand);
		}
	}
}