using JetBrains.Annotations;
using Synergy.UI;
using System;
using System.Collections.Generic;

namespace Synergy.NewUI
{
	class StepTab : UI.Panel
	{
		private readonly StepInfo info_ = new StepInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();
		private readonly DurationPanel duration_ = new DurationPanel();
		private readonly RandomizableTimePanel repeat_ = new RandomizableTimePanel();
		private readonly DelayWidgets delay_ = new DelayWidgets();

		private Step step_ = null;
		private bool ignore_ = false;

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

			using (new ScopedFlag((bool b) => ignore_ = b))
			{
				info_.Set(s);
				duration_.Set(s?.Duration);
				repeat_.Set(s?.Repeat);
				delay_.Set(s?.Delay);
			}
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			if (ignore_)
				return;

			step_.Duration = d;
		}
	}


	class StepControls : UI.Panel
	{
		public delegate void StepCallback(Step s);
		public event StepCallback SelectionChanged;

		private readonly UI.ComboBox<Step> steps_;
		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;
		private readonly UI.Button rename_;
		private bool ignore_ = false;

		public StepControls()
		{
			steps_ = new UI.ComboBox<Step>(OnSelectionChanged);
			add_ = new UI.ToolButton("+", AddStep);
			clone_ = new UI.ToolButton(S("+*"), () => CloneStep(0));
			clone0_ = new UI.ToolButton(S("+*0"), () => CloneStep(Utilities.CloneZero));
			remove_ = new UI.ToolButton("\x2013", RemoveStep);       // en dash
			up_ = new UI.ToolButton("\x25b2", () => MoveStep(-1));   // up arrow
			down_ = new UI.ToolButton("\x25bc", () => MoveStep(+1)); // down arrow
			rename_ = new UI.Button("Rename", OnRename);

			add_.Tooltip.Text = S("Add a new step");
			clone_.Tooltip.Text = S("Clone this step");
			clone0_.Tooltip.Text = S("Clone this step and zero all values");
			remove_.Tooltip.Text = S("Remove this step");
			up_.Tooltip.Text = S("Move this step earlier in the execution order");
			down_.Tooltip.Text = S("Move this step later in the execution order");

			var p = new Panel(new UI.HorizontalFlow(20));
			p.Add(add_);
			p.Add(clone_);
			p.Add(clone0_);
			p.Add(remove_);
			p.Add(up_);
			p.Add(down_);
			p.Add(rename_);

			Layout = new UI.HorizontalFlow(20);
			Add(new UI.Label(S("Steps:")));
			Add(steps_);
			Add(p);

			Synergy.Instance.Manager.StepsChanged += OnStepsChanged;
			Synergy.Instance.Manager.StepNameChanged += OnStepNameChanged;

			UpdateSteps();
		}

		public override void Dispose()
		{
			base.Dispose();
			Synergy.Instance.Manager.StepsChanged -= OnStepsChanged;
			Synergy.Instance.Manager.StepNameChanged -= OnStepNameChanged;
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
			using (new ScopedFlag(b => ignore_ = b))
			{
				var s = Synergy.Instance.Manager.AddStep();
				steps_.AddItem(s);
				steps_.Select(s);
			}
		}

		public void CloneStep(int flags)
		{
			using (new ScopedFlag(b => ignore_ = b))
			{
				var s = steps_.Selected;
				if (s != null)
				{
					var ns = Synergy.Instance.Manager.AddStep(s.Clone(flags));
					steps_.AddItem(s);
					steps_.Select(s);
				}
			}
		}

		public void RemoveStep()
		{
			var s = steps_.Selected;
			if (s == null)
				return;

			var d = new UI.MessageDialog(
				GetRoot(), UI.MessageDialog.Yes | UI.MessageDialog.No,
				S("Delete step"),
				S("Are you sure you want to delete step '{0}'?", s.Name));

			d.RunDialog(() =>
			{
				if (d.Button != UI.MessageDialog.Yes)
					return;

				using (new ScopedFlag(b => ignore_ = b))
				{
					Synergy.Instance.Manager.DeleteStep(s);
					steps_.RemoveItem(s);
				}
			});
		}

		public void MoveStep(int d)
		{
		}

		private void OnSelectionChanged(Step s)
		{
			SelectionChanged?.Invoke(s);
		}

		private void OnStepsChanged()
		{
			if (ignore_)
				return;

			UpdateSteps();
		}

		private void OnStepNameChanged(Step s)
		{
			steps_.UpdateItemsText();
		}

		private void OnRename()
		{
			var s = steps_.Selected;
			if (s == null)
				return;

			InputDialog.GetInput(
				GetRoot(), S("Rename step"), S("Step name"), s.Name, (v) =>
				{
					s.UserDefinedName = v;
				});
		}

		private void UpdateSteps()
		{
			steps_.SetItems(
				new List<Step>(Synergy.Instance.Manager.Steps),
				steps_.Selected);
		}
	}


	class StepInfo : UI.Panel
	{
		private readonly UI.CheckBox enabled_, halfMove_;
		private Step step_ = null;

		public StepInfo()
		{
			Layout = new UI.HorizontalFlow(10);

			enabled_ = new UI.CheckBox(S("Step enabled"));
			halfMove_ = new UI.CheckBox(S("Half move"));

			enabled_.Tooltip.Text = S("Whether this step is executed");
			halfMove_.Tooltip.Text = S(
				"Whether this step should stop halfway before executing " +
				"the next step");

			Add(enabled_);
			Add(halfMove_);

			enabled_.Changed += OnEnabled;
			halfMove_.Changed += OnHalfMove;
		}

		public void Set(Step s)
		{
			step_ = s;

			enabled_.Checked = s.Enabled;
			halfMove_.Checked = s.HalfMove;
		}

		private void OnNameChanged(string s)
		{
			if (step_ != null)
				step_.UserDefinedName = s;
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
}
