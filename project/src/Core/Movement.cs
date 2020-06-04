namespace Synergy
{
	class Movement : IJsonable
	{
		private IEasing easing_ = new SinusoidalEasing();
		private RandomizableFloat minimum_ = new RandomizableFloat();
		private RandomizableFloat maximum_ = new RandomizableFloat();
		private float magnitude_ = 0;
		private bool forwards_ = true;

		public Movement()
			: this(0, 0)
		{
		}

		public Movement(RandomizableFloat min, RandomizableFloat max)
		{
			minimum_ = min;
			maximum_ = max;
		}

		public Movement(float min, float max)
		{
			minimum_.Initial = min;
			maximum_.Initial = max;

			minimum_.Reset();
			maximum_.Reset();
		}

		public Movement(FloatRange r)
		{
			minimum_.Initial = r.Minimum + (r.Distance / 4);
			minimum_.Range = r.Distance / 2;
			minimum_.Reset();

			maximum_.Initial = r.Maximum - (r.Distance / 4);
			maximum_.Range = r.Distance / 2;
			maximum_.Reset();
		}

		public Movement Clone(int cloneFlags = 0)
		{
			var r = new Movement();
			CopyTo(r, cloneFlags);
			return r;
		}

		protected void CopyTo(Movement r, int cloneFlags)
		{
			r.easing_ = easing_?.Clone(cloneFlags);

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				r.minimum_ = minimum_?.Clone(cloneFlags);
				r.maximum_ = maximum_?.Clone(cloneFlags);
			}

			r.magnitude_ = 0;
			r.forwards_ = true;
		}


		public IEasing Easing
		{
			get { return easing_; }
			set { easing_ = value; }
		}

		public RandomizableFloat Minimum
		{
			get { return minimum_; }
			set { minimum_ = value; }
		}

		public RandomizableFloat Maximum
		{
			get { return maximum_; }
			set { maximum_ = value; }
		}

		public float Target
		{
			get
			{
				if (forwards_)
					return maximum_.Current;
				else
					return minimum_.Current;
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
				return new FloatRange(minimum_.Current, maximum_.Current);
			}
		}

		public FloatRange AvailableRange
		{
			get
			{
				if (maximum_.Initial > minimum_.Initial)
				{
					return new FloatRange(
						minimum_.Initial - minimum_.Range,
						maximum_.Initial + maximum_.Range);
				}
				else
				{
					return new FloatRange(
						maximum_.Initial - maximum_.Range,
						minimum_.Initial + minimum_.Range);
				}
			}
		}

		public void Tick(float deltaTime, float progress, bool forwards)
		{
			if (forwards)
			{
				if (forwards_)
					maximum_.Tick(deltaTime);
				else
					maximum_.Reset();
			}
			else
			{
				if (!forwards_)
					minimum_.Tick(deltaTime);
				else
					minimum_.Reset();
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
			o.Add("minimum", minimum_);
			o.Add("maximum", maximum_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("Movement");
			if (o == null)
				return false;

			o.Opt<EasingFactory, IEasing>("easing", ref easing_);
			o.Opt("minimum", ref minimum_);
			o.Opt("maximum", ref maximum_);

			return true;
		}
	}
}
