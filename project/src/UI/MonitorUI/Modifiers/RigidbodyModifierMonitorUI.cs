namespace Synergy
{
	class RigidbodyModifierMonitor : ModifierWithMovementMonitor
	{
		public override string ModifierType
		{
			get { return RigidbodyModifier.FactoryTypeName; }
		}

		private RigidbodyModifier modifier_ = null;

		public RigidbodyModifierMonitor()
		{
		}

		public override void AddToUI(IModifier m)
		{
			base.AddToUI(m);

			modifier_ = m as RigidbodyModifier;
			if (modifier_ == null)
				return;
		}
	}
}
