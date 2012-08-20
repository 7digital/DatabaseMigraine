using System;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	public class ClearAndTestWithOperationThatInsertsData : ClearAndTestBasic
	{
		protected override Func<IDataBase,bool> TestOperationThatFails()
		{
			return db => {
				InsertSomeDataInBisectedTable(db);
				return false;
			};
		}

		protected override Func<IDataBase,bool> TestOperationThatSucceeds()
		{
			return db => {
				InsertSomeDataInBisectedTable(db);
				return true;
			};
		}

		private void InsertSomeDataInBisectedTable(IDataBase db)
		{
			const string insertSql = @"
				SET IDENTITY_INSERT {0} ON
				INSERT INTO baz(ID,baf) VALUES (1,'lele')
				SET IDENTITY_INSERT {0} OFF";
			db.ExecuteNonQuery(String.Format(insertSql, AndTheTableIChooseIs(db).Name));
		}

		protected override Table AndTheTableIChooseIs(IDataBase db)
		{
			return db.Tables["baz"];
		}
	}
}
