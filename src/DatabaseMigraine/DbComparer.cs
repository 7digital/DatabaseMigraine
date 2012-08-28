using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseMigraine.DatabaseElements;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine
{
	public class DbComparer
	{
		public static DatabaseChangeSet CompareDatabases(Database baseDatabaseBefore, Database databaseAfter)
		{
			return CompareDatabases(baseDatabaseBefore, databaseAfter, null);
		}

		public static DatabaseChangeSet CompareDatabases(Database baseDatabaseBefore, Database databaseAfter, Func<string, string> sanitizeBeforeComparison)
		{
			Console.WriteLine("Comparing databases...");
			var changeset = new DatabaseChangeSet
			{
				TableChanges = CompareDatabases<TableElement,TableElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison),
				FunctionChanges = CompareDatabases<FunctionElement, FunctionElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison),
				ViewChanges = CompareDatabases<ViewElement, ViewElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison),
				StoredProcedureChanges = CompareDatabases<StoredProcedureElement, StoredProcedureElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison),
				TriggerChanges = CompareDatabases<TableTriggerElement, TableTriggerElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison),
				ForeignKeyChanges = CompareDatabases<ForeignKeyElement, ForeignKeyElementFactory>(baseDatabaseBefore, databaseAfter, null, sanitizeBeforeComparison)
			};
			return changeset;
		}

		public static string DefaultScriptSanitizer(string contents)
		{
			return Formatter.ContractEols(contents.Trim());
		}

		public static ChangeSet<T> CompareDatabases<T, F>(Database baseDatabaseBefore, Database databaseAfter)
			where T : IScriptableDatabaseElementWithName
			where F : IScriptableWrapperFactory<T>, new()
		{
			return CompareDatabases<T, F>(baseDatabaseBefore, databaseAfter, null, null);
		}

		public static ChangeSet<T> CompareDatabases<T,F>(Database baseDatabaseBefore, Database databaseAfter, 
			                                           Func<T, bool> discard, Func<string, string> sanitizeBeforeComparison)
			where T : IScriptableDatabaseElementWithName
			where F : IScriptableWrapperFactory<T>, new()
		{
			var changeSet = new ChangeSet<T>();
			var factory = new F();

			var beforeElements = factory.Scan(baseDatabaseBefore).ToDictionary(element => element.FullName);
			var afterElements = factory.Scan(databaseAfter).ToDictionary(element => element.FullName);

			foreach (KeyValuePair<string, T> elementBefore in beforeElements)
			{
				T afterElement;
				if (!afterElements.TryGetValue(elementBefore.Key, out afterElement))
				{
					if (discard == null || !discard(elementBefore.Value)) {
						changeSet.Removed.Add(elementBefore.Key, elementBefore.Value);
					}
					continue;
				}

				string scriptBefore = elementBefore.Value.ScriptContents;
				string scriptAfter = afterElement.ScriptContents;

				if (sanitizeBeforeComparison == null)
				{
					sanitizeBeforeComparison = DefaultScriptSanitizer;
				}

				scriptBefore = sanitizeBeforeComparison(scriptBefore);
				scriptAfter = sanitizeBeforeComparison(scriptAfter);

				if (scriptBefore != scriptAfter) {
					changeSet.Modified.Add(elementBefore.Key,
						new ChangeSet<T>.Modification { Before = elementBefore.Value, After = afterElement });
				}

				afterElements.Remove(elementBefore.Key);
			}

			foreach (string afterElement in afterElements.Keys)
			{
				if (discard == null || !discard(afterElements[afterElement])) {
					changeSet.Added.Add(afterElement, afterElements[afterElement]);
				}
			}

			return changeSet;
		}
	}
}