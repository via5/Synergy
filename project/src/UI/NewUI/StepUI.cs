using System.Collections.Generic;

namespace Synergy.NewUI
{
	class StepTab : UI.Panel
	{
		private readonly StepInfo info_ = new StepInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly RepeatWidgets repeat_ = new RepeatWidgets();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private Step step_ = null;

		public StepTab()
		{
			Layout = new UI.BorderLayout(20);
			Layout.Spacing = 30;

			tabs_.AddTab(S("Duration"), duration_);
			tabs_.AddTab(S("Repeat"), repeat_);
			tabs_.AddTab(S("Delay"), delay_);

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);

			duration_.Changed += OnDurationTypeChanged;
		}

		public void SetStep(Step s)
		{
			step_ = s;
			info_.SetStep(s);
			duration_.Set(s.Duration);
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			step_.Duration = d;
			duration_.Set(d);
		}
	}


	class StepInfo : UI.Panel
	{
		private readonly UI.TextBox name_;
		private readonly UI.CheckBox enabled_, halfMove_;
		private Step step_ = null;

		public StepInfo()
		{
			Layout = new UI.HorizontalFlow(10);

			name_ = new UI.TextBox();
			enabled_ = new UI.CheckBox(S("Step enabled"));
			halfMove_ = new UI.CheckBox(S("Half move"));

			Add(new UI.Label(S("Name")));
			Add(name_);
			Add(enabled_);
			Add(halfMove_);

			enabled_.Changed += OnEnabled;
			halfMove_.Changed += OnHalfMove;
		}

		public void SetStep(Step s)
		{
			step_ = s;
			name_.Text = s.Name;
			enabled_.Checked = s.Enabled;
			halfMove_.Checked = s.HalfMove;
		}

		private void OnEnabled(bool b)
		{
			if (step_ != null)
				step_.Enabled = b;
		}

		private void OnHalfMove(bool b)
		{
			if (step_ != null)
				step_.HalfMove = b;
		}
	}


	class StepControls : UI.Panel
	{
		public delegate void StepCallback(Step s);
		public event StepCallback SelectionChanged;

		private readonly UI.TypedComboBox<Step> steps_;
		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;

		public StepControls()
		{
			Layout = new UI.HorizontalFlow(20);

			steps_ = new UI.TypedComboBox<Step>(OnSelectionChanged);
			add_ = new ToolButton("+", AddStep);
			clone_ = new ToolButton(S("Clone"), CloneStep);
			clone0_ = new ToolButton(S("Clone 0"), CloneStepZero);
			remove_ = new ToolButton("\x2013", RemoveStep);  // en dash
			up_ = new ToolButton("\x25b2", MoveStepUp);      // up arrow
			down_ = new ToolButton("\x25bc", MoveStepDown);  // down arrow

			Add(new UI.Label(S("Step:")));
			Add(steps_);
			Add(add_);
			Add(clone_);
			Add(clone0_);
			Add(remove_);
			Add(up_);
			Add(down_);

			Synergy.Instance.Manager.StepsChanged += UpdateSteps;
			UpdateSteps();
		}

		public Step Selected
		{
			get
			{
				return steps_.Selected;
			}
		}

		public void AddStep()
		{
			var s = Synergy.Instance.Manager.AddStep();
			steps_.AddItem(s, true);
		}

		public void CloneStep()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				var ns = Synergy.Instance.Manager.AddStep(s.Clone());
				steps_.AddItem(s, true);
			}
		}

		public void CloneStepZero()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				var ns = Synergy.Instance.Manager.AddStep(
					s.Clone(global::Synergy.Utilities.CloneZero));

				steps_.AddItem(s, true);
			}
		}

		public void RemoveStep()
		{
			var s = steps_.Selected;
			if (s != null)
			{
				Synergy.Instance.Manager.DeleteStep(s);
				steps_.RemoveItem(s);
			}
		}

		public void MoveStepUp()
		{
		}

		public void MoveStepDown()
		{
		}

		private void OnSelectionChanged(Step s)
		{
			SelectionChanged?.Invoke(s);
		}

		private void UpdateSteps()
		{
			steps_.SetItems(
				new List<Step>(Synergy.Instance.Manager.Steps),
				steps_.Selected);
		}
	}


	class RepeatWidgets : UI.Panel
	{
		private UI.Panel widgets_ = new RandomDurationWidgets();

		public RepeatWidgets()
		{
			Layout = new UI.VerticalFlow();
			Add(widgets_);
		}
	}

}
