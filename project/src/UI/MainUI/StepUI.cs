namespace Synergy
{
	class StepUI
	{
		private Step currentStep_;

		private readonly Header header_;
		private readonly ConfirmableButton delete_;
		private readonly Checkbox enabled_;
		private readonly Checkbox halfMove_;
		private readonly Checkbox useGracePeriod_;

		private readonly Collapsible durationCollapsible_;
		private readonly DurationWidgets durationWidgets_;
		private readonly Collapsible repeatCollapsible_;
		private readonly RandomizableTimeWidgets repeatWidgets_;

		private readonly Collapsible delayCollapsible_;
		private readonly DelayWidgets delayWidgets_;

		private readonly WidgetList widgets_ = new WidgetList();

		public StepUI()
		{
			header_ = new Header( "");

			delete_ = new ConfirmableButton("Delete step", DeleteStep);

			enabled_ = new Checkbox(
				"Step enabled", true, StepEnabledChanged);

			halfMove_ = new Checkbox(
				"Half move", false, StepHalfMoveChanged);

			useGracePeriod_ = new Checkbox(
				"Use grace period", false, UseGracePeriodChanged);

			durationCollapsible_ = new Collapsible("Duration");
			durationWidgets_ = new DurationWidgets(
				"", DurationTypeChanged);

			repeatWidgets_ = new RandomizableTimeWidgets("Repeat");
			repeatCollapsible_ = new Collapsible("Repeat");

			foreach (var w in repeatWidgets_.GetWidgets())
				repeatCollapsible_.Add(w);

			delayCollapsible_ = new Collapsible("Delay");
			delayWidgets_ = new DelayWidgets();
		}

		public void AddToUI(Step s)
		{
			currentStep_ = s;

			if (currentStep_ == null)
				return;

			header_.Text = currentStep_.Name;
			enabled_.Parameter = currentStep_.EnabledParameter;
			halfMove_.Parameter = currentStep_.HalfMoveParameter;
			useGracePeriod_.Value = currentStep_.UseGracePeriod;
			durationWidgets_.SetValue(currentStep_?.Duration);
			repeatWidgets_.SetValue(currentStep_.Repeat, new FloatRange(0, 10));
			delayWidgets_.SetValue(currentStep_?.Delay);

			durationCollapsible_.Clear();
			durationCollapsible_.Add(durationWidgets_.GetWidgets());

			delayCollapsible_.Clear();
			delayCollapsible_.Add(delayWidgets_.GetWidgets());

			widgets_.AddToUI(header_);
			widgets_.AddToUI(delete_);
			widgets_.AddToUI(enabled_);
			widgets_.AddToUI(halfMove_);
			widgets_.AddToUI(useGracePeriod_);

			durationCollapsible_.AddToUI();
			repeatCollapsible_.AddToUI();
			delayCollapsible_.AddToUI();

			widgets_.AddToUI(new LargeSpacer());
			widgets_.AddToUI(new LargeSpacer());
			widgets_.AddToUI(new LargeSpacer());
			widgets_.AddToUI(new LargeSpacer());

			UpdateDelayCheckboxes();
		}

		public void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
			durationCollapsible_.RemoveFromUI();
			repeatCollapsible_.RemoveFromUI();
			delayCollapsible_.RemoveFromUI();
		}

		private void UpdateDelayCheckboxes()
		{
			if (currentStep_ == null)
				return;

			delayWidgets_.HalfMove = currentStep_.HalfMove;
		}

		private void DeleteStep()
		{
			if (currentStep_ != null)
			{
				Synergy.Instance.Manager.DeleteStep(currentStep_);
				Synergy.Instance.UI.NeedsReset("step deleted");
			}
		}

		private void StepEnabledChanged(bool b)
		{
			if (currentStep_ != null)
				currentStep_.Enabled = b;
		}

		private void UseGracePeriodChanged(bool b)
		{
			if (currentStep_ != null)
				currentStep_.UseGracePeriod = b;
		}

		private void DurationTypeChanged(IDuration d)
		{
			if (currentStep_ != null)
			{
				currentStep_.Duration = d;
				Synergy.Instance.UI.NeedsReset("step duration type changed");
			}
		}

		private void StepHalfMoveChanged(bool b)
		{
			if (currentStep_ != null)
			{
				currentStep_.HalfMove = b;
				UpdateDelayCheckboxes();
			}
		}
	}
}
