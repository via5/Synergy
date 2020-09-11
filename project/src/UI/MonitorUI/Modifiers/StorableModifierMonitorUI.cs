using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	class StorableModifierMonitor : ModifierWithMovementMonitor
	{
		public override string ModifierType
		{
			get { return StorableModifier.FactoryTypeName; }
		}


		private StorableModifier modifier_ = null;
		private readonly Label currentString_;

		private readonly FloatSlider triggerMag_;
		private readonly Label triggerType_;
		private readonly Label actionCurrentState_, actionLastState_;


		public StorableModifierMonitor()
		{
			currentString_ = new Label("", Widget.Right);

			triggerMag_ = new FloatSlider(
				"Trigger at", null, Widget.Right | Widget.Disabled);

			triggerType_ = new Label("", Widget.Right);
			actionCurrentState_ = new Label("", Widget.Right);
			actionLastState_ = new Label("", Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			modifier_ = m as StorableModifier;
			if (modifier_ == null)
				return;

			if (modifier_.Parameter is StringStorableParameter)
			{
				widgets_.AddToUI(currentString_);
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
			}
			else if (modifier_.Parameter is ActionStorableParameter)
			{
				widgets_.AddToUI(triggerMag_);
				widgets_.AddToUI(triggerType_);
				widgets_.AddToUI(actionCurrentState_);
				widgets_.AddToUI(actionLastState_);
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
			}

			base.AddToUI(m);
		}

		public override void Update()
		{
			base.Update();

			var p = modifier_?.Parameter;

			if (p is StringStorableParameter)
			{
				var sp = p as StringStorableParameter;
				currentString_.Text = "Current: " + (sp.Current ?? " (none)");
			}
			else if (p is ActionStorableParameter)
			{
				var ap = p as ActionStorableParameter;

				triggerMag_.Value = ap.TriggerMagnitude;

				triggerType_.Text =
					"Trigger type: " +
					ActionStorableParameter.TriggerTypeToString(ap.TriggerType);

				actionCurrentState_.Text =
					"State: " + StateToString(ap.CurrentState);

				actionLastState_.Text =
					"Last: " + StateToString(ap.LastState);
			}
		}

		private string StateToString(int i)
		{
			switch (i)
			{
				case ActionStorableParameter.StateGoingUp:
					return "going up";

				case ActionStorableParameter.StateGoingDown:
					return "going down";

				case ActionStorableParameter.StateGoingUpTriggered:
					return "mag reached, triggered";

				case ActionStorableParameter.StateGoingUpIgnored:
					return "mag reached (ignored)";

				case ActionStorableParameter.StateGoingDownTriggered:
					return "mag left, triggered";

				case ActionStorableParameter.StateGoingDownIgnored:
					return "mag left(ignored)";

				case ActionStorableParameter.StateNone:
				default:
					return "none";
			}
		}
	}
}
