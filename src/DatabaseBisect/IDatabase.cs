using System.Data;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public interface IDataBase
	{
		string Name { get; }
		TableCollection Tables { get; }
		DataSet ExecuteWithResults(string sqlCommand);
		void ExecuteNonQuery(string sqlCommand);
	}
}