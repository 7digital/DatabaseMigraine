using System.Text.RegularExpressions;

namespace DatabaseMigraine.Managers
{
	public class FunctionManager : UpdatableManager
	{
		public override string FolderName { get { return "04_Functions"; } }

		private static volatile FunctionManager _instance;
		private static readonly object SyncRoot = new object();
		public static FunctionManager Instance {
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new FunctionManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private FunctionManager() { }

		protected override string ReplaceCreateWithAlter(string scriptContents)
		{
			return Regex.Replace(scriptContents, @"CREATE\s+FUNCTION", "ALTER FUNCTION", RegexOptions.IgnoreCase);
		}
	}

}
