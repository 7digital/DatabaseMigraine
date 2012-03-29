
namespace DatabaseMigraine.DatabaseElements
{
	public interface IScriptableDatabaseElementWithName
	{
		string Name { get; }

		string FullName { get; }

		string FileName { get; }

		string FileNameWithoutExtension { get; }

		string ScriptContents { get; }

		string ScriptFileContents { get; }
	}
}
