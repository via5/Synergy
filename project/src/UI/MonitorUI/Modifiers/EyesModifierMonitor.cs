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


		private EyesModifier modifier_ = null;

		private readonly Label current_;
		private readonly Label currentPos_;
		private readonly Label head_;
		private readonly Label eyes_;
		private readonly Label saccade_;
		private readonly RandomizableTimeMonitorWidgets saccadeTime_;
		private readonly FloatSlider saccadeMin_, saccadeMax_;
		private readonly FloatSlider minDistance_;


		public EyesModifierMonitor()
		{
			current_ = new Label("", Widget.Right);
			currentPos_ = new Label("", Widget.Right);
			head_ = new Label("", Widget.Right);
			eyes_ = new Label("", Widget.Right);
			saccade_ = new Label("", Widget.Right);
			saccadeTime_ = new RandomizableTimeMonitorWidgets(
				"Saccade", Widget.Right);
			saccadeMin_ = new FloatSlider("Saccade minimum", null, Widget.Right);
			saccadeMax_ = new FloatSlider("Saccade maximum", null, Widget.Right);
			minDistance_ = new FloatSlider("Minimum distance", null, Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			modifier_ = m as EyesModifier;
			if (modifier_ == null)
				return;

			widgets_.AddToUI(current_);
			widgets_.AddToUI(currentPos_);
			widgets_.AddToUI(head_);
			widgets_.AddToUI(eyes_);

			widgets_.AddToUI(saccade_);

			foreach (var w in saccadeTime_.GetWidgets())
				widgets_.AddToUI(w);

			widgets_.AddToUI(saccadeMin_);
			widgets_.AddToUI(saccadeMax_);

			widgets_.AddToUI(minDistance_);

			base.AddToUI(m);
		}

		public override void Update()
		{
			base.Update();

			if (modifier_?.CurrentTarget == null)
			{
				current_.Text = "Current: (none)";
				currentPos_.Text = "Current pos: (none)";
			}
			else
			{
				current_.Text =
					"Current: #" + (modifier_.CurrentRealIndex + 1).ToString() +
					" " + modifier_.CurrentTarget.Name;

				currentPos_.Text =
					"Current pos: " +
					modifier_.CurrentTarget.Position.ToString("F3");
			}

			if (modifier_?.Head == null)
				head_.Text = "Head: (none)";
			else
				head_.Text = "Head: " + Utilities.FullName(modifier_.Head);

			if (modifier_?.EyeTarget == null)
				eyes_.Text = "Eye target: (none)";
			else
				eyes_.Text = "Eye target: " + Utilities.FullName(modifier_.EyeTarget);

			saccade_.Text =
				"Saccade offset: " +
				modifier_?.CurrentSaccadeOffset.ToString("F3") ?? "?";

			saccadeTime_.SetValue(modifier_?.SaccadeTime);
			saccadeMin_.Value = modifier_?.SaccadeMin ?? 0;
			saccadeMax_.Value = modifier_?.SaccadeMax ?? 0;
			minDistance_.Value = modifier_?.MinDistance ?? 0;
		}
	}
}
