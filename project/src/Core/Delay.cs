using System;

namespace Synergy
{
	sealed public class Delay : IJsonable
	{
		public const int None = 0;
		public const int HalfwayType = 1;
		public const int EndForwardsType = 2;
		public const int EndBackwardsType = 3;


		private readonly ExplicitHolder<IDuration> halfwayDuration_ =
			new ExplicitHolder<IDuration>();

		private readonly ExplicitHolder<IDuration> endForwardsDuration_ =
			new ExplicitHolder<IDuration>();

		private readonly ExplicitHolder<IDuration> endBackwardsDuration_ =
			new ExplicitHolder<IDuration>();


		private readonly BoolParameter halfway_ =
			new BoolParameter("Halfway", false);

		private readonly BoolParameter endForwards_ =
			new BoolParameter("EndForwards", false);

		private readonly BoolParameter endBackwards_ =
			new BoolParameter("EndBackwards", false);

		private bool sameDelay_ = true;
		private int activeType_ = None;

		public Delay()
			: this(null, false, false)
		{
		}

		public Delay(IDuration d, bool halfway, bool endForwards)
		{
			if (d == null)
				HalfwayDuration = new RandomDuration();
			else
				HalfwayDuration = d;

			EndForwardsDuration = new RandomDuration();
			EndBackwardsDuration = new RandomDuration();
			halfway_.Value = halfway;
			endForwards_.Value = endForwards;
		}

		public void Resume()
		{
			HalfwayDuration.Resume();
			EndForwardsDuration.Resume();
			EndBackwardsDuration.Resume();
		}

		public void Reset()
		{
			HalfwayDuration.Reset();
			EndForwardsDuration.Reset();
			EndBackwardsDuration.Reset();
		}

		public void Removed()
		{
			HalfwayDuration = null;
			EndForwardsDuration = null;
			EndBackwardsDuration = null;
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
			d.HalfwayDuration = HalfwayDuration?.Clone(cloneFlags);
			d.EndForwardsDuration = EndForwardsDuration?.Clone(cloneFlags);
			d.EndBackwardsDuration = EndBackwardsDuration?.Clone(cloneFlags);
			d.halfway_.Value = halfway_.Value;
			d.endForwards_.Value = endForwards_.Value;
			d.endBackwards_.Value = endBackwards_.Value;
			d.sameDelay_ = sameDelay_;
		}


		public IDuration ActiveDuration
		{
			get
			{
				switch (activeType_)
				{
					case HalfwayType:
						return HalfwayDuration;

					case EndForwardsType:
						return EndForwardsDuration;

					case EndBackwardsType:
						return EndBackwardsDuration;

					case None:  // fall-through
					default:
						return null;
				}
			}
		}

		public int ActiveType
		{
			get { return activeType_; }
			set { activeType_ = value; }
		}

		public IDuration SingleDuration
		{
			get { return HalfwayDuration; }
			set { HalfwayDuration = value; }
		}

		public IDuration HalfwayDuration
		{
			get
			{
				return halfwayDuration_.HeldValue;
			}

			set
			{
				if (halfwayDuration_.HeldValue != null)
					halfwayDuration_.HeldValue.Removed();

				halfwayDuration_.Set(value);
			}
		}

		public IDuration EndForwardsDuration
		{
			get
			{
				if (sameDelay_)
					return halfwayDuration_.HeldValue;
				else
					return endForwardsDuration_.HeldValue;
			}

			set
			{
				if (endForwardsDuration_.HeldValue != null)
					endForwardsDuration_.HeldValue.Removed();

				endForwardsDuration_.Set(value);
			}
		}

		public IDuration EndBackwardsDuration
		{
			get
			{
				if (sameDelay_)
					return halfwayDuration_.HeldValue;
				else
					return endBackwardsDuration_.HeldValue;
			}

			set
			{
				if (endBackwardsDuration_.HeldValue != null)
					endBackwardsDuration_.HeldValue.Removed();

				endBackwardsDuration_.Set(value);
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

		public bool StopAfter { get; set; } = false;
		public bool ResetDurationAfter { get; set; } = false;

		public bool SameDelay
		{
			get { return sameDelay_; }
			set { sameDelay_ = value; }
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("halfwayDuration", HalfwayDuration);
			o.Add("endForwardsDuration", EndForwardsDuration);
			o.Add("endBackwardsDuration", EndBackwardsDuration);
			o.Add("halfway", Halfway);
			o.Add("endForwards", EndForwards);
			o.Add("endBackwards", EndBackwards);
			o.Add("sameDelay", sameDelay_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("StepDelay");
			if (o == null)
				return false;

			IDuration d = null;

			if (o.HasKey("duration"))
			{
				// migration from v3
				o.Opt<DurationFactory, IDuration>("duration", ref d);
				HalfwayDuration = d;
				EndForwardsDuration = new RandomDuration();
				EndBackwardsDuration = new RandomDuration();
			}
			else
			{
				o.Opt<DurationFactory, IDuration>("halfwayDuration", ref d);
				HalfwayDuration = d;

				o.Opt<DurationFactory, IDuration>("endForwardsDuration", ref d);
				EndForwardsDuration = d;

				o.Opt<DurationFactory, IDuration>("endBackwardsDuration", ref d);
				EndBackwardsDuration = d;
			}

			o.Opt("halfway", halfway_);
			o.Opt("endForwards", endForwards_);
			o.Opt("endBackwards", endBackwards_);
			o.Opt("sameDelay", ref sameDelay_);

			return true;
		}
	}
}
