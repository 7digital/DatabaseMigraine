using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseMigraine.Tests
{
	public abstract class TableCop
	{
		public abstract string DbName { get; }

		protected Database DisposableDb;

		[TestFixtureSetUp]
		public void CreateDisposableDb ()
		{
			string dbCreationPath = ConfigurationManager.AppSettings["DbCreationPath"];
			Server server = DatabaseCreationTests.GetDisposableDbServer();
			var creator = new DisposableDbManager(dbCreationPath, server, DbName);
			DisposableDb = server.Databases[creator.CreateCompleteDisposableDb()];
		}

		protected static readonly string[] DeprecatedPrefixes = new[]
		{
			"tbl", // table for a plain entity
			"lnk", // link table, for M-N relationships
			"lku",  // lookup table, static initial data

			"dtbl", // denormalized table
		};

		protected virtual bool Discard(string tableName)
		{
			return tableName == "DatabaseChangeLog";
		}

		protected virtual bool Discard(Column column)
		{
			return false;
		}

		protected static bool StartsWithPrefix(string tableName)
		{
			if (DeprecatedPrefixes.Any(tableName.StartsWith))
			{
				return true;
			}
			return false;
		}

		[Test]
		public virtual void TableNamesDontHaveDeprecatedPrefix()
		{
			foreach (Table table in NonDiscardedTables())
			{
				Assert.That(StartsWithPrefix(table.Name), Is.False, "Table name shouldn't start with a deprecated prefix: " + table.Name);
			}
		}

		[Test]
		public void TablesDontHaveSmellyNames()
		{
			foreach (Table table in NonDiscardedTables())
			{
				Assert.That(table.Name, Is.Not.StringEnding("2"));
				Assert.That(table.Name.ToLower(), Is.Not.StringEnding("old"));
			}
		}

		[Test]
		public void TableNamesDontHaveUnderscores()
		{
			foreach (Table table in NonDiscardedTables())
			{
				Assert.That(table.Name, Is.Not.StringContaining("_"),
					"Table names follow pascal notation, don't include underscores please: " + table.Name);
			}
		}


		[Test]
		[Ignore("Not yet ready, SQL standard approval?")]
		public virtual void TableNamesArePlural()
		{
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: remove this
					!table.Name.StartsWith("dtbl"))
				{
					Assert.That(table.Name, Is.StringEnding("s"));
				}
			}
		}

		[Test]
		public virtual void TableFirstColumnEndsWithId()
		{
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: confirm this:
					!table.Name.StartsWith("dtbl"))
				{
					//TODO: remove the "ToLower" call
					Assert.That(table.Columns[0].Name.ToLower(), Is.StringEnding("id"),
						String.Format("First column {0} of table {1} should end with 'Id'", table.Columns[0].Name, table.Name));
				}
			}
		}

		[Test]
		public virtual void TableFirstColumnIsPrimaryKey()
		{
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: confirm this:
					!table.Name.StartsWith("dtbl"))
				{
					Assert.That(table.Columns[0].InPrimaryKey, Is.True,
						String.Format("First column {0} of table {1} should be in Primary Key", table.Columns[0].Name, table.Name));
				}
			}
		}

		//TODO: [Test]
		// public void ColumnsThatAreForeignKeyShouldBeNamedInTheSameWayAsThePkThatTheyReference ()

		[Test]
		public virtual void TableFirstColumnIsTableName()
		{
			//var offending = new Dictionary<Column, Table>();
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: remove this:
					!table.Name.StartsWith("dtbl") &&
					
					//TODO: confirm this:
					!table.Name.StartsWith("lnk")
					
					)
				{
					Column firstColumn = table.Columns[0];
					if (firstColumn.IsForeignKey)
					{
						//because in this case the rule is ColumnsThatAreForeignKeyShouldBeNamedInTheSameWayAsThePkThatTheyReference()
						continue;
					}

					string tableName = table.Name.ToLower();
					if (StartsWithPrefix(tableName))
					{
						//3 == "tbl".Length
						tableName = table.Name.Substring(3, table.Name.Length - 3).ToLower();
					}
					
					//if (table.Columns[0].Name.ToLower() != tableNameWithoutPrefix + "id")
					//{
					//    offending.Add(table.Columns[0], table);
					//}
					Assert.That(firstColumn.Name.ToLower(), Is.EqualTo(tableName + "id"),
						String.Format("First column {0} of table {1} should be same name as table but in singular", table.Columns[0].Name, table.Name));
				}
			}

			//string offendingToString = String.Empty;
			//foreach (var pair in offending)
			//{
			//    offendingToString += String.Format(Environment.NewLine + "- Table {0} column {1}", pair.Value.Name, pair.Key.Name);
			//}

			//Assert.That(offending.Count, Is.EqualTo(0), "Following columns should have same name as their table because it's the first column" +
			//    offendingToString);
		}

		[Test]
		public virtual void ColumnsEndingWithIdShouldBeForeignOrPrimaryKey()
		{
			//var offending = new Dictionary<Column, Table>();
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: confirm this:
					!table.Name.StartsWith("dtbl")
					)
				{
					foreach(Column column in table.Columns)
					{
						if (column.Name.ToLower().EndsWith("id") &&
							column.Name.ToLower() != "rowguid" &&
							!Discard(column))
						{
							//if (!column.IsForeignKey && !column.InPrimaryKey)
							//{
							//    offending.Add(column, table);
							//}
							Assert.That(column.IsForeignKey || column.InPrimaryKey,
								Is.True,
								String.Format("Column {0} of table {1} should be foreign or primary key because it ends with 'Id'", column.Name, table.Name));
						}
					}
				}
			}

			//string offendingToString = String.Empty;
			//foreach(var pair in offending)
			//{
			//    offendingToString += String.Format(Environment.NewLine + "- Table {0} column {1}", pair.Value.Name, pair.Key.Name);
			//}

			//Assert.That(offending.Count, Is.EqualTo(0), "Following columns end with ID but are not PK or FK:" +
			//    offendingToString);
		}

		[Test]
		public virtual void ColumnsBeingForeignOrPrimaryKeyShouldEndWithId()
		{
			foreach (Table table in NonDiscardedTables())
			{
				if (//TODO: confirm this:
					!table.Name.StartsWith("dtbl")
					)
				{
					foreach (Column column in table.Columns)
					{
						if (column.IsForeignKey || column.InPrimaryKey)
						{
							Assert.That(column.Name.ToLower().EndsWith("id"),
								Is.True,
								String.Format("Column {0} of table {1} should end with 'Id' because it is a foreign or primary key", column.Name, table.Name));
						}
					}
				}
			}
		}

		[Test]
		public void FindOutSmellyManyToManyTables ()
		{
			foreach (Table table in NonDiscardedTables())
			{
				var idColumnNames = new List<string>();
				foreach (Column column in table.Columns)
				{
					if (column.Name.ToLower().EndsWith("id"))
					{
						idColumnNames.Add(column.Name.ToLower().Substring(0, column.Name.Length - "id".Length));
					}
				}

				//2 would be a many-to-one; more than 3 would be nonsense (we probably need another Test for that case)
				if (idColumnNames.Count != 3)
				{
					continue;
				}


				var orderedIdColumnNames = from columnName in idColumnNames orderby columnName.Length select columnName;
				if (orderedIdColumnNames.ElementAt(0) + orderedIdColumnNames.ElementAt(1) == orderedIdColumnNames.ElementAt(2) ||
				    orderedIdColumnNames.ElementAt(1) + orderedIdColumnNames.ElementAt(0) == orderedIdColumnNames.ElementAt(2))
				{
					Assert.Fail(
						String.Format("The table {0} seems to be a many-to-many relationship that has an unneeded column called {1}.",
							table.Name, orderedIdColumnNames.ElementAt(2) + "ID") +
						" Many-to-many relationships are tables that normally just have 2 joint-ForeignKeys that act as a Primary Key." +
						" There is no need to add an extra ID column on top of them.");
				}
			}

		}

		[Test]
		public virtual void BeConsistentWrtCaseOfId ()
		{
			var counts = new Dictionary<string, List<Column>>();
			foreach (Table table in NonDiscardedTables())
			{
				foreach (Column column in table.Columns)
				{
					if (column.Name.ToLower().EndsWith("id") &&
						!column.Name.ToLower().EndsWith("guid"))
					{
						string twoChars = column.Name.Substring(column.Name.Length - "id".Length, "id".Length);
						if (!counts.ContainsKey(twoChars)) {
							counts[twoChars] = new List<Column> { column };
						} else {
							counts[twoChars].Add(column);
						}
					}
				}
			}

			var maxCount = new KeyValuePair<string, List<Column>>(null, null);
			var minCount = new KeyValuePair<string, List<Column>>(null, null);

			foreach(var individualCount in counts)
			{
				if (maxCount.Key == null && minCount.Key == null)
				{
					maxCount = individualCount;
					minCount = individualCount;
					continue;
				}
				if (maxCount.Value.Count < individualCount.Value.Count)
				{
					maxCount = individualCount;
				} else if (minCount.Value.Count > individualCount.Value.Count)
				{
					minCount = individualCount;
				}
			}

			string leastUsed = String.Empty;
			if (minCount.Key != null)
			{
				foreach(Column column in minCount.Value)
				{
					var tableName = "Unknown";
					if (column.Parent is Table)
					{
						tableName = ((Table) column.Parent).Name;
					}
					leastUsed += Environment.NewLine +
					             String.Format("- Column: {0} in table {1}", column.Name, tableName);
				}
			}
			Assert.That(counts.Keys.Count, Is.EqualTo(1),
				String.Format("It's not clear if you're using 'ID', 'Id', 'id' or 'iD'. The most used is: '{0}' ({1})." + 
				              " Least used is '{2}' ({3}) and the occurrences are:{4}", 
				              maxCount.Key, maxCount.Value.Count, minCount.Key, minCount.Value.Count, leastUsed));
		}

		private IEnumerable<Table> NonDiscardedTables()
		{
			foreach (Table table in DisposableDb.Tables)
			{
				if (!Discard(table.Name))
				{
					yield return table;
				}
			}
		}

	}
}
