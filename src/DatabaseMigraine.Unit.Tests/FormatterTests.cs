using NUnit.Framework;

namespace DatabaseMigraine.Unit.Tests
{
	[TestFixture]
	public class FormatterTests
	{
		[Test]
		public void ReplaceEveryBlankWithASpace()
		{
			const string aString = @"     eeho	   euiE 	pop

rock	on
  
	
	 
end";
			const string expected = " eeho euiE pop rock on end";
			Assert.That(Formatter.ReplaceEveryBlankWithASpace(aString), Is.EqualTo(expected));
		}

		[Test]
		public void ContractEols_Normalizes_Different_Kind_Of_Eols_Into_Just_One()
		{
			string theStringToTest = string.Format(theString, "\r\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(theStringToTest.Replace("\r\n", "\n")));
		}

		[Test]
		public void ContractEols_Doesnt_Act_Like_ReplaceEveryBlankWithASpace()
		{
			Assert.That(Formatter.ContractEols(theString), Is.EqualTo(theString.Replace("\r\n", "\n")));
		}

		const string theString = @"
IndentingTest
	Inside
		Something{0}
	EndOfInside
EndOfIndentingTest
";

		[Test]
		public void ContractEols_DoesNotNuke_OneEOL()
		{
			string theStringToTest = string.Format(theString, "\r\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(theStringToTest.Replace("\r\n", "\n")));
		}

		[Test]
		public void ContractEols_DoesNotNuke_OneUnixEOL()
		{
			string theStringToTest = string.Format(theString, "\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(theStringToTest.Replace("\r\n", "\n")));
		}

		[Test]
		public void ContractEols_Nukes_Extra_EOLs()
		{
			string theStringToTest = string.Format(theString, "\r\n\r\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(string.Format(theString, "\n").Replace("\r\n", "\n")));
		}

		[Test]
		public void ContractEols_Nukes_Extra_UnixEOLs()
		{
			string theStringToTest = string.Format(theString, "\n\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(string.Format(theString, "\n").Replace("\r\n", "\n")));
		}

		[Test]
		public void ContractEols_Nukes_Extra_EOLs_With_Blanks_Inside()
		{
			string theStringToTest = string.Format(theString, "\n	  	\n  \n	\n");

			Assert.That(Formatter.ContractEols(theStringToTest), Is.EqualTo(string.Format(theString, "\n").Replace("\r\n", "\n")));
		}
	}
}
