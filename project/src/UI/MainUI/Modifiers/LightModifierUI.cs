using UnityEngine;

namespace Synergy
{
	using LightPropertyStringList = FactoryStringList<
		LightPropertyFactory, ILightProperty>;

	class LightModifierUI : AtomWithMovementUI
	{
		private LightModifier modifier_ = null;
		private readonly LightPropertyStringList property_;

		private readonly ColorPicker color1_;
		private readonly ColorPicker color2_;

		public override string ModifierType
		{
			get { return LightModifier.FactoryTypeName; }
		}


		public LightModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasComponent<Light>)
		{
			property_ = new LightPropertyStringList(
				"Property", "", PropertyChanged, Widget.Right);

			color1_ = new ColorPicker(
				"Color 1", Color.black, Color1Changed, Widget.Right);

			color2_ = new ColorPicker(
				"Color 2", Color.black, Color2Changed, Widget.Right);
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as LightModifier;
			if (modifier_ == null)
				return;

			property_.Value = modifier_.Property;

			AddAtomWidgets(m);
			widgets_.AddToUI(property_);

			if (modifier_ != null)
			{
				var clp = modifier_.Property as ColorLightProperty;
				if (clp != null)
				{
					color1_.Value = clp.Color1;
					color2_.Value = clp.Color2;

					widgets_.AddToUI(color1_);
					widgets_.AddToUI(color2_);
				}
			}

			AddAtomWithMovementWidgets(m);

			base.AddToTopUI(m);
		}

		private void PropertyChanged(ILightProperty p)
		{
			if (modifier_ != null)
			{
				modifier_.Property = p;
				ui_.NeedsReset("light property changed");
			}
		}

		private void Color1Changed(Color c)
		{
			var clp = modifier_?.Property as ColorLightProperty;

			if (clp != null)
				clp.Color1 = color1_.Value;
		}

		private void Color2Changed(Color c)
		{
			var clp = modifier_?.Property as ColorLightProperty;

			if (clp != null)
				clp.Color2 = color2_.Value;
		}
	}
}
