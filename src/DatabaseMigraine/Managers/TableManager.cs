using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseMigraine.Managers
{
	public class TableManager : DbScriptFolderManager
	{
		public override string FolderName { get { return "01_Tables"; } }

		private static volatile TableManager _instance;
		private static readonly object SyncRoot = new object();
		public static TableManager Instance {
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new TableManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private TableManager () { }

		protected override void CheckContentViolationRules(Dictionary<string, string> elementNameToScriptContents, string originalDbName)
		{
			base.CheckContentViolationRules(elementNameToScriptContents, originalDbName);
			CheckForeignKeyInclusionViolation(elementNameToScriptContents);
		}

		public static void CheckForeignKeyInclusionViolation(IDictionary<string, string> scriptsWithContents)
		{
			var violators = new List<string>();
			var firstViolatorContent = string.Empty;
			foreach (var script in scriptsWithContents)
			{
				string contents = script.Value.ToLower();

				if (contents.Contains("foreign key")) {
					violators.Add(script.Key);
					if (String.IsNullOrEmpty(firstViolatorContent)) {
						firstViolatorContent = contents;
					}
				}
			}
			if (violators.Count > 0)
			{
				throw new ContentViolationException(
					violators,
					String.Format("The following scripts contain Foreign Keys addition, did you run the DatabaseScripter on them to split them out in a different folder?:"),
					firstViolatorContent);
			}
		}

		protected override void RemoveOldScripts<T>(string subfolderScripts, ChangeSet<T> changeSet)
		{
			base.RemoveOldScripts(subfolderScripts, changeSet);

			//FKs need to be removed as well
			string fkSubfolderScripts = Path.Combine(Path.Combine(subfolderScripts, ".."), ForeignKeyManager.Instance.FolderName);
			foreach (var removed in changeSet.Removed)
			{
				string file = Path.Combine(fkSubfolderScripts, removed.Value.FileName);
				Console.WriteLine("Deleting (FKs) " + removed.Value.FileName);
				File.Delete(file);
			}
		}
	}
}
