namespace Synergy
{
	class Options : IJsonable
	{
		private bool resetValuesOnFreeze_ = false;
		private bool resetCountersOnThaw_ = false;
		private bool verboseLog_ = false;
		private bool pickingAnimatable_ = false;

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

		public bool VerboseLog
		{
			get { return verboseLog_; }
			set { verboseLog_ = value; }
		}

		public bool PickAnimatable
		{
			get { return pickingAnimatable_; }
			set { pickingAnimatable_ = value; }
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("resetValuesOnFreeze", resetValuesOnFreeze_);
			o.Add("resetCountersOnThaw", resetCountersOnThaw_);
			o.Add("verboseLog", verboseLog_);

			return o;
		}

		public bool FromJSON(J.Node node)
		{
			var o = node.AsObject("Options");
			if (o == null)
				return false;

			o.Opt("resetValuesOnFreeze", ref resetValuesOnFreeze_);
			o.Opt("resetCountersOnThaw", ref resetCountersOnThaw_);
			o.Opt("verboseLog", ref verboseLog_);

			return true;
		}
	}
}
