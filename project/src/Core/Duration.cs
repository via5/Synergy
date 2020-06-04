using System;
using System.Collections.Generic;

namespace Synergy
{
	interface IDuration : IFactoryObject
	{
		float FirstHalfProgress { get; }
		float SecondHalfProgress { get; }
		bool InFirstHalf { get; }
		float TotalProgress { get; }
		bool InFirstHalfTotal { get; }
		bool Finished { get; }
		float TimeRemaining { get; }
		float Current { get; }

		IDuration Clone(int cloneFlags = 0);

		void Tick(float delta);
		void Reset();
		void Reset(float maxTime);
	}


	class DurationFactory : BasicFactory<IDuration>
	{
		public override List<IDuration> GetAllObjects()
		{
			return new List<IDuration>()
			{
				new RandomDuration(),
				new RampDuration()
			};
		}
	}


	abstract class BasicDuration : IDuration
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract float FirstHalfProgress { get; }
		public abstract float SecondHalfProgress { get; }
		public abstract float TotalProgress { get; }
		public abstract bool InFirstHalf { get; }
		public abstract bool InFirstHalfTotal { get; }
		public abstract bool Finished { get; }
		public abstract float TimeRemaining { get; }
		public abstract float Current { get; }

		public abstract IDuration Clone(int cloneFlags = 0);

