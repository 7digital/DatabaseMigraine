using System;
using System.Collections.Generic;

namespace DatabaseBisect
{
	public class ProgramFilesFinder : IProgramFilesFinder
	{
		private readonly IDirectory _directoryService;

		public ProgramFilesFinder (IDirectory directoryService)
		{
			_directoryService = directoryService;
		}

		public IEnumerable<string> GetPossibleLocations()
		{
			var defaultProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (_directoryService.Exists(defaultProgramFiles))
				yield return defaultProgramFiles;

			var alternative = defaultProgramFiles + " (x86)";
			if (_directoryService.Exists(alternative))
				yield return alternative;

			alternative = defaultProgramFiles + " (x64)";
			if (_directoryService.Exists(alternative))
				yield return alternative;
		}
	}
}