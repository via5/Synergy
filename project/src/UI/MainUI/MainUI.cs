using System;
using System.Collections.Generic;
using UnityEngine;
using MVR.FileManagementSecure;

namespace Synergy
{
	using StepProgressionStringList = FactoryStringList<
		StepProgressionFactory, IStepProgression>;

	class MainUI
	{
		private Manager manager_ = Synergy.Instance.Manager;
		private int currentStep_ = 0;
		private int currentModifier_ = 0;

		private Button toggleMonitor_;
		private StringList stepsList_;
		private Button insertStepBefore_;
		private Button insertStepAfter_;
		private Button cloneStep_;
		private StepProgressionStringList stepProgression_;
		private StepUI step_;

		private StringList modifiersList_;
		private Button addModifier_;
		private Button cloneModifier_;
		private Button cloneModifierZero_;
		private ModifierUI modifier_;

		MonitorUI monitor_;
		OptionsUI options_;

		private bool inMonitor_ = false;
		private bool inOptions_ = false;
		private bool inManageAnimatables_ = false;
		private readonly WidgetList widgets_ = new WidgetList();
		private readonly List<Action> handlerRemovers_ = new List<Action>();

		private bool needsReset_ = false;

		public MainUI()
		{
		}

		public void Create()
		{
			toggleMonitor_ = new Button("Monitor", ToggleMonitor);

			options_ = new OptionsUI(Widget.Right);

			stepsList_ = new StringList(
				"Steps", "", new List<string>(), StepChanged);

			insertStepBefore_ = new Button(
				"Insert step before", InsertStepBefore);

			insertStepAfter_ = new Button(
				"Insert step after", InsertStepAfter);

			cloneStep_ = new Button("Clone step", CloneStep);
			stepProgression_ = new StepProgressionStringList(
				"Step progression", StepProgressionChanged);

			modifiersList_ = new StringList(
				"Modifiers", "", new List<string>(), ModifierChanged,
				Widget.Right);
			addModifier_ = new Button(
				"Add modifier", AddModifier, Widget.Right);
			cloneModifier_ = new Button(
				"Clone modifier", CloneModifier, Widget.Right);
			cloneModifierZero_ = new Button(
				"Clone modifier zero values", CloneModifierZero,
				Widget.Right);

			step_ = new StepUI();
			modifier_ = new ModifierUI(this);
			monitor_ = new MonitorUI();

			ResetUI();
		}

		public void Update()
		{
			if (needsReset_)
				ResetUI();

			if (inMonitor_)
				monitor_.Update();

			//if (inOptions_)
			//	options_.Update();
		}

		public void NeedsReset(string why)
		{
			Synergy.LogVerbose("NeedsReset: " + why);
			needsReset_ = true;
		}

		public void ToggleMonitor()
		{
			inMonitor_ = !inMonitor_;
			NeedsReset("toggling monitor");
		}

		public void ToggleOptions()
		{
			inOptions_ = !inOptions_;
			NeedsReset("toggling options");
		}

		public void ToggleManageAnimatables()
		{
			inManageAnimatables_ = !inManageAnimatables_;
			NeedsReset("toggling manage animatables");
		}


		public Step CurrentStep
		{
			get
			{
				return Synergy.Instance.Manager.GetStep(currentStep_);
			}
		}

		public ModifierContainer CurrentModifier
		{
			get
			{
				var s = CurrentStep;
				if (s == null)
					return null;

				if (currentModifier_ >= 0 && currentModifier_ < s.Modifiers.Count)
					return s.Modifiers[currentModifier_];
				else
					return null;
			}
		}

		private void ResetUI()
		{
			Synergy.LogVerbose("resetting ui");

			ReselectStepAndModifier();

			widgets_.RemoveFromUI();
			step_.RemoveFromUI();
			modifier_.RemoveFromUI();
			monitor_.RemoveFromUI();
			//options_.RemoveFromUI();

			if (inMonitor_)
				AddMonitorToUI();
			else if (inOptions_)
				AddOptionsToUI();
			else if (inManageAnimatables_)
				AddManageAnimatablesToUI();
			else
				AddMainToUI();

			needsReset_ = false;
			Synergy.LogVerbose("done resetting ui");
		}

		private void AddMonitorToUI()
		{
			AddMonitorToggle();
			widgets_.AddToUI(new Label(Version.DisplayString, Widget.Right, TextAnchor.MiddleRight));

			AddStepSelector();
			AddModifierSelector();

			monitor_.AddToUI(CurrentStep, CurrentModifier?.Modifier);
		}

