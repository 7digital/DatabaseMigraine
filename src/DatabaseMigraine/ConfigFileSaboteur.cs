using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DatabaseMigraine
{
	public class ConfigFileSaboteur
	{
		public static string SabotageContent(string someContentOfConfigFile, string originalDbName, string newDbName)
		{
			var result = someContentOfConfigFile;
			var regex = new Regex(@"Catalog\=([^\;]*)\;");
			var matches = regex.Matches(someContentOfConfigFile);
			foreach (Match match in matches)
			{
				if (match.Groups.Count > 1)
				{
					string oldDbName = match.Groups[1].Value;
					if (!originalDbName.Contains("*"))
					{
						if (originalDbName == oldDbName)
						{
							result = result.Replace("Catalog=" + oldDbName + ";", "Catalog=" + newDbName + ";");
						}
					}
					else
					{
						if (originalDbName.EndsWith("*"))
						{
							if (oldDbName.StartsWith(originalDbName.Substring(0, originalDbName.Length - 1)))
							{
								result = result.Replace("Catalog=" + oldDbName + ";", "Catalog=" + newDbName + ";");
							}
						}
						else
						{
							throw new NotSupportedException("ConfigFileSaboteur only works with wildcards at the end of the dbname");
						}
					}
				}
			}
			return result;
		}

		public static void Sabotage(string configFile, string originalDbName, string disposableDbName)
		{
			File.WriteAllText(configFile, SabotageContent(File.ReadAllText(configFile), originalDbName, disposableDbName));
		}
	}
}
