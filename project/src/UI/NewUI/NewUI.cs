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

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			//var cp = new UI.ColorPicker();
			//cp.Borders = new Insets(2);
			//
			//root_.ContentPanel.Layout = new UI.VerticalFlow(10);
			//root_.ContentPanel.Add(cp);
			//root_.ContentPanel.Add(new TextBox("test", "placeholdeR"));


			tabs_.AddTab(S("Step"), stepTab_);
			tabs_.AddTab(S("Modifiers"), modifiersTab_);

			root_.ContentPanel.Layout = new UI.BorderLayout(30);
			root_.ContentPanel.Add(steps_, UI.BorderLayout.Top);
			root_.ContentPanel.Add(tabs_, UI.BorderLayout.Center);

			if (Synergy.Instance.Manager.Steps.Count > 0)
				SelectStep(Synergy.Instance.Manager.Steps[0]);
			else
				SelectStep(null);

			tabs_.Select(1);

			steps_.SelectionChanged += OnStepSelected;
			root_.DoLayoutIfNeeded();

			modifiersTab_.SelectTab(5);
		}

		public void SelectStep(Step s)
		{
			if (s == null)
			{
				tabs_.Visible = false;
			}
			else
			{
				tabs_.Visible = true;
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
}
