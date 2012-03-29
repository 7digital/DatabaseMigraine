
using System;
using System.Collections.Generic;
using System.IO;

namespace DatabaseMigraine.Managers
{
	public class StaticDataManager : DbScriptFolderManager
	{
		public override string FolderName { get { return "07_StaticData"; } }

		//TODO: check to insert data only in lku* tables (or lnkXYZ if lkuX and lkuYZ exist, i.e.: lnkFormatGroupFormat)

		private static volatile StaticDataManager _instance;
		private static readonly object SyncRoot = new object();
		public static StaticDataManager Instance {
			get {
				//PLEASE don't be tempted to remove the double-null-check, this is correct! http://www.mono-project.com/Gendarme.Rules.Concurrency#DoubleCheckLockingRule
				if (_instance == null) {
					lock (SyncRoot) {
						if (_instance == null) {
							_instance = new StaticDataManager();
						}
					}
				}
				return _instance;
			}
		}

		//this private ctor is just to force the consumers of the API to use the singleton
		private StaticDataManager() { }

		protected override void RunScript(System.Collections.Generic.KeyValuePair<string, string> script, string dbname)
		{
			const string trySetIdentity = @"
BEGIN TRY
  SET IDENTITY_INSERT {0} {1}
END TRY
BEGIN CATCH
END CATCH";
			string trySetIdentityInsertOff = String.Format(trySetIdentity, script.Key, "OFF");
			string trySetIdentityInsertOn = String.Format(trySetIdentity, script.Key, "ON");
			string scriptWithIdentityTweak = String.Format(@"
{0}

{1}

{2}", trySetIdentityInsertOn, script.Value, trySetIdentityInsertOff);

			base.RunScript(new KeyValuePair<string, string>(script.Key, scriptWithIdentityTweak), dbname);
		}

		internal void UpdateScripts(string dbScriptsPath, FileInfo migration)
		{
			string subfolderScripts = Path.Combine(dbScriptsPath, FolderName);
			if (!Directory.Exists(subfolderScripts))
			{
				Directory.CreateDirectory(subfolderScripts);
			}

			string insertStatement = MigrationManager.GetInsertStatementForMigration(migration);

			string staticDataFilePath = Path.Combine(subfolderScripts, MigrationManager.DatabaseChangeLogScriptName);

			File.AppendAllText(staticDataFilePath, Environment.NewLine + insertStatement);
		}
	}
}
