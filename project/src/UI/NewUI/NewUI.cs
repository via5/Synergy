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
			var mm = new MorphModifier();
			mm.Atom = Synergy.Instance.GetAtomById("Person");
			mm.AddMorph(Utilities.GetAtomMorph(mm.Atom, "Smile Full Face"));
			mm.AddMorph(Utilities.GetAtomMorph(mm.Atom, "Eyes Closed"));
			//var rm = new RigidbodyModifier();
			//rm.Atom = Synergy.Instance.GetAtomById("Person");
			//rm.Receiver = Utilities.FindRigidbody(rm.Atom, "head");
			//rm.Movement.Maximum.Initial = 100;
			var m = new ModifierContainer(mm);
			m.ModifierSync = new UnsyncedModifier(
				new RandomDuration(1), new Delay(new RandomDuration(1), false, false));
			s.AddModifier(m);

			modifiersTab_.SelectTab(2);
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
				duration_.Set(delay_.Duration);
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
