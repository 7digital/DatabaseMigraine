using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DatabaseMigraine.Managers
{
	public class ViewManager : UpdatableManager
	{
		public override string FolderName { get { return "05_Views"; } }

		private static volatile ViewManager _instance;
		private static readonly object SyncRoot = new object();
		public static ViewManager Instance
		{
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new ViewManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private ViewManager() { }


		protected override string ReplaceCreateWithAlter(string scriptContents)
		{
			return Regex.Replace(scriptContents, @"CREATE\s+VIEW", "ALTER VIEW", RegexOptions.IgnoreCase);
		}

		protected override void CheckContentViolationRules(Dictionary<string, string> elementNameToScriptContents, string originalDbName)
		{
			base.CheckContentViolationRules(elementNameToScriptContents, originalDbName);

			bool succeeded = true;
			try
			{
				CheckForViewInterdependencyViolation(elementNameToScriptContents);
			} catch (ContentViolationException)
			{
				succeeded = false;
				if (!Settings.IgnoreViewPerformanceCheckForDb(originalDbName))
					throw;
			}

			if (!succeeded)
			{
				RespectDependency = true;
			} else {
				if (Settings.IgnoreViewPerformanceCheckForDb(originalDbName))
				{
					throw new Exception(String.Format(
						"You don't need to set IgnoreViewPerformanceCheck setting to true, as your DB schema for {0} respects this rule already.", originalDbName));
				}
			}
		}

		public static void CheckForViewInterdependencyViolation(IDictionary<string, string> scriptsWithContents)
		{
			var violators = new List<string>();
			var firstViolatorContent = string.Empty;
			foreach (var currentScript in scriptsWithContents)
			{
				string contents = currentScript.Value.ToLower();

				foreach (var otherScript in scriptsWithContents)
				{
					if (otherScript.Key == currentScript.Key)
						continue;

					if (contents.Contains(otherScript.Key.ToLower())) {
						violators.Add(currentScript.Key);
						if (String.IsNullOrEmpty(firstViolatorContent)) {
							firstViolatorContent = contents;
						}
						break;
					}
				}
			}
			if (violators.Count > 0)
			{
				throw new ContentViolationException(
					violators,
					String.Format(
						"The following views contain usage of other views, which could pose performance problems in the long run:"),
					firstViolatorContent);
			}
		}
	}
}
