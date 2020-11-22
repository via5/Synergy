namespace Synergy
{
	class LinkModifierUI : AtomModifierUI
	{
		private LinkModifier modifier_ = null;

		public override string ModifierType
		{
			get { return LinkModifier.FactoryTypeName; }
		}


		public LinkModifierUI(MainUI ui)
			: base(ui)
		{
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as LinkModifier;
			if (modifier_ == null)
				return;

			AddAtomWidgets(m);

			base.AddToTopUI(m);
		}
	}
}
