using System;
using DatabaseMigraine.Tests;
using NUnit.Framework;

namespace DatabaseMigraine.Unit.Tests
{
	[TestFixture]
	public class RetryTests
	{
		[Test]
		public void Retry_Calls_N_Times_If_Always_Fails()
		{
			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
				throw new Exception();
			};

			const int times = 3;
			Assert.Throws<Exception>(() => Retry.RetryXTimes(times, justThrow, 0));
			Assert.That(count, Is.EqualTo(times));
		}

		[Test]
		public void Retry_Calls_1_Time_If_It_Always_Works()
		{
			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
			};

			const int times = 3;
			Retry.RetryXTimes(times, justThrow, 0);
			Assert.That(count, Is.EqualTo(1));
		}

		[Test]
		public void Retry_Calls_N_Times_If_It_Works_At_The_Nth()
		{
			int n = 3;
			const int times = 5;
			Assert.That(n, Is.LessThan(times), "The premises of the tests are wrong, you broke the test!");

			int count = 0;
			Action<int> justThrow = x =>
			{
				count++;
				if (count < n)
				{
					throw new Exception();
				}
			};


			Retry.RetryXTimes(times, justThrow, 0);
			Assert.That(count, Is.EqualTo(n));
		}
	}
}
