using System;
using System.Collections.Generic;

namespace Synergy
{
	class DelayMonitor
	{
		private readonly int flags_;
		private Delay delay_ = null;
		private readonly DurationMonitorWidgets singleDuration_;
		private readonly DurationMonitorWidgets halfwayDuration_;
		private readonly DurationMonitorWidgets endForwardsDuration_;
		private readonly DurationMonitorWidgets endBackwardsDuration_;

		public DelayMonitor(int flags=0)
		{
			flags_ = flags;
			singleDuration_ = new DurationMonitorWidgets("Delay", flags);
			halfwayDuration_ = new DurationMonitorWidgets("Halfway delay", flags);
			endForwardsDuration_ = new DurationMonitorWidgets("End forwards delay", flags);
			endBackwardsDuration_ = new DurationMonitorWidgets("End backwards delay", flags);
		}

		public List<IWidget> GetWidgets(Delay d)
		{
			delay_ = d;

			if (delay_ == null)
				return new List<IWidget>();

			if (delay_.SameDelay)
			{
				return singleDuration_.GetWidgets(d.SingleDuration);
			}
			else
			{
				var list = new List<IWidget>();

				list.AddRange(halfwayDuration_.GetWidgets(d.HalfwayDuration));
				list.AddRange(endForwardsDuration_.GetWidgets(d.EndForwardsDuration));
				list.AddRange(endBackwardsDuration_.GetWidgets(d.EndBackwardsDuration));

				return list;
			}
		}

		public void Update()
		{
			if (delay_ == null)
				return;

			if (delay_.SameDelay)
			{
				singleDuration_.Update();
			}
			else
			{
				halfwayDuration_.Update();
				endForwardsDuration_.Update();
				endBackwardsDuration_.Update();
			}
		}
	}


	class OverlapMonitorUI
	{
		private readonly Label osActive_;
		private readonly Label osOverlap_;
		private readonly FloatSlider timeRemaining_;
		private readonly FloatSlider overlapTime_;

		public OverlapMonitorUI(int flags=0)
		{
			osActive_ = new Label("", flags);
			osOverlap_ = new Label("", flags);
			timeRemaining_ = new FloatSlider(
				"Time remaining (overlapper)", null, flags | Widget.Disabled);
			overlapTime_ = new FloatSlider(
				"Overlap time", null, flags | Widget.Disabled);
		}

		public List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				osActive_, osOverlap_, timeRemaining_, overlapTime_
			};
		}

		public void Update(Overlapper o)
		{
			if (o == null)
			{
				osActive_.Text = "Overlap: n/a";
				osOverlap_.Text = "Overlap: n/a";
				timeRemaining_.Value = 0;
				overlapTime_.Value = 0;
			}
			else
			{
				osActive_.Text = "Active: " + o.ActiveTick.ToString();
				osOverlap_.Text = "Overlap: " + o.OverlapTick.ToString();
				timeRemaining_.Value = o.TimeRemainingForCurrent;
				overlapTime_.Set(
					0,
					Math.Max(overlapTime_.Range.Maximum, timeRemaining_.Value),
					o.OverlapTime);
			}
		}
	}


	class MonitorUI
	{
		private readonly Label runningStep_;
		private readonly Checkbox active_;
		private readonly Checkbox enabled_;
		private readonly Checkbox paused_;
		private readonly Button forceCheckEnabled_;
		private readonly Button forceRun_;
		private IDurationMonitor duration_ = null;
		private readonly RandomizableTimeMonitorWidgets repeat_;
		private readonly DelayMonitor delay_;
		private readonly OverlapMonitorUI overlap_;

		private IModifierMonitor modifierMonitor_ = null;

		private Step currentStep_ = null;
		private IModifier currentModifier_ = null;
		private readonly WidgetList widgets_ = new WidgetList();

		public MonitorUI()
		{
			runningStep_ = new Label();
			active_ = new Checkbox("Active");
			enabled_ = new Checkbox("Step enabled");
			paused_ = new Checkbox("Step paused");
			forceCheckEnabled_ = new Button(
				"Force recheck modifiers", ForceCheckEnabled);
			forceRun_ = new Button("Force run this step", ForceRunThis);
			repeat_ = new RandomizableTimeMonitorWidgets("Repeat");
			delay_ = new DelayMonitor();
			overlap_ = new OverlapMonitorUI();
		}

		public void AddToUI(Step currentStep, IModifier currentModifier)
		{
			currentStep_ = currentStep;
			currentModifier_ = currentModifier;

			if (currentStep_ == null)
				return;

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

			widgets_.AddToUI(runningStep_);
			widgets_.AddToUI(active_);
			widgets_.AddToUI(enabled_);
			widgets_.AddToUI(paused_);
			widgets_.AddToUI(forceCheckEnabled_);
			widgets_.AddToUI(forceRun_);

			if (duration_ != null)
				duration_.AddToUI(currentStep_.Duration);

			foreach (var w in repeat_.GetWidgets())
				widgets_.AddToUI(w);

			foreach (var w in delay_.GetWidgets(currentStep_?.Delay))
				widgets_.AddToUI(w);

			foreach (var w in overlap_.GetWidgets())
				widgets_.AddToUI(w);

			if (modifierMonitor_ != null)
				modifierMonitor_.AddToUI(currentModifier);
		}

		public void RemoveFromUI()
		{
			widgets_.RemoveFromUI();

			if (duration_ != null)
				duration_.RemoveFromUI();

			if (modifierMonitor_ != null)
				modifierMonitor_.RemoveFromUI();
		}

		public void Update()
		{
			var runningStep = Synergy.Instance.Manager.CurrentStep;

			if (runningStep == null)
			{
				runningStep_.Text = "(Step running: none)";
			}
			else
			{
				runningStep_.Text =
					"(Step running: " + runningStep.Name + ", " +
					runningStep.EnabledModifiers.Count.ToString() + "/" +
					runningStep.Modifiers.Count.ToString() + " m)";
			}

			if (currentStep_ == null)
			{
				active_.Value = false;
				enabled_.Value = false;
				paused_.Value = false;
			}
			else
			{
				active_.Value = Synergy.Instance.Manager.IsStepActive(currentStep_);
				enabled_.Value = currentStep_.Enabled;
				paused_.Value = currentStep_.Paused;
			}

			if (currentStep_ == null)
				repeat_.SetValue(null);
			else
				repeat_.SetValue(currentStep_.Repeat);

			if (duration_ != null)
				duration_.Update();

			delay_.Update();

			overlap_.Update(
				(Synergy.Instance.Manager.StepProgression
				as OrderedStepProgression)
					?.Overlapper);

			if (modifierMonitor_ != null)
				modifierMonitor_.Update();
		}

		public void ForceCheckEnabled()
		{
			if (currentStep_ == null)
				return;

			currentStep_.ForceGatherEnabledModifiers();
		}

		public void ForceRunThis()
		{
			if (currentStep_ == null)
				return;

			Synergy.Instance.Manager.StepProgression.ForceRun(currentStep_);
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
			else if (m is EyesModifier)
				return new EyesModifierMonitor();
			else if (m is StorableModifier)
				return new StorableModifierMonitor();
			else if (m is StepExtenderModifier)
				return new StepExtenderModifierMonitor();
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


	class StepExtenderModifierMonitor : BasicModifierMonitor
	{
		public override string ModifierType
		{
			get { return StepExtenderModifier.FactoryTypeName; }
		}

	}
}