		private void AddOptionsToUI()
		{
			//AddOptionsToggle();
			//options_.AddToUI();
		}

		private void AddManageAnimatablesToUI()
		{
			var b = new Button("Back", ToggleManageAnimatables);
			b.BackgroundColor = Color.green;

			widgets_.AddToUI(b);
			widgets_.AddToUI(new Label("Renaming will break any links", Widget.Right));

			if (Synergy.Instance.Parameters.Count == 0)
			{
				widgets_.AddToUI(new Label("No animatables"));
				return;
			}

			foreach (var p in Synergy.Instance.Parameters)
			{
				Textbox name = null;
				Button toggle = null;

				name = new Textbox("Name", p.Name, (string s) =>
				{
					p.Name = s;
				});

				toggle = new Button("Remove", () =>
				{
					if (p.Registered)
					{
						p.Unregister();
						toggle.Text = "Add";
						toggle.BackgroundColor = Utilities.DefaultButtonColor;
					}
					else
					{
						p.Register();
						toggle.Text = "Remove";
						toggle.BackgroundColor = Color.red;
					}
				}, Widget.Right);

				toggle.BackgroundColor = Color.red;

				widgets_.AddToUI(name);
				widgets_.AddToUI(toggle);
			}
		}

		private void AddMainToUI()
		{
			AddMonitorToggle();
			widgets_.AddToUI(options_.Collapsible);

			AddStepSelector();
			AddStepUI();

			AddModifierSelector();
			AddModifierUI();
		}

		private void AddMonitorToggle()
		{
			widgets_.AddToUI(toggleMonitor_);

			if (inMonitor_)
				toggleMonitor_.Text = "Close monitor";
			else
				toggleMonitor_.Text = "Monitor";

			toggleMonitor_.BackgroundColor = Color.green;
		}

		private void AddStepSelector()
		{
			var stepNames = new List<string>();
			string currentStepName = "";

			for (int i = 0; i < manager_.Steps.Count; ++i)
			{
				var name = manager_.Steps[i].Name;
				stepNames.Add(name);

				if (currentStep_ == i)
					currentStepName = name;
			}

			stepsList_.Choices = stepNames;
			stepsList_.Value = currentStepName;

			widgets_.AddToUI(stepsList_);
		}

		private void AddStepUI()
		{
			widgets_.AddToUI(insertStepBefore_);
			widgets_.AddToUI(insertStepAfter_);
			widgets_.AddToUI(cloneStep_);
			widgets_.AddToUI(stepProgression_);
			widgets_.AddToUI(new SmallSpacer());
			step_.AddToUI(CurrentStep);

			insertStepBefore_.Enabled = (currentStep_ > 0);
			cloneStep_.Enabled = (CurrentStep != null);
			stepProgression_.Value = manager_.StepProgression;
		}

		private void AddModifierSelector()
		{
			if (CurrentStep == null)
				return;

			var modifierNames = new List<string>();
			string currentModifierName = "";

			for (int j = 0; j < CurrentStep.Modifiers.Count; ++j)
			{
				var name = CurrentStep.Modifiers[j].Name;
				modifierNames.Add(name);

				if (j == currentModifier_)
					currentModifierName = name;
			}

			modifiersList_.Choices = modifierNames;
			modifiersList_.Value = currentModifierName;

			widgets_.AddToUI(modifiersList_);
		}

		private void AddModifierUI()
		{
			if (CurrentStep == null)
				return;

			widgets_.AddToUI(addModifier_);
			widgets_.AddToUI(cloneModifier_);
			widgets_.AddToUI(cloneModifierZero_);
			widgets_.AddToUI(new LargeSpacer(Widget.Right));
			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			modifier_.AddToUI(CurrentModifier);

			cloneModifier_.Enabled = (CurrentModifier != null);
			cloneModifierZero_.Enabled = (CurrentModifier != null);
		}


		private void ReselectStepAndModifier()
		{
			if (manager_.Steps.Count > 0)
			{
				if (currentStep_ < 0)
					SelectStep(0);
				else if (currentStep_ >= manager_.Steps.Count)
					SelectStep(manager_.Steps.Count - 1);
				else
					SelectStep(currentStep_);
			}
			else
			{
				SelectStep(-1);
			}

			if (CurrentStep == null || CurrentStep.Modifiers.Count == 0)
			{
				currentModifier_ = -1;
			}
			else
			{
				if (currentModifier_ < 0)
					currentModifier_ = 0;
				else if (currentModifier_ >= CurrentStep.Modifiers.Count)
					currentModifier_ = CurrentStep.Modifiers.Count - 1;
			}
		}

