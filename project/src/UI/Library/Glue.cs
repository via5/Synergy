namespace SynergyUI
{
	public class Glue
	{
		public static MVRPluginManager PluginManager
		{
			get
			{
				return Synergy.Synergy.Instance.manager;
			}
		}

		public static string GetString(string s, params object[] ps)
		{
			return Synergy.Strings.Get(s, ps);
		}

		public static void LogError(string s)
		{
			Synergy.Synergy.LogError(s);
		}

		public static void LogErrorST(string s)
		{
			Synergy.Synergy.LogErrorST(s);
		}

		public static void LogVerbose(string s)
		{
			Synergy.Synergy.LogVerbose(s);
		}

		public static MVRScriptUI ScriptUI
		{
			get
			{
				return Synergy.Synergy.Instance
					.UITransform.GetComponentInChildren<MVRScriptUI>();
			}
		}
	}
}
