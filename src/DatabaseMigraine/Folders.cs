using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseMigraine
{
	//borrowed from http://stackoverflow.com/questions/2801165/asp-net-c-copy-directory-with-subdirectories-with-system-io
	// TODO: move to a different place?
	public class Folders
	{
		public string Source { get; private set; }
		public string Target { get; private set; }

		public Folders(string source, string target)
		{
			Source = source;
			Target = target;
		}

		public static void CopyDirectory(string source, string target)
		{
			var stack = new Stack<Folders>();
			stack.Push(new Folders(source, target));

			while (stack.Count > 0)
			{
				var folders = stack.Pop();
				Directory.CreateDirectory(folders.Target);
				foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
				{
					string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
					if (File.Exists(targetFile)) File.Delete(targetFile);
					try {
						File.Copy(file, targetFile);
					} catch (PathTooLongException ex) {
						Directory.Delete(target, true);
						throw new InvalidOperationException(
							String.Format("Couldn't copy {0} to {1} because of {2}", file, targetFile, ex.GetType().Name), ex);
					}
				}

				foreach (var folder in Directory.GetDirectories(folders.Source))
				{
					stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
				}
			}
		}
	}
}