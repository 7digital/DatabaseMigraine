using System;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Instrumentation;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class ForeignKeyElement : Base, IScriptableDatabaseElementWithName
	{
		private readonly ForeignKey _foreignKey;

		internal ForeignKeyElement(ForeignKey foreignKey) : base (foreignKey, foreignKey)
		{
			_foreignKey = foreignKey;
		}

		public override string FileNameWithoutExtension
		{
			get { return _foreignKey.Parent.Name; }
		}

		public override string FullName
		{
			get { return _foreignKey.Parent.Name + ParentToChildrenSeparatorInFullName + Name; }
		}

		public override string ScriptContents
		{
			get
			{
				foreach (string fragment in ParentTableScriptAlterFragments())
				{
					if (fragment.ToLower().Contains(_foreignKey.Name.ToLower()))
					{
						return fragment;
					}
				}
				throw new InstanceNotFoundException(String.Format("FK {0} was not found in script of table {1}", _foreignKey.Name, _foreignKey.Parent.Name));
			}
		}

		public override string ScriptFileContents
		{
			get { return JoinScriptFragments(ParentTableScriptAlterFragments()); }
		}

		private static string JoinScriptFragments(string[] fragments)
		{
			var wrapper = new StringCollection();
			wrapper.AddRange(fragments);
			return JoinScriptFragments(wrapper);
		}

		internal string [] ParentTableScriptAlterFragments()
		{
			return _foreignKey.Parent.Script(ForeignKeyScriptingOptions)
				.Cast<string>()
				.Where(fragment => fragment.ToLower().Contains("alter table"))
				.ToArray();
		}

		static readonly ScriptingOptions ForeignKeyScriptingOptions = new ScriptingOptions
		{
			DriAllConstraints = false,
			DriPrimaryKey = false,
			DriDefaults = false,
			DriIndexes = false,
			Statistics = false,
			DriForeignKeys = true,
			DriWithNoCheck = true,
			SchemaQualifyForeignKeysReferences = true
		};
	}
}
