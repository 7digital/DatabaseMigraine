using System.Collections.Generic;

namespace DatabaseMigraine.Managers
{
	public class ChiefExecutive
	{
		public static HashSet<DbScriptFolderManager> GetExposedElementManagers()
		{
			return new HashSet<DbScriptFolderManager> {
				TableManager.Instance,
				ViewManager.Instance,
				FunctionManager.Instance,
				StoredProcedureManager.Instance,
			};
		}

		public static HashSet<UpdatableManager> GetAllUpdatableManagers()
		{
			return new HashSet<UpdatableManager> {
				ViewManager.Instance,
				FunctionManager.Instance,
				TriggerManager.Instance,
				StoredProcedureManager.Instance,
			};
		}
	}
}