namespace Synergy
{
	sealed class Movement : IJsonable
	{
		private IEasing easing_ = new SinusoidalEasing();

		private readonly ExplicitHolder<RandomizableFloat> minimum_ =
			new ExplicitHolder<RandomizableFloat>();

		private readonly ExplicitHolder<RandomizableFloat> maximum_ =
			new ExplicitHolder<RandomizableFloat>();

		private float magnitude_ = 0;
		private bool forwards_ = true;


		public Movement()
			: this(0, 0)
		{
		}

		public Movement(float min, float max)
			: this(
				  new RandomizableFloat(min, 0),
				  new RandomizableFloat(max, 0))
		{
		}

		public Movement(FloatRange r)
			: this(
				  new RandomizableFloat(
					  r.Minimum + (r.Distance / 4),
					  r.Distance / 2),
				  new RandomizableFloat(
					  r.Maximum - (r.Distance / 4),
					  r.Distance / 2))
		{
		}

		public Movement(RandomizableFloat min, RandomizableFloat max)
		{
			Minimum = min;
			Maximum = max;
		}

		public Movement Clone(int cloneFlags = 0)
		{
			var r = new Movement();
			CopyTo(r, cloneFlags);
			return r;
		}

		private void CopyTo(Movement r, int cloneFlags)
		{
			r.easing_ = easing_?.Clone(cloneFlags);

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				r.Minimum = Minimum?.Clone(cloneFlags);
				r.Maximum = Maximum?.Clone(cloneFlags);
			}

			r.magnitude_ = 0;
			r.forwards_ = true;
		}

		public void Removed()
		{
			Minimum = null;
			Maximum = null;
		}


		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}

		public RandomizableFloat Minimum
		{
			get
			{
				return minimum_.HeldValue;
			}

			set
			{
				minimum_.HeldValue?.Removed();
				minimum_.Set(value);
				minimum_.HeldValue?.Reset();
			}
		}

		public RandomizableFloat Maximum
		{
			get
			{
				return maximum_.HeldValue;
			}

			set
			{
				maximum_.HeldValue?.Removed();
				maximum_.Set(value);
				maximum_.HeldValue?.Reset();
			}
		}

		public float Target
		{
			get
			{
				if (forwards_)
					return Maximum.Current;
				else
					return Minimum.Current;
			}
		}

		public float Magnitude
		{
			get { return magnitude_; }
		}

		public void Reset()
		{
			magnitude_ = CalculateMagnitude(0, true);
		}

		public FloatRange CurrentRange
		{
			get
			{
				return new FloatRange(Minimum.Current, Maximum.Current);
			}
		}

		public FloatRange AvailableRange
		{
			get
			{
				if (Maximum.Initial > Minimum.Initial)
				{
					return new FloatRange(
						Minimum.Initial - Minimum.Range,
						Maximum.Initial + Maximum.Range);
				}
				else
				{
					return new FloatRange(
						Maximum.Initial - Maximum.Range,
						Minimum.Initial + Minimum.Range);
				}
			}
		}

		public void Tick(float deltaTime, float progress, bool forwards)
		{
			if (forwards)
			{
				if (forwards_)
					Maximum.Tick(deltaTime);
				else
					Maximum.Reset();
			}
			else
			{
				if (!forwards_)
					Minimum.Tick(deltaTime);
				else
					Minimum.Reset();
			}

			magnitude_ = CalculateMagnitude(progress, forwards);
			forwards_ = forwards;
		}

		public float CalculateMagnitude(float progress, bool forwards)
		{
			float m = CurrentRange.Distance * easing_.Magnitude(
				forwards ? progress : 1 - progress);

			if (CurrentRange.Minimum < CurrentRange.Maximum)
				return CurrentRange.Minimum + m;
			else
				return CurrentRange.Minimum - m;
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("easing", easing_);
			o.Add("minimum", Minimum);
			o.Add("maximum", Maximum);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("Movement");
			if (o == null)
				return false;

			o.Opt<EasingFactory, IEasing>("easing", ref easing_);

			{
				RandomizableFloat m = null;
				o.Opt("minimum", ref m);
				Minimum = m;
			}

			{
				RandomizableFloat m = null;
				o.Opt("maximum", ref m);
				Maximum = m;
			}

			return true;
		}
	}
}
