using System;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;

namespace DatabaseBisect.Tests.Unit
{
	[TestFixture]
	public class SimpleDatabaseBisect
	{

		[Test]
		public void ExecutorBisectsTableThatAnalystHands()
		{
			var someAnalyst = new SomeAnalyst();
			var someBisector = new SomeBisector();
			var executor = new Executor(someAnalyst, someBisector);
			executor.BisectDatabase();
			Assert.That(someAnalyst.TableChosen, Is.EqualTo(someBisector.TableBisected));
		}

		class SomeAnalyst : IAnalyst
		{
			public SomeAnalyst()
			{
				TableChosen = new Table();
			}

			public Table TableChosen { get; private set; }

			public Table ChooseTableToBisect(IDataBase db)
			{
				return TableChosen;
			}
		}

		class SomeBisector : IBisector
		{
			public void BisectTableOnce(IDataBase db, Table table, Func<IDataBase, bool> verification)
			{
				TableBisected = table;
			}

			public Table TableBisected { get; private set; }
		}



		[Test]
		public void ExecutorDoesNotBisectTableIfAnalystHasFinished()
		{
			var executor = new Executor(new LazyAnalyst(), new WrongBisector());
			//just making sure this call below doesn't Assert.Fail!
			executor.BisectDatabase();
		}

		class LazyAnalyst : IAnalyst
		{
			public Table ChooseTableToBisect(IDataBase db)
			{
				//analyst has finished
				return null;
			}
		}

		class WrongBisector : IBisector
		{
			public void BisectTableOnce(IDataBase db, Table table, Func<IDataBase, bool> verification)
			{
				Assert.Fail("Should not be reached");
			}
		}
	}
}
