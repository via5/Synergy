using Synergy.UI;
using System;

namespace Synergy.NewUI
{
	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private UI.Tabs tabs_ = new UI.Tabs();
		private StepControls steps_ = new StepControls();
		private StepTab stepTab_ = new StepTab();
		private ModifiersTab modifiersTab_ = new ModifiersTab();
		private PresetsTab presetsTab_ = new PresetsTab();

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			tabs_.AddTab(S("Step"), stepTab_);
			tabs_.AddTab(S("Modifiers"), modifiersTab_);
			tabs_.AddTab(S("Presets"), presetsTab_);

			root_.ContentPanel.Layout = new UI.BorderLayout(20);
			root_.ContentPanel.Add(steps_, UI.BorderLayout.Top);
			root_.ContentPanel.Add(tabs_, UI.BorderLayout.Center);

			if (Synergy.Instance.Manager.Steps.Count > 0)
				SelectStep(Synergy.Instance.Manager.Steps[0]);
			else
				SelectStep(null);

			//tabs_.Select(1);

			steps_.SelectionChanged += OnStepSelected;
			root_.DoLayoutIfNeeded();

			//modifiersTab_.SelectTab(6);
		}

		public void SelectStep(Step s)
		{
			if (s == null)
			{
				tabs_.SetTabVisible(0, false);
				tabs_.SetTabVisible(1, false);
			}
			else
			{
				tabs_.SetTabVisible(0, true);
				tabs_.SetTabVisible(1, true);

				stepTab_.SetStep(s);
				modifiersTab_.SetStep(s);
			}
		}

		public void Tick()
		{
			root_.DoLayoutIfNeeded();
		}

		private void OnStepSelected(Step s)
		{
			SelectStep(s);
		}
	}


	class DelayWidgets : UI.Panel
	{
		private readonly UI.CheckBox halfWay_, endForwards_, endBackwards_;
		private readonly RandomDurationWidgets duration_ = new RandomDurationWidgets();

		private Delay delay_ = null;
		private bool ignore_ = false;

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

			using (new ScopedFlag((b) => ignore_ = b))
			{
				duration_.Set(delay_.SingleDuration);
				halfWay_.Checked = delay_.Halfway;
				endForwards_.Checked = delay_.EndForwards;
				endBackwards_.Checked = delay_.EndBackwards;
			}
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
}
