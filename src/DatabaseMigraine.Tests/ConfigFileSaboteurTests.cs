using System;
using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	[TestFixture]
	public class ConfigFileSaboteurTests
	{
		const string SomeContentOfConfigFile =
"<configuration>" +
"  <connectionStrings>" +
"    <add name=\"someDsn\" connectionString=\"Server=someServerName,59281;Initial Catalog=someDbName;User Id=someUserId;Password=somePassword;Min Pool Size=5;Max Pool Size=200;Application Name=SomeAppName\" />" +
"  </connectionStrings>" +
"  <appSettings>" +
"    <add key=\"someOtherDsn\" value=\"User Id=someUserId;Password=somePassword;Initial Catalog=someOtherDbName;Server=celda;Min Pool Size=5;Max Pool Size=200;Application Name=SomeAppName;\" />" +
"    <add key=\"YetAnotherDsn\" value = \"User Id=someUserId;Password=somePassword;Initial Catalog=someDbName_some_suffix;Server=someServerName;Min Pool Size=5;Max Pool Size=200;Application Name=SomeAppName;\"/>" +
"  </appSettings>" +
"</configuration>";

		[Test]
		public void Connection_string_is_changed ()
		{
			const string newDbName = "DELETEME_blah";
			const string originalDbName = "someDbName";
			string result = ConfigFileSaboteur.SabotageContent(SomeContentOfConfigFile, originalDbName, newDbName);

			string expected = SomeContentOfConfigFile.Replace(String.Format("Initial Catalog={0};", originalDbName), 
			                                                  String.Format("Initial Catalog={0};", newDbName));

			Assert.That(result, Is.EqualTo(expected));
		}

		[Test]
		public void If_using_a_star_all_connection_strings_complying_with_the_prefix_are_changed()
		{
			const string newDbName = "DELETEME_blah";
			const string originalDbName = "someDbName*";
			string result = ConfigFileSaboteur.SabotageContent(SomeContentOfConfigFile, originalDbName, newDbName);

			string expected = SomeContentOfConfigFile.Replace("Initial Catalog=someDbName_some_suffix;",
			                                                  String.Format("Initial Catalog={0};", newDbName));
			expected = expected.Replace("Initial Catalog=someDbName;",
			                            String.Format("Initial Catalog={0};", newDbName));

			Assert.That(result, Is.EqualTo(expected));
		}
	}
}
