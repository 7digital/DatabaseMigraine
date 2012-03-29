using System.Text.RegularExpressions;

namespace DatabaseMigraine.Managers
{
	public class TriggerManager : UpdatableManager
	{
		public override string FolderName { get { return "03_Triggers"; } }

		private static volatile TriggerManager _instance;
		private static readonly object SyncRoot = new object();
		public static TriggerManager Instance
		{
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new TriggerManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private TriggerManager() { }

		protected override string ReplaceCreateWithAlter(string scriptContents)
		{
			return Regex.Replace(scriptContents, @"CREATE\s+TRIGGER", "ALTER TRIGGER", RegexOptions.IgnoreCase);
		}
	}
}
