using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DatabaseBisect
{
	public class NUnitFinder
	{
		private readonly IDirectory _dirLayer;
		private readonly IProgramFilesFinder _programFilesFinder;
		public NUnitFinder(IDirectory dir, IProgramFilesFinder programFilesFinder)
		{
			this._programFilesFinder = programFilesFinder;
			this._dirLayer = dir;
		}

		public IEnumerable<DirectoryInfo> GetNUnitDirs()
		{
			return
				from programFileFolder in _programFilesFinder.GetPossibleLocations()
					from fileSystemEntry in _dirLayer.GetFileSystemEntries(programFileFolder)
						where fileSystemEntry.Contains("NUnit") && _dirLayer.Exists(fileSystemEntry)
							select new DirectoryInfo(fileSystemEntry);
		}
	}
}
