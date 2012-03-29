using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public abstract class Base
	{
		private readonly IScriptable _scriptable;
		private readonly ScriptNameObjectBase _nameElement;

		protected const string ParentToChildrenSeparatorInFullName = "->";

		protected Base(IScriptable scriptable, ScriptNameObjectBase nameElement)
		{
			if (scriptable == null)
			{
				throw new ArgumentNullException("scriptable");
			}
			if (nameElement == null)
			{
				throw new ArgumentNullException("nameElement");
			}
			if (scriptable != nameElement)
			{
				throw new ArgumentException("Both supplied parameters should be same object");
			}

			_nameElement = nameElement;
			_scriptable = scriptable;
		}

		public virtual string FileNameWithoutExtension
		{
			get { return _nameElement.Name; }
		}

		public virtual string FullName
		{
			get
			{
				if (!String.IsNullOrEmpty(Schema))
				{
					return Schema + "." + Name;
				}
				return Name;
			}
		}

		private string Schema
		{
			get
			{
				var schemaElement = _nameElement as ScriptSchemaObjectBase;
				if (schemaElement != null &&
					schemaElement.Schema.ToLower() != "dbo" &&
					schemaElement.Schema.ToLower() != "[dbo]") {
					return schemaElement.Schema;
				}
				return null;
			}
		}

		//TODO: reuse SqlSuffix
		public string FileName { get { return FileNameWithoutExtension + ".sql"; } }

		public string Name
		{
			get { return _nameElement.Name; }
		}

		public virtual string ScriptContents
		{
			get { return JoinScriptFragments(_scriptable.Script(DefaultScriptOptions)); }
		}

		public virtual string ScriptFileContents
		{
			get { return ScriptContents; }
		}

		static readonly ScriptingOptions DefaultScriptOptions = new ScriptingOptions
		{
			DriPrimaryKey = true,
			Statistics = false,
			DriDefaults = true
		};

		public static string JoinScriptFragments(StringCollection fragments)
		{
			string separator = Environment.NewLine + "GO" + Environment.NewLine;
			string fullContent = String.Empty;
			foreach (string str in fragments)
			{
				fullContent += str + separator;
			}
			return fullContent;
		}
	}
}
