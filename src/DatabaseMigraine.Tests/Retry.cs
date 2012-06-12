using System;

namespace DatabaseMigraine.Tests
{
	public class Retry
	{
		public static void RetryXTimes<T>(int times, Action<T> method, T instance)
		{
			while (true)
			{
				try
				{
					method(instance);
					break;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e);
					if (--times == 0)
					{
						throw;
					}
				}
			}
		}
	}
}
