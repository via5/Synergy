using System;
using System.Collections.Generic;

namespace Synergy
{
	public interface IDuration : IFactoryObject
	{
		float FirstHalfProgress { get; }
		float SecondHalfProgress { get; }
		bool InFirstHalf { get; }
		float TotalProgress { get; }
		bool InFirstHalfTotal { get; }
		bool FirstHalfFinished { get; }
		bool Finished { get; }
		float TimeRemaining { get; }
		float TimeRemainingInHalf { get; }
		float Current { get; }

		IDuration Clone(int cloneFlags = 0);
		void Removed();

		void Tick(float delta);
		void Reset();
		void Reset(float maxTime);
	}


	public sealed class DurationFactory : BasicFactory<IDuration>
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


	public abstract class BasicDuration : IDuration
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract float FirstHalfProgress { get; }
		public abstract float SecondHalfProgress { get; }
		public abstract float TotalProgress { get; }
		public abstract bool InFirstHalf { get; }
		public abstract bool InFirstHalfTotal { get; }
		public abstract bool FirstHalfFinished { get; }
		public abstract bool Finished { get; }
		public abstract float TimeRemaining { get; }
		public abstract float TimeRemainingInHalf { get; }
		public abstract float Current { get; }

		public abstract IDuration Clone(int cloneFlags = 0);

		public virtual void Removed()
		{
			// no-op
		}

