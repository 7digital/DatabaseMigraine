using System;
using System.Linq;
using DatabaseMigraine.DatabaseElements;

namespace DatabaseMigraine
{
	public class DatabaseChangeSet
	{
		public ChangeSet<TableElement> TableChanges { get; set; }
		public ChangeSet<FunctionElement> FunctionChanges { get; set; }
		public ChangeSet<ViewElement> ViewChanges { get; set; }
		public ChangeSet<StoredProcedureElement> StoredProcedureChanges { get; set; }
		public ChangeSet<TableTriggerElement> TriggerChanges { get; set; }
		public ChangeSet<ForeignKeyElement> ForeignKeyChanges { get; set; }

		public bool IsEmpty
		{
			get
			{
				return
					TableChanges.IsEmpty &&
					FunctionChanges.IsEmpty &&
					ViewChanges.IsEmpty &&
					StoredProcedureChanges.IsEmpty &&
					TriggerChanges.IsEmpty &&
					ForeignKeyChanges.IsEmpty;
			}
		}

		public bool HasModifications
		{
			get
			{
				return
					TableChanges.Modified.Count > 0 ||
					FunctionChanges.Modified.Count > 0 ||
					ViewChanges.Modified.Count > 0 ||
					StoredProcedureChanges.Modified.Count > 0 ||
					TriggerChanges.Modified.Count > 0 ||
					ForeignKeyChanges.Modified.Count > 0;
			}
		}

		//FIXME: I hate out parameters, but tell me a better way of doing this??
		public void GetFirstDifference(out string nameOfElement, out string after, out string before)
		{
			if (IsEmpty) {
				throw new InvalidOperationException("This changeSet is empty");
			}

			if (!TableChanges.IsEmpty)
			{
				nameOfElement = TableChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = TableChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = TableChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}
			if (!FunctionChanges.IsEmpty)
			{
				nameOfElement = FunctionChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = FunctionChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = FunctionChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}
			if (!ViewChanges.IsEmpty)
			{
				nameOfElement = ViewChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = ViewChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = ViewChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}
			if (!StoredProcedureChanges.IsEmpty)
			{
				nameOfElement = StoredProcedureChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = StoredProcedureChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = StoredProcedureChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}
			if (!TriggerChanges.IsEmpty)
			{
				nameOfElement = TriggerChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = TriggerChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = TriggerChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}
			if (!ForeignKeyChanges.IsEmpty)
			{
				nameOfElement = ForeignKeyChanges.Modified.Values.FirstOrDefault().Before.Name;
				after = ForeignKeyChanges.Modified.Values.FirstOrDefault().After.ScriptContents;
				before = ForeignKeyChanges.Modified.Values.FirstOrDefault().Before.ScriptContents;
				return;
			}

			throw new InvalidOperationException("This changeSet is empty? I'm confused");
		}

		public override string ToString()
		{
			if (IsEmpty)
				return "No differences";

			string info = string.Empty;

			if (!TableChanges.IsEmpty)
			{
				info += string.Format("Table differences:{0}{1}{0}{0}", Environment.NewLine, TableChanges);
			}
			if (!FunctionChanges.IsEmpty)
			{
				info += string.Format("Function differences:{0}{1}{0}{0}", Environment.NewLine, FunctionChanges);
			}
			if (!ViewChanges.IsEmpty)
			{
				info += string.Format("View differences:{0}{1}{0}{0}", Environment.NewLine, ViewChanges);
			}
			if (!StoredProcedureChanges.IsEmpty)
			{
				info += string.Format("StoredProcedure differences:{0}{1}{0}{0}", Environment.NewLine, StoredProcedureChanges);
			}
			if (!TriggerChanges.IsEmpty)
			{
				info += string.Format("Trigger differences:{0}{1}{0}{0}", Environment.NewLine, TriggerChanges);
			}
			if (!ForeignKeyChanges.IsEmpty)
			{
				info += string.Format("ForeignKey differences:{0}{1}{0}{0}", Environment.NewLine, ForeignKeyChanges);
			}

			return info;
		}
	}
}