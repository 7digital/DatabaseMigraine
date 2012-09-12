using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class StoredProcedureElementFactory : IScriptableWrapperFactory<StoredProcedureElement>
	{
		public IEnumerable<StoredProcedureElement> Scan(Database db)
		{
			foreach (StoredProcedure storedProcedure in db.StoredProcedures)
			{
				if (storedProcedure.IsSystemObject)
				{
					continue;
				}
				yield return new StoredProcedureElement(storedProcedure);
			}
			yield break;
		}
	}
}