namespace Synergy
{
	class RigidbodyModifierMonitor : ModifierWithMovementMonitor
	{
		public override string ModifierType
		{
			get { return RigidbodyModifier.FactoryTypeName; }
		}

		private RigidbodyModifier modifier_ = null;
		private readonly FloatSlider mag_;

		public RigidbodyModifierMonitor()
		{
			mag_ = new FloatSlider(
				"Real magnitude", 0, new FloatRange(-500f, 500f), null,
				Widget.Disabled | Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			base.AddToUI(m);

			modifier_ = m as RigidbodyModifier;
			if (modifier_ == null)
				return;

			widgets_.AddToUI(mag_);
		}

		public override void Update()
		{
			base.Update();

			if (modifier_ != null)
				mag_.Value = modifier_.RealMagnitude;
		}
	}
}
