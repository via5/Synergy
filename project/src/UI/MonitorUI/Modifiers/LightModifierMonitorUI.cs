namespace Synergy
{
	class LightModifierMonitor : ModifierWithMovementMonitor
	{
		public override string ModifierType
		{
			get { return LightModifier.FactoryTypeName; }
		}
	}
}
