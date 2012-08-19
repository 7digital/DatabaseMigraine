using System.Collections.Generic;

namespace DatabaseBisect
{
	public interface IProgramFilesFinder
	{
		IEnumerable<string> GetPossibleLocations();
	}
}