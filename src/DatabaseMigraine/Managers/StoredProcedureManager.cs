using System.Text.RegularExpressions;

namespace DatabaseMigraine.Managers
{
	public class StoredProcedureManager : UpdatableManager
	{
		public override string FolderName { get { return "06_StoredProcedures"; } }

		private static volatile StoredProcedureManager _instance;
		private static readonly object SyncRoot = new object();
		public static StoredProcedureManager Instance
		{
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new StoredProcedureManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private StoredProcedureManager() { }

		protected override string ReplaceCreateWithAlter(string scriptContents)
		{
			var scriptContentsWithAlter = Regex.Replace(scriptContents, @"CREATE\s+PROCEDURE", "ALTER PROCEDURE", RegexOptions.IgnoreCase);
			scriptContentsWithAlter = Regex.Replace(scriptContentsWithAlter, @"CREATE\s+PROC", "ALTER PROC", RegexOptions.IgnoreCase);
			return scriptContentsWithAlter;
		}
	}
}
