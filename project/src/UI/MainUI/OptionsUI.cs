using MVR.FileManagementSecure;

namespace Synergy
{
	class OptionsUI
	{
		private readonly Options options_ = Synergy.Instance.Options;

		private readonly Collapsible collapsible_;
		private readonly Checkbox resetValuesOnFreeze_;
		private readonly Checkbox resetCountersOnThaw_;
		private readonly Checkbox pickAnimatable_;
		private readonly Checkbox loop_;
		private readonly Button manageAnimatable_;
		private readonly FloatSlider overlapTime_;
		private readonly StringList logLevel_;
		private readonly Checkbox logOverlap_;
		private readonly Checkbox usePlaceholder_;

		public OptionsUI(int flags = 0)
		{
			collapsible_ = new Collapsible("Options and presets", null, flags);


			resetValuesOnFreeze_ = new Checkbox(
				"Reset positions on freeze", options_.ResetValuesOnFreeze,
				ResetValuesOnFreezeChanged, flags);

			resetCountersOnThaw_ = new Checkbox(
				"Reset counters on thaw", options_.ResetCountersOnThaw,
				ResetCountersOnThaw, flags);

			loop_ = new Checkbox(
				"Loop steps", options_.Loop,
				Loop, flags);

			pickAnimatable_ = new Checkbox(
				"Pick animatable", PickAnimatableChanged, flags);

			manageAnimatable_ = new Button(
				"Manage animatables", ManageAnimatables, flags);

			overlapTime_ = new FloatSlider(
				"Global overlap time", options_.OverlapTime,
				new FloatRange(0, 1), OverlapTimeChanged, flags);

			logLevel_ = new StringList(
				"Log level", Options.LogLevelToString(options_.LogLevel),
				Options.GetLogLevelNames(), LogLevelChanged, flags);

			logOverlap_ = new Checkbox(
				"Log overlap", LogOverlapChanged, flags);

			usePlaceholder_ = new Checkbox(
				"Save: use placeholder for atoms", null, flags);

			collapsible_.Add(resetValuesOnFreeze_);
			collapsible_.Add(resetCountersOnThaw_);
			collapsible_.Add(loop_);
			collapsible_.Add(pickAnimatable_);
			collapsible_.Add(manageAnimatable_);
			collapsible_.Add(overlapTime_);
			collapsible_.Add(logLevel_);
			collapsible_.Add(logOverlap_);
			collapsible_.Add(new SmallSpacer(flags));

			collapsible_.Add(usePlaceholder_);

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
				"Step: load, add modifiers to current step",
				() => { LoadStep(Utilities.PresetMerge); },
				flags));

			collapsible_.Add(new Button(
				"Step: load, append as new step",
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
				"Modifier: load, append to current step",
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

			int flags = Utilities.FullPreset;
			if (usePlaceholder_.Value)
				flags |= Utilities.PresetUsePlaceholder;

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.CompletePresetExtension;

					Synergy.Instance.Manager.SavePreset(path, flags);
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

			int flags = Utilities.StepPreset;
			if (usePlaceholder_.Value)
				flags |= Utilities.PresetUsePlaceholder;

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.StepPresetExtension;

					Synergy.Instance.Manager.SavePreset(path, flags);
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

			int flags = Utilities.ModifierPreset;
			if (usePlaceholder_.Value)
				flags |= Utilities.PresetUsePlaceholder;

			SuperController.singleton.GetMediaPathDialog(
				(string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					if (!path.Contains("."))
						path += "." + Utilities.ModifierPresetExtension;

					Synergy.Instance.Manager.SavePreset(path, flags);
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

		private void Loop(bool b)
		{
			options_.Loop = b;
		}

		private void PickAnimatableChanged(bool b)
		{
			options_.PickAnimatable = b;
			Synergy.Instance.UI.NeedsReset("pick animatable changed");
		}

		private void ManageAnimatables()
		{
			Synergy.Instance.UI.ToggleManageAnimatables();
		}

		private void OverlapTimeChanged(float f)
		{
			options_.OverlapTime = f;
		}

		private void LogLevelChanged(string s)
		{
			var i = Options.LogLevelFromString(s);
			if (i == -1)
				return;

			options_.LogLevel = i;
		}

		private void LogOverlapChanged(bool b)
		{
			options_.LogOverlap = b;
		}
	}
}
