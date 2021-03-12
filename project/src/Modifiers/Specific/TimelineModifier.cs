namespace Synergy
{
	class TimelineModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "timeline";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Timeline";
		public override string GetDisplayName() { return DisplayName; }

		private string anim_ = "";
		private Integration.Gaze gaze_ = new Integration.Gaze();
		private Integration.Blink blink_ = new Integration.Blink();
		private Integration.Timeline tl_ = new Integration.Timeline();
		private bool disableEyeModifiers_;
		private Delay delay_ = new Delay();

		public string Animation
		{
			get { return anim_; }
			set { anim_ = value; }
		}

		public Integration.Gaze Gaze
		{
			get { return gaze_; }
		}

		public Integration.Blink Blink
		{
			get { return blink_; }
		}

		public bool DisableEyeModifiers
		{
			get { return disableEyeModifiers_; }
			set { disableEyeModifiers_ = value; }
		}

		public Delay Delay
		{
			get { return delay_; }
		}


		public override void Reset()
		{
			base.Reset();
			delay_.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);

			if (delay_.ActiveType == Delay.None)
			{
				if (!tl_.IsPlaying)
				{
					tl_.Play(anim_);
					delay_.ActiveType = Delay.EndForwardsType;
				}
			}
			else
			{
				if (!tl_.IsPlaying)
				{
					delay_.ActiveDuration.Tick(deltaTime);

					if (delay_.ActiveDuration.Finished)
					{
						delay_.ActiveDuration.Reset();
						delay_.ActiveType = Delay.None;
					}
				}
			}
		}


		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new TimelineModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected void CopyTo(TimelineModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);

			m.anim_ = anim_;
			m.gaze_ = gaze_.Clone();
			m.blink_ = blink_.Clone();
			m.tl_ = tl_.Clone();
			m.disableEyeModifiers_ = disableEyeModifiers_;
			m.delay_ = delay_.Clone(cloneFlags);
		}



		protected override string MakeName()
		{
			string s = "TL";

			if (anim_ != "")
				s += " " + anim_;

			return s;
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();
			tl_.Atom = Atom;
			gaze_.Atom = Atom;
			blink_.Atom = Atom;
		}

		public override FloatRange PreferredRange
		{
			get { return new FloatRange(); }
		}
	}
}
