namespace Synergy
{
	class TimelineModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "timeline";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Timeline";
		public override string GetDisplayName() { return DisplayName; }

		private string anim_ = "";
		private Integration.Gaze gazeActive_ = new Integration.Gaze();
		private Integration.Gaze gazeInactive_ = new Integration.Gaze();
		private Integration.Blink blinkActive_ = new Integration.Blink();
		private Integration.Blink blinkInactive_ = new Integration.Blink();
		private Integration.Timeline tl_ = new Integration.Timeline();
		private bool inhibitEyeModifiers_ = false;
		private Delay delay_ = new Delay();
		private bool active_ = false;

		public string Animation
		{
			get { return anim_; }
			set { anim_ = value; }
		}

		public Integration.Gaze GazeActive
		{
			get { return gazeActive_; }
		}

		public Integration.Gaze GazeInactive
		{
			get { return gazeInactive_; }
		}

		public Integration.Blink BlinkActive
		{
			get { return blinkActive_; }
		}

		public Integration.Blink BlinkInactive
		{
			get { return blinkInactive_; }
		}

		public bool InhibitEyeModifiers
		{
			get { return inhibitEyeModifiers_; }
			set { inhibitEyeModifiers_ = value; }
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
				if (tl_.IsPlaying)
				{
					//Synergy.LogError(tl_.TimeRemaining.ToString());
				}
				else
				{
					tl_.Play(anim_);
					active_ = true;
					gazeActive_.Check();
					blinkActive_.Check();

					if (delay_.EndForwards)
						delay_.ActiveType = Delay.EndForwardsType;
				}

				if (inhibitEyeModifiers_)
					ParentStep.AddInhibit(EyeModifierType);
			}
			else
			{
				if (tl_.IsPlaying)
				{
					if (inhibitEyeModifiers_)
						ParentStep.AddInhibit(EyeModifierType);

					//	Synergy.LogError(tl_.TimeRemaining.ToString());
				}
				else
				{
					if (active_)
					{
						active_ = false;
						gazeInactive_.Check();
						blinkInactive_.Check();
					}

					delay_.ActiveDuration.Tick(deltaTime);

					if (delay_.ActiveDuration.Finished)
					{
						delay_.ActiveDuration.Reset();
						delay_.ActiveType = Delay.None;
					}
				}
			}
		}

		public override void Removed()
		{
			base.Removed();
			delay_.Removed();
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
			m.gazeActive_ = gazeActive_.Clone();
			m.gazeInactive_ = gazeInactive_.Clone();
			m.blinkActive_ = blinkActive_.Clone();
			m.blinkInactive_ = blinkInactive_.Clone();
			m.tl_ = tl_.Clone();
			m.inhibitEyeModifiers_ = inhibitEyeModifiers_;
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
			gazeActive_.Atom = Atom;
			gazeInactive_.Atom = Atom;
			blinkActive_.Atom = Atom;
			blinkInactive_.Atom = Atom;
		}

		public override FloatRange PreferredRange
		{
			get { return new FloatRange(); }
		}
	}
}
