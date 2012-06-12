using System;
using NUnit.Framework;

namespace DatabaseMigraine.Unit.Tests
{
	public class ScriptComparerTests
	{
		[Test]
		public void Sanitize_Identity_Disregards_Identity_Numbers_To_Compare()
		{
			const string input = "blah bla iDentity(10,3) bla blah";
			string expected = input.Replace("10,3", "x,y").ToLower();
			Assert.That(ScriptComparer.SanitizeIdentity(input), Is.EqualTo(expected));
		}

		[Test]
		public void Sanitize_Replication_Disregards_NotForReplication_Clause()
		{
			const string input = "blah bla not  FoR rePlication bla blah";
			string expected = input.Replace("not  FoR rePlication", String.Empty).ToLower();
			Assert.That(ScriptComparer.SanitizeReplication(input), Is.EqualTo(expected));
		}

		[Test]
		public void Sanitize_SqlServer_Square_Bracket_Quirks()
		{
			const string input = "blah bla [heeeh] [hee hee] [pepe.juan] bla blah";
			string expected = input.Replace("[heeeh]", "heeeh").ToLower();
			Assert.That(ScriptComparer.SanitizeSqlServerRhetoric(input), Is.EqualTo(expected));
		}
	}
}
