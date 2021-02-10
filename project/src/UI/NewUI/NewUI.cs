using UI = SynergyUI;

namespace Synergy.NewUI
{
	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private UI.Tabs tabs_ = new UI.Tabs();
		private StepControls steps_ = new StepControls();
		private ModifierControls modifiers_ = new ModifierControls();
		private WelcomeTab welcomeTab_ = new WelcomeTab();
		private StepTab stepTab_ = new StepTab();
		private ModifierTab modifierTab_ = new ModifierTab();
		private PresetsTab presetsTab_ = new PresetsTab();
		private OptionsTab optionsTab_ = new OptionsTab();
		private bool showWelcome_ = false;

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			tabs_.AddTab(S("Welcome"), welcomeTab_);
			tabs_.AddTab(S("Step"), stepTab_);
			tabs_.AddTab(S("Modifier"), modifierTab_);
			tabs_.AddTab(S("Presets"), presetsTab_);
			tabs_.AddTab(S("Options"), optionsTab_);

			var top = new UI.Panel(new UI.VerticalFlow(10));
			top.Add(steps_);
			top.Add(modifiers_);

			root_.ContentPanel.Layout = new UI.BorderLayout(20);
			root_.ContentPanel.Add(top, UI.BorderLayout.Top);
			root_.ContentPanel.Add(tabs_, UI.BorderLayout.Center);

			steps_.SelectionChanged += OnStepSelected;
			modifiers_.SelectionChanged += OnModifierSelected;

			if (Synergy.Instance.Manager.Steps.Count > 0)
			{
				SelectStep(Synergy.Instance.Manager.Steps[0]);
			}
			else
			{
				showWelcome_ = true;
				SelectStep(null);
			}

