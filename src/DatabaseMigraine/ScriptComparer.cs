using System;
using System.Text.RegularExpressions;

namespace DatabaseMigraine
{
	public class ScriptComparer
	{
		private static bool IgnoreCaseSensitivity
		{
			get { return Settings.IsBoolSettingEnabled("IgnoreCaseSensitivity"); }
		}

		private static bool IgnoreIdentitySeed
		{
			get { return Settings.IsBoolSettingEnabled("IgnoreIdentitySeed"); }
		}

		private static bool IgnoreNotForReplication
		{
			get { return Settings.IsBoolSettingEnabled("IgnoreNotForReplication"); }
		}

		public static string SanitizeIdentity(string scriptContents)
		{
			var regex = new Regex(@"identity\s*\((\s*\d+\s*,\s*\d+\s*)\)", RegexOptions.IgnoreCase);
			return regex.Replace(scriptContents, "identity(x,y)");
		}

		public static string SanitizeReplication(string scriptContents)
		{
			var regex = new Regex(@"not\s+for\s+replication", RegexOptions.IgnoreCase);
			return regex.Replace(scriptContents, String.Empty);
		}

		private static string SanitizeCase(string scriptContents)
		{
			return scriptContents.ToLower();
		}

		private static string SanitizeParenthesis(string scriptContents)
		{
			return scriptContents.Replace(") ,", "),");
		}

		private static string SanitizeByTrimming(string scriptContents)
		{
			return Formatter.Formatter.ReplaceEveryBlankWithASpace(scriptContents.Trim());
		}

		//FIXME: experiment about removing this Sanitize, because our scripts should mimic exactly what SqlServer outputs
		public static string SanitizeSqlServerRhetoric(string scriptContents)
		{
			foreach (Match match in new Regex (@"\[[^\.\s]+\]").Matches(scriptContents))
			{
				scriptContents = scriptContents.Replace(match.Value, match.Value.Substring(1, match.Value.Length - 2));
			}

			scriptContents = scriptContents.Replace("dbo.", String.Empty);

			return scriptContents;
		}

		public static string Sanitize(string scriptContents)
		{
			if (IgnoreCaseSensitivity)
			{
				scriptContents = SanitizeCase(scriptContents);
			}

			if (IgnoreIdentitySeed)
			{
				scriptContents = SanitizeIdentity(scriptContents);
			}

			if (IgnoreNotForReplication)
			{
				scriptContents = SanitizeReplication(scriptContents);
			}

			return SanitizeParenthesis(SanitizeSqlServerRhetoric(SanitizeByTrimming(scriptContents)));
		}
	}
}