using System;

namespace Synergy
{
	sealed class Delay : IJsonable
	{
		private readonly ExplicitHolder<IDuration> duration_ =
			new ExplicitHolder<IDuration>();

		private readonly BoolParameter halfway_ =
			new BoolParameter("Halfway", false);

		private readonly BoolParameter endForwards_ =
			new BoolParameter("EndForwards", false);

		private readonly BoolParameter endBackwards_ =
			new BoolParameter("EndBackwards", false);


		public Delay()
			: this(null, false, false)
		{
		}

		public Delay(IDuration d, bool halfway, bool endForwards)
		{
			if (d == null)
				Duration = new RandomDuration();
			else
				Duration = d;

			halfway_.Value = halfway;
			endForwards_.Value = endForwards;
		}

		public void Removed()
		{
			Duration = null;
			halfway_.Unregister();
			endForwards_.Unregister();
			endBackwards_.Unregister();
		}

		public Delay Clone(int cloneFlags = 0)
		{
			var d = new Delay();
			CopyTo(d, cloneFlags);
			return d;
		}

		private void CopyTo(Delay d, int cloneFlags)
		{
			d.Duration = Duration?.Clone(cloneFlags);
			d.halfway_.Value = halfway_.Value;
			d.endForwards_.Value = endForwards_.Value;
			d.endBackwards_.Value = endBackwards_.Value;
		}

		public IDuration Duration
		{
			get
			{
				return duration_.HeldValue;
			}

			set
			{
				if (duration_.HeldValue != null)
					duration_.HeldValue.Removed();

				duration_.Set(value);
			}
		}

		public bool Halfway
		{
			get { return halfway_.Value; }
			set { halfway_.Value = value; }
		}

		public BoolParameter HalfwayParameter
		{
			get { return halfway_; }
		}

		public bool EndForwards
		{
			get { return endForwards_.Value; }
			set { endForwards_.Value = value; }
		}

		public BoolParameter EndForwardsParameter
		{
			get { return endForwards_; }
		}

		public bool EndBackwards
		{
			get { return endBackwards_.Value; }
			set { endBackwards_.Value = value; }
		}

		public BoolParameter EndBackwardsParameter
		{
			get { return endBackwards_; }
		}

		public bool Active { get; set; } = false;
		public bool StopAfter { get; set; } = false;
		public bool ResetDurationAfter { get; set; } = false;

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("duration", Duration);
			o.Add("halfway", Halfway);
			o.Add("endForwards", EndForwards);
			o.Add("endBackwards", EndBackwards);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("StepDelay");
			if (o == null)
				return false;

			IDuration d = null;
			o.Opt<DurationFactory, IDuration>("duration", ref d);
			Duration = d;

			o.Opt("halfway", halfway_);
			o.Opt("endForwards", endForwards_);
			o.Opt("endBackwards", endBackwards_);

			return true;
		}
	}
}
