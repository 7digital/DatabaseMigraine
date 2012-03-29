using System;
using System.Configuration;

namespace DatabaseMigraine
{
	internal class Settings
	{
		//should be an enum, not a string
		internal static bool IsBoolSettingEnabled(string setting)
		{
			try
			{
				if (Boolean.Parse(ConfigurationManager.AppSettings[setting] ?? false.ToString()))
				{
					return true;
				}
			}
				//default to true
			catch (ConfigurationErrorsException) { }
			catch (FormatException) { }

			return false;
		}

		internal static bool IgnoreViewPerformanceCheckForDb(string dbname)
		{
			return IsBoolSettingEnabled(String.Format("IgnoreViewPerformanceCheck+{0}", dbname));
		}
	}
}
