using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	abstract class BasicRandomizableValue<T, Parameter> : IJsonable
		where Parameter : BasicParameter<T>
	{
		protected readonly Parameter initial_;
		protected readonly Parameter range_;
		protected readonly FloatParameter interval_;

		protected T current_;
		protected float elapsed_;
		protected float totalElapsed_;


		public BasicRandomizableValue(
			Parameter initial, Parameter range, FloatParameter interval)
		{
			initial_ = initial;
			range_ = range;
			interval_ = interval;

			current_ = initial.Value;
			elapsed_ = 0;
			totalElapsed_ = float.MaxValue;
		}

		protected void CopyTo(
			BasicRandomizableValue<T, Parameter> r, int cloneFlags)
		{
			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				r.initial_.Value = initial_.Value;
				r.range_.Value = range_.Value;
				r.interval_.Value = interval_.Value;
			}

			current_ = initial_.Value;
			elapsed_ = 0;
			totalElapsed_ = float.MaxValue;
		}

		public virtual void Removed()
		{
			initial_.Unregister();
			range_.Unregister();
			interval_.Unregister();
		}

		public T Initial
		{
			get { return initial_.Value; }
			set { initial_.Value = value; }
		}

		public Parameter InitialParameter
		{
			get { return initial_; }
		}

		public T Range
		{
			get { return range_.Value; }
			set { range_.Value = value; }
		}

		public Parameter RangeParameter
		{
			get { return range_; }
		}

		public float Interval
		{
			get { return interval_.Value; }
			set { interval_.Value = value; }
		}

		public FloatParameter IntervalParameter
		{
			get { return interval_; }
		}


		public T Current
		{
			get { return current_; }
		}

		public float Elapsed
		{
			get { return elapsed_; }
		}

		public float TotalElapsed
		{
			get { return totalElapsed_; }
		}

		public virtual float ActualInterval
		{
			get { return interval_.Value; }
		}


		public void Reset()
		{
			elapsed_ = 0;

			if (totalElapsed_ >= (ActualInterval - 0.009f))
			{
				Next();
				elapsed_ = 0;
				totalElapsed_ = 0;
			}
		}

		public virtual void Tick(float delta)
		{
			elapsed_ += delta;
			totalElapsed_ += delta;
		}


		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
		protected abstract void Next();
	}


	class RandomizableFloat : BasicRandomizableValue<float, FloatParameter>
	{
		public RandomizableFloat()
			: this(0, 0, 0)
		{
		}

		public RandomizableFloat(float initial, float range, float interval=0.0f)
			: base(
				  new FloatParameter(
					  "Initial", initial, 500, Parameter.AllowNegative),
				  new FloatParameter(
					  "Range", range, 500, Parameter.AllowNegative),
				  new FloatParameter("Interval", interval, 10))
		{
		}

		public RandomizableFloat Clone(int cloneFlags = 0)
		{
			var r = new RandomizableFloat();
			CopyTo(r, cloneFlags);
			return r;
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("initial", initial_);
			o.Add("range", range_);
			o.Add("interval", interval_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RandomizableFloat");
			if (o == null)
				return false;

			o.Opt("initial", initial_);
			o.Opt("range", range_);
			o.Opt("interval", interval_);

			return true;
		}

		protected override void Next()
		{
			current_ = UnityEngine.Random.Range(
				initial_.Value - range_.Value,
				initial_.Value + range_.Value);
		}
	}


	class RandomizableInt : BasicRandomizableValue<int, IntParameter>
	{
		public RandomizableInt(int initial=0)
			: this(initial, 0, 0)
		{
		}

		public RandomizableInt(int initial, int range, int interval)
			: base(
				  new IntParameter(
					  "Initial", initial, 500, Parameter.AllowNegative),
				  new IntParameter(
					  "Range", range, 500, Parameter.AllowNegative),
				  new FloatParameter("Interval", interval, 10))
		{
		}

		public RandomizableInt Clone(int cloneFlags = 0)
		{
			var r = new RandomizableInt();
			CopyTo(r, cloneFlags);
			return r;
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("initial", initial_);
			o.Add("range", range_);
			o.Add("interval", interval_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RandomizableInt");
			if (o == null)
				return false;

			o.Opt("initial", initial_);
			o.Opt("range", range_);
			o.Opt("interval", interval_);

			return true;
		}

		protected override void Next()
		{
			current_ = UnityEngine.Random.Range(
				initial_.Value - range_.Value,
				initial_.Value + range_.Value);
		}
	}


	class RandomizableTime : BasicRandomizableValue<float, FloatParameter>
	{
		public const int CutoffClosest = 0;
		public const int CutoffFloor = 1;
		public const int CutoffCeil = 2;
		public const int CutoffExact = 3;

		private int cutoff_ = 0;


		public RandomizableTime()
			: this(0, 0, 0)
		{
		}

		public RandomizableTime(float initial)
			: this(initial, 0, 0)
		{
		}

		public RandomizableTime(
			float initial, float range, float interval,
			int cutoff=CutoffClosest)
				: base(
					  new FloatParameter( "Initial", initial, 10),
					  new FloatParameter("Range", range, 10),
					  new FloatParameter("Interval", interval, 10))
		{
			cutoff_ = cutoff;
		}

		public RandomizableTime Clone(int cloneFlags = 0)
		{
			var r = new RandomizableTime();
			CopyTo(r, 0);
			return r;
		}

		protected void CopyTo(RandomizableTime r, int cloneFlags)
		{
			base.CopyTo(r, cloneFlags);
			r.cutoff_ = cutoff_;
		}


		public static List<string> GetCutoffNames()
		{
			return new List<string>()
			{
				"Closest to interval",
				"Always before interval",
				"Always after interval",
				"Exact"
			};
		}

		public static string CutoffToString(int i)
		{
			var names = GetCutoffNames();

			if (i < 0 || i >= names.Count)
				return "?";

			return names[i];
		}

		public static int CutoffFromString(string s)
		{
			var names = GetCutoffNames();

			for (int i = 0; i < names.Count; ++i)
			{
				if (names[i] == s)
					return i;
			}

			return -1;
		}


		public int Cutoff
		{
			get { return cutoff_; }
			set { cutoff_ = value; }
		}

		public float FirstHalfProgress
		{
			get
			{
				if (current_ <= 0)
					return 1;

				float p = elapsed_ / (current_ / 2);
				if (p > 1.0f)
					p = 1.0f;

				return p;
			}
		}

		public float SecondHalfProgress
		{
			get
			{
				if (current_ <= 0)
					return 1;

				float p = (elapsed_ - (current_ / 2)) / (current_ / 2);
				if (p > 1.0f)
					p = 1.0f;

				return p;
			}
		}

		public bool Finished
		{
			get
			{
				return (elapsed_ > current_);
			}
		}

		public bool InFirstHalf
		{
			get
			{
				return (elapsed_ <= (current_ / 2));
			}
		}

		public override float ActualInterval
		{
			get
			{
				if (current_ <= 0)
					return interval_.Value;

				switch (cutoff_)
				{
					case CutoffClosest:
						return ClosestInterval();

					case CutoffFloor:
						return FloorInterval();

					case CutoffCeil:
						return CeilInterval();

					case CutoffExact:
					default:
						return interval_.Value;
				}
			}
		}

		public float TimeRemaining
		{
			get
			{
				return Math.Max(current_ - elapsed_, 0);
			}
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("initial", initial_);
			o.Add("range", range_);
			o.Add("interval", interval_);
			o.Add("cutoff", cutoff_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RandomizableTime");
			if (o == null)
				return false;

			o.Opt("initial", initial_);
			o.Opt("range", range_);
			o.Opt("interval", interval_);
			o.Opt("cutoff", ref cutoff_);

			return true;
		}

		public void Reset(float maxTime)
		{
			if (totalElapsed_ >= (ActualInterval - 0.009f))
			{
				if (Next(maxTime))
				{
					elapsed_ = 0;
					totalElapsed_ = 0;
				}
			}
			else
			{
				if (current_ <= maxTime)
					elapsed_ = 0;
			}
		}

		protected override void Next()
		{
			current_ = UnityEngine.Random.Range(
				initial_.Value - range_.Value,
				initial_.Value + range_.Value);
		}

		private bool Next(float maxTime)
		{
			if (maxTime < (initial_.Value - range_.Value))
			{
				// not enough time left
				return false;
			}

			if (maxTime >= (initial_.Value + range_.Value))
			{
				// plenty of time
				Next();
				return true;
			}
			else
			{
				// clamp
				current_ = maxTime;
				return true;
			}
		}

		private float ClosestInterval()
		{
			if (current_ > interval_.Value)
			{
				return current_;
			}
			else if (current_ < interval_.Value)
			{
				float previous = (float)Math.Floor(interval_.Value / current_) * current_;
				float next = previous + current_;

				float previousDelta = interval_.Value - previous;
				float nextDelta = next - interval_.Value;

				if (previousDelta < nextDelta)
					return previous;
				else
					return next;
			}
			else
			{
				return interval_.Value;
			}
		}

		private float FloorInterval()
		{
			if (current_ > interval_.Value)
				return current_;
			else if (current_ < interval_.Value)
				return (float)Math.Floor(interval_.Value / current_) * current_;
			else
				return interval_.Value;
		}

		private float CeilInterval()
		{
			if (current_ > interval_.Value)
				return current_;
			else if (current_ < interval_.Value)
				return (float)Math.Floor(interval_.Value / current_) * current_ + current_;
			else
				return interval_.Value;
		}
	}
}
