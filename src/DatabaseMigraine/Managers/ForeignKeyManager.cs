using System;
using System.IO;

namespace DatabaseMigraine.Managers
{
	public class ForeignKeyManager : DbScriptFolderManager
	{
		public override string FolderName { get { return "02_ForeignKeys"; } }

		private static volatile ForeignKeyManager _instance;
		private static readonly object SyncRoot = new object();
		public static ForeignKeyManager Instance {
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new ForeignKeyManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private ForeignKeyManager() { }

		protected override void RemoveOldScripts<T>(string subfolderScripts, ChangeSet<T> changeSet)
		{
			foreach (var removed in changeSet.Removed)
			{
				string file = Path.Combine(subfolderScripts, removed.Value.FileName);
				Console.WriteLine("Updating (to remove part of it) " + removed.Value.FileName);

				if (!File.Exists(file))
				{
					//this may be because the whole table is removed, then:
					// the UpdateModifiedScripts() call over the TableChanges already
					// removed the script, so: nothing to do
					continue;
				}

				string contentToRemove = removed.Value.ScriptFileContents.Trim();
				string currentContent = File.ReadAllText(file).Trim();

				var offSet = currentContent.IndexOf(contentToRemove);

				string contentToWrite = currentContent.Substring(0, offSet) +
				                        currentContent.Substring(offSet + contentToRemove.Length);

				if (contentToWrite.Trim().Length == 0)
				{
					File.Delete(file);
				} else {
					File.WriteAllText(file, contentToWrite);
				}
			}
		}
	}
}
