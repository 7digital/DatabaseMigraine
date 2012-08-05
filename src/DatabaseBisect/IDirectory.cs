namespace DatabaseBisect
{
	public interface IDirectory
	{
		bool Exists(string path);

		string[] GetFileSystemEntries (string path);
	}
}