		private void SelectStep(int stepIndex)
		{
			if (CurrentStep != null)
			{
				foreach (var r in handlerRemovers_)
					r();

				handlerRemovers_.Clear();
			}

			if (currentStep_ != stepIndex)
			{
				currentStep_ = stepIndex;
				currentModifier_ = 0;
			}

			if (CurrentStep != null)
			{
				for (int i = 0; i < CurrentStep.Modifiers.Count; ++i)
				{
					int icopy = i;

					var m = CurrentStep.Modifiers[icopy];
					if (m.Modifier != null)
					{
						var mcopy = m.Modifier;

						ModifierNameChangedHandler h =
							() => OnModifierNameChanged(icopy);

						handlerRemovers_.Add(new Action(
							() => mcopy.NameChanged -= h));

						mcopy.NameChanged += h;
					}
				}
			}

			NeedsReset("step selection changed");
		}

		private void SelectModifier(int i)
		{
			currentModifier_ = i;
			NeedsReset("modifier selection changed");
		}


		private void OnModifierNameChanged(int i)
		{
			if (CurrentStep == null)
				return;

			if (i < 0 || i >= CurrentStep.Modifiers.Count)
				return;

			var m = CurrentStep.Modifiers[i].Modifier;
			if (m == null)
				return;

			var choices = new List<string>(modifiersList_.Choices);
			if (i < 0 || i >= choices.Count)
				return;

			var newName = m.Name;
			choices[i] = newName;

			modifiersList_.Choices = choices;

			if (currentModifier_ == i)
				modifiersList_.Value = newName;
		}

		private void InsertStepBefore()
		{
			manager_.InsertStep(currentStep_ < 0 ? 0 : currentStep_);
			SelectStep(currentStep_);
			NeedsReset("step added");
		}

		private void InsertStepAfter()
		{
			manager_.InsertStep(currentStep_ + 1);
			SelectStep(currentStep_ + 1);
			NeedsReset("step added");
		}

		private void CloneStep()
		{
			if (CurrentStep == null)
				return;

			manager_.AddStep(CurrentStep.Clone());
			SelectStep(manager_.Steps.Count - 1);
			NeedsReset("step cloned");
		}

		private void StepProgressionChanged(IStepProgression p)
		{
			manager_.StepProgression = p;
		}

		private void StepChanged(string s)
		{
			var values = stepsList_.Choices;

			for (int i = 0; i < values.Count; ++i)
			{
				if (values[i] == s)
				{
					SelectStep(i);
					return;
				}
			}
		}

		private void AddModifier()
		{
			if (CurrentStep == null)
				return;

			CurrentStep.AddEmptyModifier();
			SelectModifier(CurrentStep.Modifiers.Count - 1);
		}

		private void CloneModifier()
		{
			if (CurrentStep == null || CurrentStep.Modifiers.Count == 0)
				return;

			CurrentStep.AddModifier(CurrentModifier.Clone());
			SelectModifier(CurrentStep.Modifiers.Count - 1);
		}

		private void CloneModifierZero()
		{
			if (CurrentStep == null || CurrentStep.Modifiers.Count == 0)
				return;

			CurrentStep.AddModifier(CurrentModifier.Clone(
				Utilities.CloneZero));

			SelectModifier(CurrentStep.Modifiers.Count - 1);
		}

		private void ModifierChanged(string s)
		{
			var values = modifiersList_.Choices;

			for (int i = 0; i < values.Count; ++i)
			{
				if (values[i] == s)
				{
					SelectModifier(i);
					return;
				}
			}
		}
	}


	class OptionsUI
	{
		private readonly Options options_ = Synergy.Instance.Options;

		private readonly Collapsible collapsible_;
		private readonly Checkbox resetValuesOnFreeze_;
		private readonly Checkbox resetCountersOnThaw_;
		private readonly Checkbox pickAnimatable_;
		private readonly Button manageAnimatable_;

