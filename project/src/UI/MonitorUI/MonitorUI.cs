namespace Synergy
{
	class MonitorUI
	{
		private readonly Label runningStep_;
		private IDurationMonitor duration_ = null;
		private readonly RandomizableTimeMonitorWidgets repeat_;
		private IDurationMonitor delay_ = null;
		private readonly Label waitingFor_;
		private readonly FloatSlider gracePeriod_;
		private IModifierMonitor modifierMonitor_ = null;

		private Step currentStep_ = null;
		private IModifier currentModifier_ = null;
		private readonly WidgetList widgets_ = new WidgetList();

		public MonitorUI()
		{
			runningStep_ = new Label();
			repeat_ = new RandomizableTimeMonitorWidgets("Repeat");
			waitingFor_ = new Label();
			gracePeriod_ = new FloatSlider("Grace period");
		}

		public void AddToUI(Step currentStep, IModifier currentModifier)
		{
			currentStep_ = currentStep;
			currentModifier_ = currentModifier;

			if (currentModifier_ != null)
			{
				if (modifierMonitor_ == null ||
					modifierMonitor_.ModifierType != currentModifier.GetFactoryTypeName())
				{
					modifierMonitor_ = CreateModifierMonitor(currentModifier);
				}
			}

			if (currentStep_?.Duration != null)
			{
				if (duration_ == null ||
					duration_.DurationType != currentStep_.Duration.GetFactoryTypeName())
				{
					duration_ = CreateDurationMonitor(
						"Duration", currentStep_.Duration);
				}
			}

			if (currentStep_?.Delay?.Duration != null)
			{
				if (delay_ == null ||
					delay_.DurationType != currentStep_.Delay.Duration.GetFactoryTypeName())
				{
					delay_ = CreateDurationMonitor(
						"Delay", currentStep_.Delay.Duration);
				}
			}

			widgets_.AddToUI(runningStep_);

			if (duration_ != null)
				duration_.AddToUI(currentStep_.Duration);

			foreach (var w in repeat_.GetWidgets())
				widgets_.AddToUI(w);

			if (delay_ != null)
				delay_.AddToUI(currentStep_.Delay.Duration);

			widgets_.AddToUI(waitingFor_);
			widgets_.AddToUI(gracePeriod_);

			if (modifierMonitor_ != null)
				modifierMonitor_.AddToUI(currentModifier);
		}

		public void RemoveFromUI()
		{
			widgets_.RemoveFromUI();

			if (duration_ != null)
				duration_.RemoveFromUI();

			if (delay_ != null)
				delay_.RemoveFromUI();

			if (modifierMonitor_ != null)
				modifierMonitor_.RemoveFromUI();
		}

		public void Update()
		{
			var runningStep = Synergy.Instance.Manager.CurrentStep;
			if (runningStep == null)
				runningStep_.Text = "Step running: (none)";
			else
				runningStep_.Text = "Step running: " + runningStep.Name;

			if (currentStep_ == null)
				repeat_.SetValue(null);
			else
				repeat_.SetValue(currentStep_.Repeat);

			var wf = currentStep_?.WaitingFor;
			if (wf == null)
				waitingFor_.Text = "Waiting for nothing";
			else
				waitingFor_.Text = "Waiting for " + wf.Name;

			var gp = currentStep_?.GracePeriod;
			if (gp.HasValue)
				gracePeriod_.Value = gp.Value;
			else
				gracePeriod_.Value = 0;

			if (duration_ != null)
				duration_.Update();

			if (delay_ != null)
				delay_.Update();

			if (modifierMonitor_ != null)
				modifierMonitor_.Update();
		}

		private IModifierMonitor CreateModifierMonitor(IModifier m)
		{
			if (m is RigidbodyModifier)
				return new RigidbodyModifierMonitor();
			else if (m is MorphModifier)
				return new MorphModifierMonitor();
			else if (m is LightModifier)
				return new LightModifierMonitor();
			else if (m is AudioModifier)
				return new AudioModifierMonitor();
			else
				return null;
		}

		public static IDurationMonitor CreateDurationMonitor(
			string name, IDuration d, int flags=0)
		{
			if (d is RandomDuration)
				return new RandomDurationMonitor(name, flags);
			else if (d is RampDuration)
				return new RampDurationMonitor(name, flags);
			else
				return null;
		}
	}
}
