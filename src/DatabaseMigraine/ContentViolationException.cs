using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseMigraine
{
    [Serializable]
	public class ContentViolationException : Exception
	{
		internal ContentViolationException(List<string> scriptFilePaths, string msg, string contentOfFirstElement) :
			base(String.Format(msg + CombinePaths(scriptFilePaths) +
			Environment.NewLine + Environment.NewLine + "And first violator has this content: " +
			Environment.NewLine + contentOfFirstElement))
		{
		}

		private static string CombinePaths(List<string> scriptFilePath)
		{
			return Environment.NewLine + String.Join("," + Environment.NewLine,
			  scriptFilePath.ConvertAll(Path.GetFileName).ToArray());
		}
	}
}