		public abstract void Tick(float delta);
		public abstract void Reset();
		public abstract void Reset(float maxTime);

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
	}


	class RandomDuration : BasicDuration
	{
		public static string FactoryTypeName { get; } = "randomRange";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random range";
		public override string GetDisplayName() { return DisplayName; }

		private RandomizableTime time_ = new RandomizableTime(1);


		public RandomDuration()
		{
		}

		public RandomDuration(float s, float r=0)
		{
			time_ = new RandomizableTime(s, r, 0);
		}

		public override float FirstHalfProgress
		{
			get { return time_.FirstHalfProgress; }
		}

		public override float SecondHalfProgress
		{
			get { return time_.SecondHalfProgress; }
		}

		public override bool InFirstHalf
		{
			get { return time_.InFirstHalf; }
		}

		public override float TotalProgress
		{
			get
			{
				if (time_.Current <= 0)
					return 1;

				return time_.Elapsed / (time_.Current / 2);
			}
		}

		public override bool InFirstHalfTotal
		{
			get { return time_.InFirstHalf; }
		}

		public override bool Finished
		{
			get { return time_.Finished; }
		}

		public override float TimeRemaining
		{
			get
			{
				return time_.TimeRemaining;
			}
		}

		public override float Current
		{
			get { return time_.Current; }
		}

		public RandomizableTime Time
		{
			get
			{
				return time_;
			}
		}

		public override IDuration Clone(int cloneFlags = 0)
		{
			var d = new RandomDuration();
			CopyTo(d, cloneFlags);
			return d;
		}

		protected void CopyTo(RandomDuration d, int cloneFlags)
		{
			d.time_ = time_.Clone(cloneFlags);
		}

		public override void Tick(float delta)
		{
			time_.Tick(delta);
		}

		public override void Reset()
		{
			time_.Reset();
		}

		public override void Reset(float maxTime)
		{
			time_.Reset(maxTime);
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("time", time_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RandomDuration");
			if (o == null)
				return false;

			o.Opt("time", ref time_);

			return true;
		}
	}


	class RampDuration : BasicDuration
	{
		public static string FactoryTypeName { get; } = "ramp";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Ramp";
		public override string GetDisplayName() { return DisplayName; }

		private IEasing easing_ = new LinearEasing();
		private float min_ = 0;
		private float max_ = 0;
		private float over_ = 1;
		private float hold_ = 0;
		private bool rampUp_ = true;
		private bool rampDown_ = true;

		private bool goingUp_ = true;
		private float current_ = 0;
		private float elapsed_ = 0;
		private float totalElapsed_ = 0;
		private bool holding_ = false;
		private float holdingElapsed_ = 0;
		private bool finished_ = false;

		public RampDuration()
		{
		}

		public RampDuration(float min, float max, float over, float hold)
		{
			min_ = min;
			max_ = max;
			over_ = over;
			hold_ = hold;
		}

		public override float FirstHalfProgress
		{
			get
			{
				if (current_ <= 0)
					return 1;

				return Utilities.Clamp(elapsed_ / (current_ / 2), 0, 1);
			}
		}

		public override float SecondHalfProgress
		{
			get
			{
				if (current_ <= 0)
					return 1;

				return Utilities.Clamp((elapsed_ - (current_ / 2)) / (current_ / 2), 0, 1);
			}
		}

		public override bool InFirstHalf
		{
			get
			{
				return (elapsed_ < (current_ / 2));
			}
		}

		public override float TotalProgress
		{
			get
			{
				return Progress;
			}
		}

		public override bool InFirstHalfTotal
		{
			get
			{
				return goingUp_;
			}
		}

		public override bool Finished
		{
			get
			{
				return finished_ && elapsed_ >= current_;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				float t = 0;

				if (goingUp_)
				{
					t += over_ - totalElapsed_;
					t += hold_;
					t += over_;
				}
				else if (holding_)
				{
					t += hold_ - holdingElapsed_;
					t += over_;
				}
				else
				{
					t += totalElapsed_;
				}

				return Math.Max(t, 0);
			}
		}

		public override float Current
		{
			get { return current_; }
		}

		public float Over
		{
			get { return over_; }
			set { over_ = value; }
		}

		public FloatRange Range
		{
			get { return new FloatRange(min_, max_); }
		}

		public float Minimum
		{
			get { return min_; }
			set { min_ = value; }
		}

		public float Maximum
		{
			get { return max_; }
			set { max_ = value; }
		}

		public float Hold
		{
			get { return hold_; }
			set { hold_ = value; }
		}

		public bool RampUp
		{
			get { return rampUp_; }
			set { rampUp_ = value; }
		}

		public bool RampDown
		{
			get { return rampDown_; }
			set { rampDown_ = value; }
		}

		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}


		public float Elapsed
		{
			get { return elapsed_; }
		}

		public float TotalElapsed
		{
			get { return totalElapsed_; }
		}

		public float Progress
		{
			get
			{
				if (over_ <= 0)
					return 1;

				return Utilities.Clamp(totalElapsed_ / over_, 0, 1);
			}
		}

		public float HoldingProgress
		{
			get
			{
				if (hold_ <= 0)
					return 1;

				return holdingElapsed_ / hold_;
			}
		}

		public float HoldingElapsed
		{
			get { return holdingElapsed_; }
		}

		public override IDuration Clone(int cloneFlags = 0)
		{
			var d = new RampDuration();
			CopyTo(d, cloneFlags);
			return d;
		}

		protected void CopyTo(RampDuration d, int cloneFlags)
		{
			d.easing_ = easing_?.Clone(cloneFlags);

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				d.min_ = min_;
				d.max_ = max_;
				d.over_ = over_;
				d.current_ = current_;
			}
		}

		public override void Tick(float delta)
		{
			if (!finished_)
			{
				if (goingUp_)
				{
					if (rampUp_)
						totalElapsed_ += delta;
					else
						totalElapsed_ = over_;

					if (totalElapsed_ >= over_)
					{
						totalElapsed_ = over_;
						Next();
						goingUp_ = false;
						holding_ = true;
					}
				}
				else
				{
					if (!holding_)
					{
						if (rampDown_)
						{
							totalElapsed_ -= delta;

							if (totalElapsed_ <= 0)
							{
								totalElapsed_ = 0;
								goingUp_ = true;
								finished_ = true;
							}
						}
						else
						{
							totalElapsed_ = 0;
							goingUp_ = true;
							finished_ = true;
						}
					}
				}
			}

			elapsed_ += delta;

			if (holding_)
			{
				holdingElapsed_ += delta;
				if (holdingElapsed_ >= hold_)
				{
					holding_ = false;
					holdingElapsed_ = 0;
				}

				if (elapsed_ >= current_)
					elapsed_ = 0;
			}
			else
			{
				if (elapsed_ >= current_)
				{
					if (!finished_)
					{
						Next();
						elapsed_ = 0;
					}
				}
			}
		}

		public override void Reset()
		{
			elapsed_ = 0;
			finished_ = false;
			Next();
		}

		public override void Reset(float maxTime)
		{
			if ((over_ * 2 + hold_) > maxTime)
				return ;

			Reset();
		}

		private void Next()
		{
			if (!holding_)
			{
				float m = easing_.Magnitude(Progress);

				if (max_ > min_)
					current_ = min_ + m * Range.Distance;
				else
					current_ = min_ - m * Range.Distance;
			}
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("easing", easing_);
			o.Add("minimum", min_);
			o.Add("maximum", max_);
			o.Add("over", over_);
			o.Add("hold", hold_);
			o.Add("rampUp", rampUp_);
			o.Add("rampDown", rampDown_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RampDuration");
			if (o == null)
				return false;

			o.Opt<EasingFactory, IEasing>("easing", ref easing_);

			o.Opt("minimum", ref min_);
			o.Opt("maximum", ref max_);
			o.Opt("over", ref over_);
			o.Opt("hold", ref hold_);
			o.Opt("rampUp", ref rampUp_);
			o.Opt("rampDown", ref rampDown_);

			return true;
		}
	}
}