		public abstract void Tick(float delta);
		public abstract void Reset();
		public abstract void Reset(float maxTime);

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
	}


	public sealed class RandomDuration : BasicDuration
	{
		public static string FactoryTypeName { get; } = "randomRange";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random range";
		public override string GetDisplayName() { return DisplayName; }

		private readonly ExplicitHolder<RandomizableTime> time_ =
			new ExplicitHolder<RandomizableTime>();


		public RandomDuration()
			: this(1)
		{
		}

		public RandomDuration(float s, float r=0)
		{
			Time = new RandomizableTime(s, r, 0);
		}


		// [0-1] for first the first half
		//
		public override float FirstHalfProgress
		{
			get { return Time.FirstHalfProgress; }
		}

		// [0-1] for the second half
		//
		public override float SecondHalfProgress
		{
			get { return Time.SecondHalfProgress; }
		}

		// whether overall progress is <= 0.5
		//
		public override bool InFirstHalf
		{
			get { return Time.InFirstHalf; }
		}

		// whether overall progress is > 0.5
		//
		public override bool FirstHalfFinished
		{
			get { return !Time.InFirstHalf; }
		}

		// [0-1] for the current half
		//
		public override float TotalProgress
		{
			get
			{
				if (Time.Current <= 0)
					return 1;

				float f;

				if (InFirstHalf)
					f = Time.Elapsed / (Time.Current / 2);
				else
					f = (Time.Elapsed - 0.5f) / (Time.Current / 2);

				return Utilities.Clamp(f, 0, 1);
			}
		}

		// same as InFirstHalf
		//
		public override bool InFirstHalfTotal
		{
			get { return Time.InFirstHalf; }
		}

		// whether the duration has elapsed completely
		//
		public override bool Finished
		{
			get { return Time.Finished; }
		}

		// remaining time in the duration
		//
		public override float TimeRemaining
		{
			get
			{
				return Time.TimeRemaining;
			}
		}

		// remaining time in the current half
		//
		public override float TimeRemainingInHalf
		{
			get
			{
				return Time.TimeRemainingInHalf;
			}
		}

		// current duration
		//
		public override float Current
		{
			get { return Time.Current; }
		}

		// underlying random time
		//
		public RandomizableTime Time
		{
			get
			{
				return time_.HeldValue;
			}

			private set
			{
				if (time_.HeldValue != null)
					time_.HeldValue.Removed();

				time_.Set(value);
			}
		}


		public override IDuration Clone(int cloneFlags = 0)
		{
			var d = new RandomDuration();
			CopyTo(d, cloneFlags);
			return d;
		}

		private void CopyTo(RandomDuration d, int cloneFlags)
		{
			d.Time = Time.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			Time = null;
		}

		public override void Tick(float deltaTime)
		{
			Time.Tick(deltaTime);
		}

		public override void Reset()
		{
			Time.Reset();
		}

		public override void Reset(float maxTime)
		{
			Time.Reset(maxTime);
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("time", Time);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("RandomDuration");
			if (o == null)
				return false;

			RandomizableTime t = null;
			o.Opt("time", ref t);
			Time = t;

			return true;
		}
	}


	public sealed class RampDuration : BasicDuration
	{
		public static string FactoryTypeName { get; } = "ramp";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Ramp";
		public override string GetDisplayName() { return DisplayName; }

		private IEasing easing_ = new LinearEasing();

		private readonly FloatParameter min_ =
			new FloatParameter("Minimum", 1, 10);

		private readonly FloatParameter max_ =
			new FloatParameter("Maximum", 1, 10);

		private readonly FloatParameter timeUp_ =
			new FloatParameter("RampTimeUp", 1, 10);

		private readonly FloatParameter timeDown_ =
			new FloatParameter("RampTimeDown", 1, 10);

		private readonly FloatParameter hold_ =
			new FloatParameter("HoldMaximum", 0, 10);

		private readonly BoolParameter rampUp_ =
			new BoolParameter("RampUp", true);

		private readonly BoolParameter rampDown_ =
			new BoolParameter("RampDown", true);

		private bool goingUp_ = true;
		private float current_ = 0;
		private float elapsed_ = 0;
		private float totalElapsed_ = 0;
		private bool holding_ = false;
		private float holdingElapsed_ = 0;
		private bool finished_ = false;

		public RampDuration()
			: this(1, 1, 1, 0)
		{
		}

		public RampDuration(float min, float max, float over, float hold)
			: this(min, max, over, over, hold)
		{
		}

		public RampDuration(
			float min, float max, float timeUp, float timeDown, float hold)
		{
			min_.Value = min;
			max_.Value = max;
			timeUp_.Value = timeUp;
			timeDown_.Value = timeDown;
			hold_.Value = hold;
			current_ = min;
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

		public override bool FirstHalfFinished
		{
			get { return TotalProgress >= 1.0f && !holding_; }
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
					t += TimeUp - totalElapsed_;
					t += Hold;
					t += TimeDown;
				}
				else if (holding_)
				{
					t += Hold - holdingElapsed_;
					t += TimeDown;
				}
				else
				{
					t += totalElapsed_;
				}

				return Math.Max(t, 0);
			}
		}

		public override float TimeRemainingInHalf
		{
			get
			{
				float t = 0;

				if (goingUp_)
				{
					t += TimeUp - totalElapsed_;
					t += Hold;
				}
				else if (holding_)
				{
					t += Hold - holdingElapsed_;
				}
				else
				{
					t += TimeUp;
					t += Hold;
					t += TimeDown;
					t -= totalElapsed_;
				}

				return Math.Max(t, 0);
			}
		}

		public override float Current
		{
			get { return current_; }
		}

		public float TimeUp
		{
			get { return timeUp_.Value; }
			set { timeUp_.Value = value; }
		}

		public FloatParameter TimeUpParameter
		{
			get { return timeUp_; }
		}

		public float TimeDown
		{
			get { return timeDown_.Value; }
			set { timeDown_.Value = value; }
		}

		public FloatParameter TimeDownParameter
		{
			get { return timeDown_; }
		}

		public FloatRange Range
		{
			get { return new FloatRange(Minimum, Maximum); }
		}

		public float Minimum
		{
			get { return min_.Value; }
			set { min_.Value = value; }
		}

		public FloatParameter MinimumParameter
		{
			get { return min_; }
		}

		public float Maximum
		{
			get { return max_.Value; }
			set { max_.Value = value; }
		}

		public FloatParameter MaximumParameter
		{
			get { return max_; }
		}

		public float Hold
		{
			get { return hold_.Value; }
			set { hold_.Value = value; }
		}

		public FloatParameter HoldParameter
		{
			get { return hold_; }
		}

		public bool RampUp
		{
			get { return rampUp_.Value; }
			set { rampUp_.Value = value; }
		}

		public BoolParameter RampUpParameter
		{
			get { return rampUp_; }
		}

		public bool RampDown
		{
			get { return rampDown_.Value; }
			set { rampDown_.Value = value; }
		}

		public BoolParameter RampDownParameter
		{
			get { return rampDown_; }
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
				if (goingUp_)
				{
					if (TimeUp <= 0)
						return 1;

					return Utilities.Clamp(totalElapsed_ / TimeUp, 0, 1);
				}
				else
				{
					if (TimeDown <= 0)
						return 1;

					return Utilities.Clamp(totalElapsed_ / TimeDown, 0, 1);
				}
			}
		}

		public float HoldingProgress
		{
			get
			{
				if (Hold <= 0)
					return 1;

				return holdingElapsed_ / Hold;
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

		public override void Removed()
		{
			base.Removed();

			min_.Unregister();
			max_.Unregister();
			timeUp_.Unregister();
			timeDown_.Unregister();
			hold_.Unregister();
			rampUp_.Unregister();
			rampDown_.Unregister();
		}

		private void CopyTo(RampDuration d, int cloneFlags)
		{
			d.easing_ = easing_?.Clone(cloneFlags);

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				d.min_.Value = min_.Value;
				d.max_.Value = max_.Value;
				d.timeUp_.Value = timeUp_.Value;
				d.timeDown_.Value = timeDown_.Value;
				d.hold_.Value = hold_.Value;
			}

			rampUp_.Value = d.rampUp_.Value;
			rampDown_.Value = d.rampDown_.Value;
		}

		public override void Tick(float delta)
		{
			if (!finished_)
			{
				if (goingUp_)
				{
					if (RampUp)
						totalElapsed_ += delta;
					else
						totalElapsed_ = TimeUp;

					if (totalElapsed_ >= TimeUp)
					{
						totalElapsed_ = TimeDown;
						goingUp_ = false;
						Next();
						holding_ = true;
					}
				}
				else
				{
					if (!holding_)
					{
						if (RampDown)
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

			bool wasInFirstHalf = (elapsed_ <= (current_ / 2));
			elapsed_ += delta;
			bool isInSecondHalf = (elapsed_ > (current_ / 2));

			if (holding_)
			{
				holdingElapsed_ += delta;

				if ((holdingElapsed_ >= Hold) && wasInFirstHalf && isInSecondHalf)
				{
					elapsed_ = current_ / 2;
					holding_ = false;
					holdingElapsed_ = 0;
				}
				else if (elapsed_ >= current_)
				{
					elapsed_ = 0;
				}
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
			if ((TimeUp + TimeDown + Hold) > maxTime)
			{
				finished_ = true;
				elapsed_ = 0;
				current_ = 0;
				return;
			}

			Reset();
		}

		private void Next()
		{
			if (!holding_)
			{
				float m = easing_.Magnitude(Progress);

				if (Maximum > Minimum)
					current_ = Minimum + m * Range.Distance;
				else
					current_ = Minimum - m * Range.Distance;
			}
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("easing", easing_);
			o.Add("minimum", min_);
			o.Add("maximum", max_);
			o.Add("timeUp", timeUp_);
			o.Add("timeDown", timeDown_);
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

			o.Opt("minimum", min_);
			o.Opt("maximum", max_);
			o.Opt("hold", hold_);
			o.Opt("rampUp", rampUp_);
			o.Opt("rampDown", rampDown_);

			if (o.HasKey("over"))
			{
				// migration
				var over = new FloatParameter("over", 0, 0);
				o.Opt("over", over);

				TimeUp = over.Value;
				TimeDown = over.Value;
			}
			else
			{
				o.Opt("timeUp", timeUp_);
				o.Opt("timeDown", timeDown_);
			}

			return true;
		}
	}
}
