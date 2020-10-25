using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	class StepExtenderModifierUI : AtomModifierUI
	{
		public override string ModifierType
		{
			get { return StepExtenderModifier.FactoryTypeName; }
		}

		public StepExtenderModifierUI(MainUI ui)
			: base(ui)
		{
		}
	}
}
