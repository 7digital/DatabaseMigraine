using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseMigraine.DatabaseElements;

namespace DatabaseMigraine
{
	public class ChangeSet<T> where T : IScriptableDatabaseElementWithName
	{
		public struct Modification
		{
			public T Before { get; set; }
			public T After { get; set; }
		}

		public ChangeSet ()
		{
			Removed = new Dictionary<string, T>();
			Added = new Dictionary<string, T>();
			Modified = new Dictionary<string, Modification>();
		}

		public Dictionary<string, T> Removed { get; private set; }
		public Dictionary<string, T> Added { get; private set; }
		public Dictionary<string, Modification> Modified { get; private set; }

		public bool IsEmpty
		{
			get { return Added.Count == 0 && Removed.Count == 0 && Modified.Count == 0; }
		}

		public override string ToString()
		{
			if (IsEmpty)
				return "No differences";

			string info = String.Empty;

			if (Added.Count > 0 || Removed.Count > 0) {
				var nl = Environment.NewLine;
				string infoNoEltsDiffered = nl + "The number of {0}s in the databases are different:" + nl + nl;
				if (Added.Count > 0)
				{
					infoNoEltsDiffered += "- {0}s present in disposable DB, absent in reference environment: {1}." + nl + nl;
				}
				if (Removed.Count > 0)
				{
					infoNoEltsDiffered += "- {0}s present in reference environment, absent in disposable DB: {2}." + nl + nl;
				}

				string elementSeparation = Environment.NewLine + " * ";
				infoNoEltsDiffered = String.Format(infoNoEltsDiffered,
													 typeof(T).Name,
													 elementSeparation + String.Join(elementSeparation, Added.Keys.ToArray()),
													 elementSeparation + String.Join(elementSeparation, Removed.Keys.ToArray()));

				info += infoNoEltsDiffered;
			}

			if (Modified.Count > 0)
			{
				string elementList = string.Join(Environment.NewLine, Modified.Keys.ToArray());

				const string differencesMsg = "{0} {4}s are different: {1}{2}{1}.{1} Last difference is in element [{3}]:";

				string lastElement = Modified.Keys.Last();

				info += String.Format(differencesMsg,
				                      Modified.Count,
				                      Environment.NewLine,
				                      elementList,
				                      lastElement,
				                      typeof (T).Name);
			}


			return info;
		}
	}
}