using System.Collections.Generic;

namespace Synergy
{
	class Options : IJsonable
	{
		public const int LogLevelError = 0;
		public const int LogLevelWarn = 1;
		public const int LogLevelInfo = 2;
		public const int LogLevelVerbose = 3;

		private bool resetValuesOnFreeze_ = false;
		private bool resetCountersOnThaw_ = false;
		private bool pickingAnimatable_ = false;
		private float overlapTime_ = 1;
		private int logLevel_ = LogLevelInfo;
		private bool logOverlap_ = false;
		private bool newUI_ = false;

		public static List<string> GetLogLevelNames()
		{
			var list = new List<string>();

			foreach (var i in GetLogLevels())
				list.Add(LogLevelToString(i));

			return list;
		}

		public static List<int> GetLogLevels()
		{
			return new List<int>()
			{
				LogLevelError,
				LogLevelWarn,
				LogLevelInfo,
				LogLevelVerbose
			};
		}

		public static string LogLevelToString(int i)
		{
			switch (i)
			{
				case LogLevelError:   return "Error";
				case LogLevelWarn:    return "Warning";
				case LogLevelInfo:    return "Info";
				case LogLevelVerbose: return "Verbose";
				default:              return "?";
			}
		}

		public static int LogLevelFromString(string s)
		{
			var names = GetLogLevelNames();
			for (int i = 0; i < names.Count; ++i)
			{
				if (names[i] == s)
					return i;
			}

			return -1;
		}


		public Options()
		{
			newUI_ = (SuperController.singleton.GetAtomByUid("synergyuitest") != null);
		}

		public bool ResetValuesOnFreeze
		{
			get { return resetValuesOnFreeze_; }
			set { resetValuesOnFreeze_ = value; }
		}

		public bool ResetCountersOnThaw
		{
			get { return resetCountersOnThaw_; }
			set { resetCountersOnThaw_ = value; }
		}

		public bool PickAnimatable
		{
			get { return pickingAnimatable_; }
			set { pickingAnimatable_ = value; }
		}

		public float OverlapTime
		{
			get { return overlapTime_; }
			set { overlapTime_ = value; }
		}

		public int LogLevel
		{
			get { return logLevel_; }
			set { logLevel_ = value; }
		}

		public bool LogOverlap
		{
			get { return logOverlap_; }
			set { logOverlap_ = value; }
		}

		public bool NewUI
		{
			get { return newUI_; }
		}

		public void SetNewUI(bool b)
		{
			newUI_ = b;
			Synergy.Instance.UI.NeedsReset("new ui " + b.ToString());
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("resetValuesOnFreeze", resetValuesOnFreeze_);
			o.Add("resetCountersOnThaw", resetCountersOnThaw_);
			o.Add("overlapTime", overlapTime_);
			o.Add("newUI", newUI_);

			return o;
		}

		public bool FromJSON(J.Node node)
		{
			var o = node.AsObject("Options");
			if (o == null)
				return false;

			o.Opt("resetValuesOnFreeze", ref resetValuesOnFreeze_);
			o.Opt("resetCountersOnThaw", ref resetCountersOnThaw_);
			o.Opt("overlapTime", ref overlapTime_);
			o.Opt("newUI", ref newUI_);

			return true;
		}
	}
}