			root_.DoLayoutIfNeeded();
		}

		public void Destroy()
		{
			root_.Destroy();
		}

		public Step SelectedStep
		{
			get { return steps_.Selected; }
		}

		public ModifierContainer SelectedModifier
		{
			get { return modifiers_.Selected; }
		}

		public void SelectStep(Step s)
		{
			if (s == null)
			{
				tabs_.SetTabVisible(stepTab_, false);
				tabs_.SetTabVisible(modifierTab_, false);
			}
			else
			{
				tabs_.SetTabVisible(stepTab_, true);
				tabs_.SetTabVisible(modifierTab_, true);

				stepTab_.SetStep(s);
				modifierTab_.SetStep(s);
			}

			UpdateWelcomeTab();
			modifiers_.Set(s);
		}

		public void Tick()
		{
			root_.DoLayoutIfNeeded();
		}

		private void OnStepSelected(Step s)
		{
			SelectStep(s);
		}

		private void OnModifierSelected(ModifierContainer m)
		{
			modifierTab_.SelectModifier(m);
			UpdateWelcomeTab();
		}

		private void UpdateWelcomeTab()
		{
			// hide on load if there's at least one step, see ctor above
			//
			// if not, keep it visible until there's one step with one modifier,
			// then select the modifier tab automatically
			//
			// after that, keep it hidden and don't interfere

			if (showWelcome_)
			{
				var sel = steps_.Selected;
				var stepCount = Synergy.Instance.Manager.Steps.Count;

				showWelcome_ =
					sel == null ||
					stepCount == 0 ||
					(stepCount == 1 && sel.Modifiers.Count == 0);

				if (!showWelcome_)
					tabs_.Select(modifierTab_);
			}

			tabs_.SetTabVisible(welcomeTab_, showWelcome_);
		}
	}


	class DelayWidgets : UI.Panel
	{
		private readonly UI.CheckBox halfWay_, endForwards_, endBackwards_;
		private readonly RandomDurationWidgets duration_ = new RandomDurationWidgets();

		private Delay delay_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public DelayWidgets()
		{
			Layout = new UI.VerticalFlow(30);

			halfWay_ = new UI.CheckBox(S("Halfway"));
			endForwards_ = new UI.CheckBox(S("End forwards"));
			endBackwards_ = new UI.CheckBox(S("End backwards"));

			var p = new UI.Panel(new UI.HorizontalFlow());
			p.Add(halfWay_);
			p.Add(endForwards_);
			p.Add(endBackwards_);

			Add(p);
			Add(duration_);

			halfWay_.Changed += OnHalfwayChanged;
			endForwards_.Changed += OnEndForwardsChanged;
			endBackwards_.Changed += OnEndBackwardsChanged;
		}

		public void Set(Delay d)
		{
			delay_ = d;

			ignore_.Do(() =>
			{
				duration_.Set(delay_.SingleDuration);
				halfWay_.Checked = delay_.Halfway;
				endForwards_.Checked = delay_.EndForwards;
				endBackwards_.Checked = delay_.EndBackwards;
			});
		}

		private void OnHalfwayChanged(bool b)
		{
			if (ignore_)
				return;

			delay_.Halfway = b;
		}

		private void OnEndForwardsChanged(bool b)
		{
			if (ignore_)
				return;

			delay_.EndForwards = b;
		}

		private void OnEndBackwardsChanged(bool b)
		{
			if (ignore_)
				return;

			delay_.EndBackwards = b;
		}
	}


	class FactoryComboBoxItem<ObjectType>
		where ObjectType : IFactoryObject
	{
		private readonly IFactoryObjectCreator creator_;

		public FactoryComboBoxItem(IFactoryObjectCreator creator)
		{
			creator_ = creator;
		}

		public ObjectType CreateFactoryObject()
		{
			return (ObjectType)creator_.Create();
		}

		public string FactoryTypeName
		{
			get { return creator_.FactoryTypeName; }
		}

		public override string ToString()
		{
			return creator_.DisplayName;
		}
	}


	class WelcomeTab : UI.Panel
	{
		public WelcomeTab()
		{
			Layout = new UI.VerticalFlow();

			var add = UI.Utilities.AddSymbol;

			Add(new UI.Label(S(
				"Welcome to Synergy. Press the " + add + " button next to " +
				"\"Steps\" to add the first step, then press the " + add + " " +
				"button next to \"Modifiers\" to add a modifier in that " +
				"step.")));
		}
	}


	class PresetsTab : UI.Panel
	{
		private readonly UI.CheckBox usePlaceholder_ =
			new UI.CheckBox(S("Save: use placeholder for atoms"));

		public PresetsTab()
		{
			Layout = new UI.VerticalFlow(10, false);

			Add(usePlaceholder_);
			Add(new UI.Spacer(20));


			Add(new UI.Button(
				S("Full: save"),
				SaveFull));

			Add(new UI.Button(
				S("Full: load, replace everything"),
				() => LoadFull(Utilities.PresetReplace)));

			Add(new UI.Button(
				S("Full: load, append steps"),
				() => LoadFull(Utilities.PresetAppend)));

			Add(new UI.Spacer(20));


			Add(new UI.Button(
				S("Step: save current"),
				SaveStep));

			Add(new UI.Button(
				S("Step: load, replace current"),
				() => LoadStep(Utilities.PresetReplace)));

			Add(new UI.Button(
				S("Step: load, add modifiers to current step"),
				() => LoadStep(Utilities.PresetMerge)));

			Add(new UI.Button(
				S("Step: load, append as new step"),
				() => LoadStep(Utilities.PresetAppend)));

			Add(new UI.Spacer(20));


			Add(new UI.Button(
				S("Modifier: save current"),
				SaveModifier));

			Add(new UI.Button(
				S("Modifier: load, replace current"),
				() => LoadModifier(Utilities.PresetReplace)));

			Add(new UI.Button(
				S("Modifier: load, append to current step"),
				() => LoadModifier(Utilities.PresetAppend)));

			Add(new UI.Spacer(20));
		}


		public void SaveFull()
		{
			OptionsUI.SaveFull(usePlaceholder_.Checked);
		}

		public void LoadFull(int flags)
		{
			OptionsUI.LoadFull(flags);
		}

		public void SaveStep()
		{
			OptionsUI.SaveStep(usePlaceholder_.Checked);
		}

		public void LoadStep(int flags)
		{
			OptionsUI.LoadStep(flags);
		}

		public void SaveModifier()
		{
			OptionsUI.SaveModifier(usePlaceholder_.Checked);
		}

		public void LoadModifier(int flags)
		{
			OptionsUI.LoadModifier(flags);
		}
	}


	class OptionsTab : UI.Panel
	{
		private readonly Options options_ = Synergy.Instance.Options;

		public OptionsTab()
		{
			var overlapPanel = new UI.Panel(new UI.HorizontalFlow(10));
			overlapPanel.Add(new UI.Label(S("Global overlap time")));
			overlapPanel.Add(new UI.TextSlider(
				options_.OverlapTime, 0, 2,
				(f) => options_.OverlapTime = f));

			var logPanel = new UI.Panel(new UI.HorizontalFlow(10));
			logPanel.Add(new UI.Label(S("Log level")));
			logPanel.Add(new UI.ComboBox<string>(
				Options.GetLogLevelNames(),
				Options.LogLevelToString(options_.LogLevel),
				(s) => options_.LogLevel = Options.LogLevelFromString(s)));

			Layout = new UI.VerticalFlow(10, false);

			Add(new UI.CheckBox(
				S("Reset positions on freeze"),
				(b) => options_.ResetValuesOnFreeze = b,
				options_.ResetValuesOnFreeze));

			Add(new UI.CheckBox(
				S("Reset counters on thaw"),
				(b) => options_.ResetCountersOnThaw = b,
				options_.ResetCountersOnThaw));

			Add(new UI.Spacer(20));

			Add(new UI.CheckBox(
				"Pick animatable",
				PickAnimatableChanged));

			Add(new UI.Button(
				S("Manage animatables"),
				ManageAnimatables));

			Add(new UI.Spacer(20));

			Add(overlapPanel);

			Add(new UI.Spacer(20));

			Add(logPanel);
			Add(new UI.CheckBox(
				S("Log overlap"),
				(b) => options_.LogOverlap = b,
				options_.LogOverlap));

			Add(new UI.Spacer(20));

			Add(new UI.Button(
				S("Switch to old UI"),
				() => options_.SetNewUI(false)));
		}

		public void PickAnimatableChanged(bool b)
		{
		}

		public void ManageAnimatables()
		{
		}
	}
}
