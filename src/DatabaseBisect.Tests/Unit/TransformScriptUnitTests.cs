using System;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Unit
{
	[TestFixture]
	public class TransformScriptUnitTests
	{
		[Test]
		public void TransformScriptToBackupCreation_Simple()
		{
			const string tableName = "Foo";
			string beforeScript = String.Format("CREATE TABLE {0}(blah blah)", tableName);
			string expectedScript = String.Format("CREATE TABLE {0}(blah blah)", tableName + Analyst.BackupSuffix);
			Assert.That(Bisector.TransformCreationScriptForBackup(beforeScript, tableName), Is.EqualTo(expectedScript));
		}

		[Test]
		public void TransformScriptToBackupCreation_WithSquareBrackets()
		{
			const string tableName = "Foo";
			string beforeScript = String.Format("CREATE TABLE [{0}](blah blah)", tableName);
			string expectedScript = String.Format("CREATE TABLE {0}(blah blah)", tableName + Analyst.BackupSuffix);
			Assert.That(Bisector.TransformCreationScriptForBackup(beforeScript, tableName), Is.EqualTo(expectedScript));
		}

		[Test]
		public void TransformScriptToBackupCreation_WithDboAndSquareBrackets()
		{
			const string tableName = "Foo";
			string beforeScript = String.Format("CREATE TABLE dbo.[{0}](blah blah)", tableName);
			string expectedScript = String.Format("CREATE TABLE {0}(blah blah)", tableName + Analyst.BackupSuffix);
			Assert.That(Bisector.TransformCreationScriptForBackup(beforeScript, tableName), Is.EqualTo(expectedScript));
		}

		[Test]
		public void TransformScriptToBackupCreation_WithDboAndALotOfSquareBrackets()
		{
			const string tableName = "Foo";
			string beforeScript = String.Format("CREATE TABLE [dbo].[{0}](blah blah)", tableName);
			string expectedScript = String.Format("CREATE TABLE {0}(blah blah)", tableName + Analyst.BackupSuffix);
			Assert.That(Bisector.TransformCreationScriptForBackup(beforeScript, tableName), Is.EqualTo(expectedScript));
		}

		[Test]
		public void TransformScriptToBackupCreation_WithTrickyContents()
		{
			const string tableName = "Foo";
			string beforeScript = String.Format("CREATE TABLE [dbo].[{0}](FooBar blah blah)", tableName);
			string expectedScript = String.Format("CREATE TABLE {0}(FooBar blah blah)", tableName + Analyst.BackupSuffix);
			Assert.That(Bisector.TransformCreationScriptForBackup(beforeScript, tableName), Is.EqualTo(expectedScript));
		}
	}
}