using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatabaseMigraine.Managers;
using Formatter = DatabaseMigraine.Formatter;


namespace DatabaseScripter
{
	class Program
	{
		private static string _pathToWorkOn;

		static void Main(string[] args)
		{
			_pathToWorkOn = args[0];
			var dir = new DirectoryInfo(_pathToWorkOn);

			if (!dir.Exists)
			{
				Console.WriteLine("Directory not found: " + _pathToWorkOn);
				Environment.Exit(1);
			}
			_pathToWorkOn = dir.FullName;

			var sqlFiles = Directory.GetFileSystemEntries(_pathToWorkOn, "*.sql").Select(x => new FileInfo(x));

			if (!sqlFiles.Any())
			{
				Console.WriteLine("No *.sql files found in " + _pathToWorkOn);
				Console.WriteLine("Please supply a path as an argument or run this program in the same location where the sql files are.");
				Environment.Exit(1);
			}

			try
			{
				foreach (var sqlFile in sqlFiles)
				{
					string targetFilePath = FindDestination(sqlFile);

					if (String.IsNullOrEmpty(targetFilePath)) {
						if (Directory.Exists(targetFilePath) || File.Exists(targetFilePath))
						{
							Console.Error.WriteLine("Warning, could not move because target file already exists: " + targetFilePath);
						} else {
							Move(sqlFile, targetFilePath);
						}
					}
					else
					{
						File.Create(Path.Combine(sqlFile.Directory.FullName, "blah.sql"));
						File.Create(Path.Combine(sqlFile.Directory.FullName, "blah2.sql"));
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				Environment.Exit(2);
			}
		}

		static void Move (FileInfo sourceFileName, string destFileName)
		{
			if (IsGit()) {
				Git.Move(sourceFileName, destFileName);
			} else {
				File.Move(sourceFileName.FullName, destFileName);
			}

			RestoreDefaultEncodingThatIsGrepable(new FileInfo(destFileName));
		}

		private static bool IsGit()
		{
			string currentPath = _pathToWorkOn;
			while (true)
			{
				const string dotGitPathName = ".git";

				//Console.WriteLine("Looking for path in " + currentPath);
				string dotGitPath = Path.Combine(currentPath, dotGitPathName);
				if (Directory.Exists(dotGitPath)) {
					return true;
				}

				var directories = currentPath.Split(new[] { Path.DirectorySeparatorChar });
				var dotdotcount = directories.Count(dir => dir == "..");
				if (dotdotcount > (directories.Length / 2))
				{
					return false;
				}

				currentPath = Path.Combine(currentPath, "..");
			}
		}

		private static void RestoreDefaultEncodingThatIsGrepable(FileInfo destFileName)
		{
			if (Formatter.IsEncodingGrepable(destFileName))
				return;

			//this seems lame but it works
			File.WriteAllText(destFileName.FullName, File.ReadAllText(destFileName.FullName));
		}

		static string FindDestination(FileInfo originalFile)
		{
			var targetFile = RemoveDboPrefix(originalFile.FullName);

			if (suffixToDirectoryNameCorrespondence.Keys.Count == 0) {
				suffixToDirectoryNameCorrespondence.Add("Table", TableManager.Instance.FolderName);
				suffixToDirectoryNameCorrespondence.Add("UserDefinedFunction", FunctionManager.Instance.FolderName);
				suffixToDirectoryNameCorrespondence.Add("View", ViewManager.Instance.FolderName);
				suffixToDirectoryNameCorrespondence.Add("StoredProcedure", StoredProcedureManager.Instance.FolderName);
			}

			foreach(string suffix in suffixToDirectoryNameCorrespondence.Keys)
			{
				if (targetFile.ToLower().EndsWith("." + suffix.ToLower() + ".sql"))
				{
					targetFile = targetFile.Replace("." + suffix, String.Empty);

					string subFolderName = suffixToDirectoryNameCorrespondence[suffix];
					if (!originalFile.FullName.Contains(subFolderName))
					{
						var targetDir = Path.Combine(Path.GetDirectoryName(targetFile), subFolderName);
						if (!Directory.Exists(targetDir))
						{
							Directory.CreateDirectory(targetDir);
						}
						targetFile = Path.Combine(targetDir, Path.GetFileName(targetFile));
					}
				}
			}

			if (targetFile.Contains(suffixToDirectoryNameCorrespondence["Table"]))
			{
				ExtractForeignKeys(originalFile);
			}
			return targetFile;
		}

		private static void ExtractForeignKeys(FileInfo originalFile)
		{
			string foreignKeysDirName = ForeignKeyManager.Instance.FolderName;

			string content = File.ReadAllText(originalFile.FullName);
			if (!content.ToLower().Contains("alter table"))
			{
				return;
			}

			var cutOffset = content.ToLower().IndexOf("alter table");

			string foreignKeys = content.Substring(cutOffset);

			string foreignKeysDir = string.Empty;
			if (originalFile.FullName.Contains(suffixToDirectoryNameCorrespondence["Table"]))
			{
				foreignKeysDir = originalFile.DirectoryName.
					Replace(suffixToDirectoryNameCorrespondence["Table"], foreignKeysDirName);
			}
			else
			{
				foreignKeysDir = Path.Combine(originalFile.DirectoryName, foreignKeysDirName);
			}
			if (!Directory.Exists(foreignKeysDir))
			{
				Directory.CreateDirectory(foreignKeysDir);
			}
			
			string targetFile = Path.Combine(foreignKeysDir, originalFile.Name.Replace(".Table.", "."));
			targetFile = RemoveDboPrefix(targetFile);

			File.WriteAllText(targetFile, foreignKeys);

			string contentWithoutAlterTable = content.Substring(0, cutOffset);
			File.WriteAllText(originalFile.FullName, contentWithoutAlterTable);
		}

		private static string RemoveDboPrefix(string targetFile)
		{
			if (Path.GetFileName(targetFile).StartsWith("dbo."))
			{
				targetFile = targetFile.Replace("dbo.", String.Empty);
			}
			return targetFile;
		}

		static readonly Dictionary <string, string> suffixToDirectoryNameCorrespondence = new Dictionary<string, string>();

		static class Git
		{
			internal static void Move (FileInfo origin, string target)
			{
				string originDir = origin.DirectoryName;
				string targetDir = Path.GetDirectoryName(target);

				string commonDir;
				if (targetDir.Length > originDir.Length)
				{
					if (!targetDir.StartsWith(originDir))
					{
						throw new ArgumentException("Something went wrong, origin and destination should have a common path");
					}
					commonDir = originDir;
				} else
				{
					if (!originDir.StartsWith(targetDir))
					{
						throw new ArgumentException("Something went wrong, origin and destination should have a common path");
					}
					commonDir = targetDir;
				}

				string originPath = origin.FullName.Substring(commonDir.Length).TrimStart(Path.DirectorySeparatorChar);
				string targetPath = target.Substring(commonDir.Length).TrimStart(Path.DirectorySeparatorChar);

				var process = new System.Diagnostics.Process
				{
					StartInfo =
					{
						CreateNoWindow = true,
						WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
						FileName = "git",
						Arguments = "mv " + originPath + " " + targetPath,
						WorkingDirectory = commonDir
					}
				};
				process.Start();
				process.WaitForExit();
			}
		}
	}
}
