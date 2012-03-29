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
				if (view.IsSystemObject)
				{
					continue;
				}
				yield return new ViewElement(view);
			}
			yield break;
		}
	}
}