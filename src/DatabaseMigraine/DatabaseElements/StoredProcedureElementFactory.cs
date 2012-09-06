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
				if (IsSystemObject(storedProcedure))
					continue;

				yield return new StoredProcedureElement(storedProcedure);
			}
			yield break;
		}

		private static bool IsSystemObject(StoredProcedure storedProcedure)
		{
			try
			{
				if (storedProcedure.IsSystemObject)
				{
					return true;
				}
			}
			catch (UnknownPropertyException)
			{
			}
			return false;
		}
	}
}