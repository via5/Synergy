using Synergy.UI;

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
			//s.Duration = new RampDuration();
			//Synergy.Instance.Manager.AddStep();

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

			var s = Synergy.Instance.Manager.AddStep();
			s.AddModifier(new ModifierContainer(new RigidbodyModifier()));
			modifiersTab_.SelectTab(1);
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
		private readonly UI.CheckBox halfWay_, end_;
		private readonly RandomDurationWidgets duration_ = new RandomDurationWidgets();

		public DelayWidgets()
		{
			Layout = new UI.VerticalFlow(30);

			halfWay_ = new UI.CheckBox(S("Halfway"));
			end_ = new UI.CheckBox(S("End"));

			var p = new UI.Panel(new UI.HorizontalFlow());
			p.Add(halfWay_);
			p.Add(end_);

			Add(p);
			Add(duration_);
		}

		public void Set(Delay d)
		{
			duration_.Set(d.Duration);
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
