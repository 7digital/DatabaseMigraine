using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DatabaseBisect
{
	public class NUnitFinder
	{
		private IDirectory _dirLayer;
		public NUnitFinder(IDirectory dir, IProgramFilesFinder programFilesFinder)
		{
			this._dirLayer = dir;
		}

		public IEnumerable<DirectoryInfo> GetNUnitDirs()
		{
			var programFileFolders = _dirLayer.GetFileSystemEntries("C:\\Program Files");
			var onlyUnitDirs = programFileFolders
				.Where(x => x.Contains("NUnit"));
			var dirInfoObjs = onlyUnitDirs.Select(x => new DirectoryInfo(x));
			return dirInfoObjs;
		}

	}
}
