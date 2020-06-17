namespace Synergy.NewUI
{
	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private StepControls steps_ = new StepControls();
		private StepTab stepTab_ = new StepTab();
		private ModifiersTab modifiersTab_ = new ModifiersTab();

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			var s = Synergy.Instance.Manager.AddStep();
			s.Duration = new RampDuration();
			Synergy.Instance.Manager.AddStep();

			var tabs = new UI.Tabs();
			tabs.AddTab(S("Step"), stepTab_);
			tabs.AddTab(S("Modifiers"), modifiersTab_);

			root_.Layout = new UI.BorderLayout(30);
			root_.Add(steps_, UI.BorderLayout.Top);
			root_.Add(tabs, UI.BorderLayout.Center);

			steps_.SelectionChanged += OnStepSelected;

			if (Synergy.Instance.Manager.Steps.Count > 0)
				SelectStep(Synergy.Instance.Manager.Steps[0]);

			root_.DoLayoutIfNeeded();
		}

		public void SelectStep(Step s)
		{
			stepTab_.SetStep(s);
			modifiersTab_.SetStep(s);
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
		private readonly DurationPanel duration_ = new DurationPanel();

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
	}


	class ToolButton : UI.Button
	{
		public ToolButton(string text = "", UI.Button.Callback clicked = null)
			: base(text, clicked)
		{
			MinimumSize = new UI.Size(50, DontCare);
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


	class FactoryComboBox<FactoryType, ObjectType>
		: UI.TypedComboBox<FactoryComboBoxItem<ObjectType>>
			where FactoryType : IGenericFactory, new()
			where ObjectType : class, IFactoryObject
	{
		public delegate void FactoryTypeCallback(ObjectType o);
		public event FactoryTypeCallback FactoryTypeChanged;

		public FactoryComboBox(FactoryTypeCallback factoryTypeChanged = null)
		{
			var f = new FactoryType();

			foreach (var creator in f.GetAllCreators())
				AddItem(new FactoryComboBoxItem<ObjectType>(creator));

			SelectionChanged += OnSelectionChanged;

			if (factoryTypeChanged != null)
				FactoryTypeChanged += factoryTypeChanged;
		}

		private void OnSelectionChanged(FactoryComboBoxItem<ObjectType> item)
		{
			if (item == null)
				FactoryTypeChanged?.Invoke(null);
			else
				FactoryTypeChanged?.Invoke(item.CreateFactoryObject());
		}

		public void Select(ObjectType d)
		{
			Select(IndexOf(d));
		}

		public int IndexOf(ObjectType d)
		{
			if (d == null)
				return -1;

			var items = Items;

			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i].FactoryTypeName == d.GetFactoryTypeName())
					return i;
			}

			return -1;
		}
	}
}
