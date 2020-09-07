using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	class EyesModifierMonitor : BasicModifierMonitor
	{
		public override string ModifierType
		{
			get { return EyesModifier.FactoryTypeName; }
		}
	}
}