		public OptionsUI(int flags = 0)
		{
			collapsible_ = new Collapsible("Options and presets", null, flags);


			resetValuesOnFreeze_ = new Checkbox(
				"Reset positions on freeze", options_.ResetValuesOnFreeze,
				ResetValuesOnFreezeChanged, flags);

			resetCountersOnThaw_ = new Checkbox(
				"Reset counters on thaw", options_.ResetCountersOnThaw,
				ResetCountersOnThaw, flags);

			pickAnimatable_ = new Checkbox(
				"Pick animatable", PickAnimatableChanged, flags);

			manageAnimatable_ = new Button(
				"Manage animatables", ManageAnimatables, flags);

			collapsible_.Add(resetValuesOnFreeze_);
			collapsible_.Add(resetCountersOnThaw_);
			collapsible_.Add(pickAnimatable_);
			collapsible_.Add(manageAnimatable_);
			collapsible_.Add(new SmallSpacer(flags));


			collapsible_.Add(new Button(
				"Full: save", SaveFull, flags));

			collapsible_.Add(new Button(
				"Full: load, replace everything",
				() => { LoadFull(Utilities.PresetReplace); },
				flags));

			collapsible_.Add(new Button(
				"Full: load, append steps",
				() => { LoadFull(Utilities.PresetAppend); },
				flags));

			collapsible_.Add(new SmallSpacer(flags));


			collapsible_.Add(new Button(
				"Step: save current", SaveStep, flags));

			collapsible_.Add(new Button(
				"Step: load, replace current",
				() => { LoadStep(Utilities.PresetReplace); },
				flags));

			collapsible_.Add(new Button(
				"Step: load, append",
				() => { LoadStep(Utilities.PresetAppend); },
				flags));

			collapsible_.Add(new SmallSpacer(flags));


			collapsible_.Add(new Button(
				"Modifier: save current", SaveModifier, flags));

			collapsible_.Add(new Button(
				"Modifier: load, replace current",
				() => { LoadModifier(Utilities.PresetReplace); },
				flags));

			collapsible_.Add(new Button(
				"Modifier: load, append",
				() => { LoadModifier(Utilities.PresetAppend); },
				flags));


			collapsible_.Add(new SmallSpacer(flags));
		}

		public Collapsible Collapsible
		{
			get { return collapsible_; }
		}


		public void SaveFull()
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.CompletePresetExtension;

					Synergy.Instance.Manager.SavePreset(
						path, Utilities.FullPreset);
				},
				Utilities.CompletePresetExtension,
				Utilities.PresetSavePath);

			var browser = SuperController.singleton.mediaFileBrowserUI;
			browser.SetTextEntry(true);
			browser.ActivateFileNameField();
		}

		public void LoadFull(int flags)
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);
			var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
				Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					Synergy.Instance.Manager.LoadPreset(
						path, Utilities.FullPreset | flags);

					Synergy.Instance.UI.NeedsReset("complete preset loaded");
				},
				Utilities.CompletePresetExtension, Utilities.PresetSavePath,
				false, true, false, null, false, shortcuts);
		}

		public void SaveStep()
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.StepPresetExtension;

					Synergy.Instance.Manager.SavePreset(
						path, Utilities.StepPreset);
				},
				Utilities.StepPresetExtension, Utilities.PresetSavePath);

			var browser = SuperController.singleton.mediaFileBrowserUI;
			browser.SetTextEntry(true);
			browser.ActivateFileNameField();
		}

		public void LoadStep(int flags)
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);
			var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
				Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					Synergy.Instance.Manager.LoadPreset(
						path, Utilities.StepPreset | flags);

					Synergy.Instance.UI.NeedsReset("step preset loaded");
				},
				Utilities.StepPresetExtension, Utilities.PresetSavePath,
				false, true, false, null, false, shortcuts);
		}

		public void SaveModifier()
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.ModifierPresetExtension;

					Synergy.Instance.Manager.SavePreset(
						path, Utilities.ModifierPreset);
				},
				Utilities.ModifierPresetExtension, Utilities.PresetSavePath);

			var browser = SuperController.singleton.mediaFileBrowserUI;
			browser.SetTextEntry(true);
			browser.ActivateFileNameField();
		}

		public void LoadModifier(int flags)
		{
			FileManagerSecure.CreateDirectory(Utilities.PresetSavePath);
			var shortcuts = FileManagerSecure.GetShortCutsForDirectory(
				Utilities.PresetSavePath);

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					Synergy.Instance.Manager.LoadPreset(
						path, Utilities.ModifierPreset | flags);

					Synergy.Instance.UI.NeedsReset("modifier preset loaded");
				},
				Utilities.ModifierPresetExtension, Utilities.PresetSavePath,
				false, true, false, null, false, shortcuts);
		}


		private void ResetValuesOnFreezeChanged(bool b)
		{
			options_.ResetValuesOnFreeze = b;
		}

		private void ResetCountersOnThaw(bool b)
		{
			options_.ResetCountersOnThaw = b;
		}

		private void PickAnimatableChanged(bool b)
		{
			Synergy.Instance.Options.PickAnimatable = b;
			Synergy.Instance.UI.NeedsReset("pick animatable changed");
		}

		private void ManageAnimatables()
		{
			Synergy.Instance.UI.ToggleManageAnimatables();
		}
	}
}
