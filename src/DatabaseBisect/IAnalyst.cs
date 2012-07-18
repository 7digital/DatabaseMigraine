using Microsoft.SqlServer.Management.Smo;

namespace DatabaseBisect
{
	public interface IAnalyst
	{
		Table ChooseTableToBisect(IDataBase db);
	}
}