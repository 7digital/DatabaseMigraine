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
		public NUnitFinder(IDirectory dir)
		{
			this._dirLayer = dir;
		}

		public IEnumerable<DirectoryInfo> GetNUnitDirs()
		{
			//return Directory.GetFileSystemEntries("C:\\Program Files (x86)", "NUnit*").Select(x => new DirectoryInfo(x));
			return new[] { new DirectoryInfo("C:\\"), new DirectoryInfo("C:\\") };
		}

	}
}
