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


		private readonly Label current_;
		private EyesModifier modifier_ = null;


		public EyesModifierMonitor()
		{
			current_ = new Label("", Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			modifier_ = m as EyesModifier;
			if (modifier_ == null)
				return;

			widgets_.AddToUI(current_);
		}

		public override void Update()
		{
			if (modifier_?.CurrentTarget == null)
			{
				current_.Text = "Current: (none)";
			}
			else
			{
				current_.Text =
					"Current: #" + (modifier_.CurrentIndex + 1).ToString() +
					" " + modifier_.CurrentTarget.Name;
			}
		}
	}
}
