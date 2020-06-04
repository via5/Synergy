namespace Synergy
{
	class Delay : IJsonable
	{
		private IDuration duration_ = new RandomDuration();

		private bool halfway_ = false;
		private bool endForwards_ = false;
		private bool endBackwards_ = false;


		public Delay()
		{
		}

		public Delay(IDuration d, bool halfway, bool endForwards)
		{
			if (d != null)
				duration_ = d;

			halfway_ = halfway;
			endForwards_ = endForwards;
		}

		public Delay Clone(int cloneFlags = 0)
		{
			var d = new Delay();
			CopyTo(d, cloneFlags);
			return d;
		}

		private void CopyTo(Delay d, int cloneFlags)
		{
			d.duration_ = duration_?.Clone(cloneFlags);
			d.halfway_ = halfway_;
			d.endForwards_ = endForwards_;
			d.endBackwards_ = endBackwards_;
		}

		public IDuration Duration
		{
			get { return duration_; }
			set { duration_ = value; }
		}

		public bool Halfway
		{
			get { return halfway_; }
			set { halfway_ = value; }
		}

		public bool EndForwards
		{
			get { return endForwards_; }
			set { endForwards_ = value; }
		}

		public bool EndBackwards
		{
			get { return endBackwards_; }
			set { endBackwards_ = value; }
		}

		public bool Active { get; set; } = false;
		public bool StopAfter { get; set; } = false;
		public bool ResetDurationAfter { get; set; } = false;

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("duration", duration_);
			o.Add("halfway", Halfway);
			o.Add("endForwards", EndForwards);
			o.Add("endBackwards", EndBackwards);
			o.Add("endForwards", EndForwards);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("StepDelay");
			if (o == null)
				return false;

			o.Opt<DurationFactory, IDuration>("duration", ref duration_);
			o.Opt("halfway", ref halfway_);
			o.Opt("endForwards", ref endForwards_);
			o.Opt("endBackwards", ref endBackwards_);
			o.Opt("endForwards", ref endForwards_);

			return true;
		}
	}
}
