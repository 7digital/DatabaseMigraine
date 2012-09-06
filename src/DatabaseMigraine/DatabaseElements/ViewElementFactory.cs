using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace DatabaseMigraine.DatabaseElements
{
	public class ViewElementFactory : IScriptableWrapperFactory<ViewElement>
	{
		public IEnumerable<ViewElement> Scan(Database db)
		{
			foreach (View view in db.Views)
			{
				if (IsSystemObject(view))
					continue;

				yield return new ViewElement(view);
			}
			yield break;
		}

		internal static bool IsSystemObject(View view)
		{
			try
			{
				if (view.IsSystemObject)
				{
					return true;
				}
			}
			catch (UnknownPropertyException)
			{
			}
			return false;
		}

	}
}