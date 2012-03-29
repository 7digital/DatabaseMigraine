using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatabaseMigraine.DatabaseElements;
using DatabaseMigraine.Managers;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine
{
	public abstract class DbScriptFolderManager
	{
		public abstract string FolderName { get; }

		protected SqlExecutor SqlExecutor;
		protected bool RespectDependency;

		protected DbScriptFolderManager()
		{
			RespectDependency = false;
		}

		public int RunScripts(Server disposableDbServer, string dbScriptsPath, string dbname, string originalDbName)
		{
			return RunScripts(disposableDbServer, dbScriptsPath, dbname, originalDbName, null);
		}

		public virtual int RunScripts(Server disposableDbServer, string dbScriptsPath, string dbname, string originalDbName, IEnumerable<string> scriptFileNameWhiteList)
		{
			if (SqlExecutor == null) {
				SqlExecutor = new SqlExecutor(disposableDbServer);
			}

			IEnumerable<FileInfo> scriptPaths = GetSqlSriptsIn(dbScriptsPath);

			foreach (var scriptFileName in scriptPaths)
			{
				CheckEncodingConvention(scriptFileName);
			}

			IDictionary<string, string> scriptsWithContent = RetrieveContentsWhileCheckConventions(originalDbName, scriptPaths);

			if (RespectDependency) {
				scriptsWithContent = GetCorrectOrderOfScriptsRespectingDependencies(scriptsWithContent);
			}

			int count = 0;

			foreach (KeyValuePair<string, string> script in scriptsWithContent)
			{
				if (scriptFileNameWhiteList != null &&
				    !scriptFileNameWhiteList.Contains(script.Key)) {

					Console.WriteLine("Discarding " + script.Key);
					continue;
				}

				Console.WriteLine("Running " + script.Key);

				RunScript(script, dbname);
				count++;
			}

			return count;
		}

		protected virtual void RunScript(KeyValuePair<string, string> script, string dbname)
		{
			try {
				SqlExecutor.ExecuteNonQuery(script.Value, dbname);
			} catch (ExecutionFailureException e) {
				throw new InvalidOperationException(
					String.Format("{0}: Error running the script with name '{1}'", 
						GetType().Name, script.Key), e);
			}
		}

		private static IDictionary<string, string> GetCorrectOrderOfScriptsRespectingDependencies(
			IDictionary<string, string> scriptsWithContent)
		{
			var orderedScripts = new LinkedList<string>(scriptsWithContent.Keys);

			foreach (string currentScript in scriptsWithContent.Keys)
			{
				foreach (string otherScript in scriptsWithContent.Keys)
				{
					if (otherScript == currentScript) {
						continue;
					}

					string scriptContents = scriptsWithContent[currentScript];

					if (scriptContents.ToLower().Contains(otherScript.ToLower()))
					{
						LinkedListNode<string> currentNode = orderedScripts.Find(currentScript);
						orderedScripts.AddBefore(currentNode, otherScript);
						orderedScripts.Remove(orderedScripts.FindLast(otherScript));
					}
				}
			}

			var newOrderedDictionary = new Dictionary<string, string>();
			foreach (var scriptFile in orderedScripts)
			{
				newOrderedDictionary.Add(scriptFile, scriptsWithContent[scriptFile]);
			}
			return newOrderedDictionary;
		}

		public virtual IEnumerable<FileInfo> GetSqlSriptsIn(string dbScriptsPath) {
			string creationScripts = Path.Combine(dbScriptsPath, FolderName);
			if (!Directory.Exists(creationScripts)) {
				yield break;
			}
			foreach (var path in Directory.GetFileSystemEntries(creationScripts, "*.sql"))
			{
				yield return new FileInfo(path);
			}
		}

		protected virtual IDictionary<string, string> RetrieveContentsWhileCheckConventions(string originalDbName, IEnumerable<FileInfo> scripts)
		{
			var elementNameToScriptContents = new Dictionary<string, string>();

			foreach (var script in scripts)
			{
				string elementName = CheckScriptNameConventions(script);

				string scriptContents = File.ReadAllText(script.FullName);

				elementNameToScriptContents.Add(elementName, scriptContents);
			}

			CheckContentViolationRules(elementNameToScriptContents, originalDbName);

			return elementNameToScriptContents;
		}

		internal static void CheckEncodingConvention(FileInfo script)
		{
			string charSet;
			if (!Formatter.Formatter.IsEncodingGrepable(script, out charSet))
			{
				throw new NotSupportedException(
					String.Format("The file {0} has the encoding {1} which is not easily grep-able, please use the DatabaseScripter on the folder to fix this",
					script.Name, charSet));
			}
		}

		protected virtual void CheckContentViolationRules(Dictionary<string, string> elementNameToScriptContents, string originalDbName)
		{
			CheckDbNameHardcodingViolation(elementNameToScriptContents, originalDbName);
			CheckNameNotMatchingElementViolation(elementNameToScriptContents);
		}

		public const string TempPrefix = "tmp";

		public const string DbaPrefix = "dba_";

		private static string CheckScriptNameConventions(FileInfo script)
		{
			if (script == null) {
				throw new ArgumentNullException("script");
			}

			const string dboPrefix = "dbo.";

			string elementName = Path.GetFileNameWithoutExtension(script.FullName);

			if (elementName.ToLower().StartsWith(dboPrefix)) {
				throw new ArgumentException(String.Format(
					"No script should start with the '{0}' prefix, please use DatabaseScripter to transform into Migrator structure: {1}", dboPrefix, elementName));
			}

			if (elementName.ToLower().StartsWith(TempPrefix)) {
				throw new ArgumentException(String.Format(
					"Elements with the prefix '{0}' are reserved for DBA use, not development: {1}", TempPrefix, elementName));
			}

			if (elementName.ToLower().StartsWith(DbaPrefix)) {
				throw new ArgumentException(String.Format(
					"Elements with the prefix '{0}' are reserved for DBA use, not development: {1}", DbaPrefix, elementName));
			}

			return elementName;
		}

		public static void CheckDbNameHardcodingViolation (IDictionary<string,string> scriptsWithContents, string originalDbName)
		{
			var violators = new List<string>();
			string firstViolatorContent = String.Empty;
			foreach(var script in scriptsWithContents)
			{
				string contents = script.Value.ToLower();

				if (contents.Contains("[" + originalDbName.ToLower() + "]") ||
					contents.Contains("use " + originalDbName)) {
					violators.Add(script.Key);
					if (String.IsNullOrEmpty(firstViolatorContent))
					{
						firstViolatorContent = contents;
					}
				}
			}
			if (violators.Count > 0)
			{
				throw new ContentViolationException(
					violators,
					String.Format("The following scripts contain the database name hardcoded '{0}':", originalDbName),
					firstViolatorContent);
			}
		}

		public static void CheckNameNotMatchingElementViolation(IDictionary<string, string> scriptsWithContents)
		{
			var violators = new List<string>();
			var firstViolatorContent = string.Empty;
			foreach (var script in scriptsWithContents)
			{
				string contents = script.Value;

				if (!contents
					
					//this is kind-of-hack to prevent this kind of elements: [MusicEncyclopedia].[tblAlbums]
					.Replace("[", String.Empty).Replace("]", String.Empty)
					//the hack ends here :)

					.Contains(script.Key)) {

					violators.Add(script.Key);
					if (String.IsNullOrEmpty(firstViolatorContent))
					{
						firstViolatorContent = contents;
					}
				}
			}
			if (violators.Count > 0)
			{
				throw new ContentViolationException(violators,
					"The following scripts are not named in the same way as the element they provide (this policy is case-sensitive):",
					firstViolatorContent);
			}
		}

		protected void UpdateScripts<T>(string dbScriptsPath, ChangeSet<T> changeSet) where T : IScriptableDatabaseElementWithName
		{
			string subfolderScripts = Path.Combine(dbScriptsPath, FolderName);
			if (!Directory.Exists(subfolderScripts))
			{
				Directory.CreateDirectory(subfolderScripts);
			}

			UpdateModifiedScripts(subfolderScripts, changeSet);
			IncludeNewScripts(subfolderScripts, changeSet);
			RemoveOldScripts(subfolderScripts, changeSet);

			//there were no changes
			if (!Directory.GetFiles(subfolderScripts).Any())
			{
				Directory.Delete(subfolderScripts);
			}
		}

		protected void UpdateModifiedScripts<T>(string subfolderScripts, ChangeSet<T> changeSet) where T : IScriptableDatabaseElementWithName
		{
			foreach (var updated in changeSet.Modified)
			{
				string file = Path.Combine(subfolderScripts, updated.Value.After.FileName);
				Console.WriteLine("Updating " + updated.Value.After.FileName);

				string contentToWrite = updated.Value.After.ScriptFileContents.Trim();
				File.WriteAllText(file, contentToWrite);
			}
		}

		protected virtual void RemoveOldScripts<T>(string subfolderScripts, ChangeSet<T> changeSet) where T : IScriptableDatabaseElementWithName
		{
			foreach (var removed in changeSet.Removed)
			{
				string file = Path.Combine(subfolderScripts, removed.Value.FileName);
				Console.WriteLine("Deleting " + removed.Value.FileName);
				File.Delete(file);
			}
		}

		private void IncludeNewScripts<T>(string subfolderScripts, ChangeSet<T> changeSet) where T : IScriptableDatabaseElementWithName
		{
			foreach (var added in changeSet.Added)
			{
				string file = Path.Combine(subfolderScripts, added.Value.FileName);
				Console.WriteLine("Adding " + added.Value.FileName);
				File.WriteAllText(file, added.Value.ScriptFileContents);
			}
		}

		protected bool PreFormatScripts<T>(string dbScriptsPath, ChangeSet<T> changeSet) where T : IScriptableDatabaseElementWithName
		{
			string subfolderScripts = Path.Combine(dbScriptsPath, FolderName);
			if (!Directory.Exists(subfolderScripts))
			{
				return false;
			}

			bool preformatWasPerformed = false;

			foreach (var updated in changeSet.Modified)
			{
				Console.WriteLine("Preformatting " + updated.Value.Before.FileName);
				string file = Path.Combine(subfolderScripts, updated.Value.Before.FileName);

				string previousContent = File.ReadAllText(file);
				string contentThatShouldBeTherePreviously = updated.Value.Before.ScriptFileContents;
				if (DbComparer.DefaultScriptSanitizer(previousContent) !=
					DbComparer.DefaultScriptSanitizer(contentThatShouldBeTherePreviously))
				{
					File.WriteAllText(file, contentThatShouldBeTherePreviously);
					preformatWasPerformed = true;
				}
			}

			return preformatWasPerformed;
		}

		public static bool PreFormatScripts(string dbScriptsPath, DatabaseChangeSet changeSet)
		{
			return
				TableManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.TableChanges) |
				ForeignKeyManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.ForeignKeyChanges) |
				FunctionManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.FunctionChanges) |
				ViewManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.ViewChanges) |
				StoredProcedureManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.StoredProcedureChanges) |
				TriggerManager.Instance.PreFormatScripts(dbScriptsPath, changeSet.TriggerChanges);
		}

		public static void UpdateScripts(string dbScriptsPath, DatabaseChangeSet changeSet, FileInfo migrationFileName)
		{
			TableManager.Instance.UpdateScripts(dbScriptsPath, changeSet.TableChanges);
			ForeignKeyManager.Instance.UpdateScripts(dbScriptsPath, changeSet.ForeignKeyChanges);
			FunctionManager.Instance.UpdateScripts(dbScriptsPath, changeSet.FunctionChanges);
			ViewManager.Instance.UpdateScripts(dbScriptsPath, changeSet.ViewChanges);
			StoredProcedureManager.Instance.UpdateScripts(dbScriptsPath, changeSet.StoredProcedureChanges);
			TriggerManager.Instance.UpdateScripts(dbScriptsPath, changeSet.TriggerChanges);

			//TODO: changeSet.StaticDataChanges should exist, so we use the same overload than above
			StaticDataManager.Instance.UpdateScripts(dbScriptsPath, migrationFileName);
		}
	}
}