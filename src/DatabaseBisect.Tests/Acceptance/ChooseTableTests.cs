using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Acceptance
{
	[TestFixture]
	public class ChooseTableTests : DbHelper
	{
		[Test]
		public void ChooseTableDoesntChooseAnEmptyTable()
		{
			var db = GivenADisposableDbCreatedForTesting();
			DbState dbState = AndTheDbHasAtLeast2TablesAndTheFirstOneIsEmptyAndSecondOneIsNotEmpty(db);
			var table = WhenWeChooseATableToBisect(db);
			ThenTheTableSelectedIsNotAnEmptyOne(table, dbState);
		}

		[Test]
		public void ChooseTableChoosesTheBiggestTable()
		{
			var db = GivenADisposableDbCreatedForTesting();
			DbState dbState = AndTheDbHasAtLeast2NonEmptyTablesWithDifferentNumberOfRowsAndFirstTableShouldntHaveTheHighestRowCount(db);
			var table = WhenWeChooseATableToBisect(db);
			ThenTheTableSelectedIsTheOneWithTheHighestNumberOfRows(table, dbState);
		}

		private void ThenTheTableSelectedIsTheOneWithTheHighestNumberOfRows(Table table, DbState dbState)
		{
			KeyValuePair<string, int>? highest = null;
			foreach (var tableToRowCount in dbState)
			{
				if (highest == null)
				{
					highest = tableToRowCount;
					continue;
				}
				if (tableToRowCount.Value > highest.Value.Value)
				{
					highest = tableToRowCount;
				}
			}
			Assert.That(highest, Is.Not.Null);
			Assert.That(highest.Value.Key, Is.EqualTo(table.Name));
		}

		private void ThenTheTableSelectedIsNotAnEmptyOne(Table table, DbState state)
		{
			Assert.That(state[table.Name], Is.GreaterThan(0));
		}

		private Table WhenWeChooseATableToBisect(Database db)
		{
			return BisectOperations.ChooseTableToBisect(db);
		}

		private static DbState AndTheDbHasAtLeast2NonEmptyTablesWithDifferentNumberOfRowsAndFirstTableShouldntHaveTheHighestRowCount(Database db)
		{
			var state = new DbState(db);
			int? firstNonEmptyNumberOfRows = null;

			foreach (var rowCount in state.Values)
			{
				if (rowCount == 0)
				{
					continue;
				}
				if (firstNonEmptyNumberOfRows == null)
				{
					firstNonEmptyNumberOfRows = rowCount;
				}
				else
				{
					if (firstNonEmptyNumberOfRows.Value < rowCount)
					{
						return state;
					}
				}
			}
			Assert.Fail("Test setup is wrong: should have at least 2 tables with different rowcount and bigger than zero, and the first table shouldn't be the one with the highest rowcount...");
			return null;
		}
	}
}
