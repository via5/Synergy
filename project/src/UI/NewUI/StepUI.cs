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
		private IgnoreFlag ignore_ = new IgnoreFlag();

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

			ignore_.Do(() =>
			{
				info_.Set(s);
				duration_.Set(s?.Duration);
				repeat_.Set(s?.Repeat);
				delay_.Set(s?.Delay);
			});
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
		class StepItem
		{
			public Step step;

			public StepItem(Step s)
			{
				step = s;
			}

			public override string ToString()
			{
				if (step == null)
					return "?";

				string s = step.Name;

				var i = Synergy.Instance.Manager.IndexOfStep(step);
				if (i >= 0)
					s = "#" + (i + 1).ToString() + " " + s;

				return s;
			}
		}

		public delegate void StepCallback(Step s);
		public event StepCallback SelectionChanged;

		private readonly UI.ComboBox<StepItem> steps_;
		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;
		private readonly UI.Button rename_;
		private readonly FactoryComboBox<StepProgressionFactory, IStepProgression> progression_;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public StepControls()
		{
			steps_ = new UI.ComboBox<StepItem>(OnSelectionChanged);
			add_ = new UI.ToolButton(UI.Utilities.AddSymbol, AddStep);
			clone_ = new UI.ToolButton(UI.Utilities.CloneSymbol, () => CloneStep(0));
			clone0_ = new UI.ToolButton(UI.Utilities.CloneZeroSymbol, () => CloneStep(Utilities.CloneZero));
			remove_ = new UI.ToolButton(UI.Utilities.RemoveSymbol, RemoveStep);
			up_ = new UI.ToolButton(UI.Utilities.UpArrow, () => MoveStep(-1));   // up arrow
			down_ = new UI.ToolButton(UI.Utilities.DownArrow, () => MoveStep(+1)); // down arrow
			rename_ = new UI.Button(S("Rename"), OnRename);
			progression_ = new FactoryComboBox<StepProgressionFactory, IStepProgression>(
				OnProgressionChanged);

			add_.Tooltip.Text = S("Add a new step");
			clone_.Tooltip.Text = S("Clone this step");
			clone0_.Tooltip.Text = S("Clone this step and zero all values");
			remove_.Tooltip.Text = S("Remove this step");
			up_.Tooltip.Text = S("Move this step earlier in the execution order");
			down_.Tooltip.Text = S("Move this step later in the execution order");

			steps_.NavButtons = true;

			var p = new Panel(new UI.HorizontalFlow(20));
			p.Add(add_);
			p.Add(clone_);
			p.Add(clone0_);
			p.Add(remove_);
			p.Add(up_);
			p.Add(down_);
			p.Add(rename_);
			p.Add(progression_);

			Layout = new UI.HorizontalFlow(20);
			Add(new UI.Label(S("Steps")));
			Add(steps_);
			Add(p);

			Synergy.Instance.Manager.StepsChanged += OnStepsChanged;
			Synergy.Instance.Manager.StepNameChanged += OnStepNameChanged;

			progression_.Select(Synergy.Instance.Manager.StepProgression);
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
				return steps_.Selected?.step;
			}
		}

		public void AddStep()
		{
			ignore_.Do(() =>
			{
				var s = Synergy.Instance.Manager.AddStep();
				steps_.AddItem(new StepItem(s), true);
			});
		}

		public void CloneStep(int flags)
		{
			ignore_.Do(() =>
			{
				var s = steps_.Selected?.step;

				if (s != null)
				{
					var ns = Synergy.Instance.Manager.AddStep(s.Clone(flags));
					steps_.AddItem(new StepItem(ns), true);
				}
			});
		}

		public void RemoveStep()
		{
			var s = steps_.Selected?.step;
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

				ignore_.Do(() =>
				{
					Synergy.Instance.Manager.DeleteStep(s);
					steps_.RemoveItem(steps_.Selected);
				});
			});
		}

		public void MoveStep(int d)
		{
		}

		private void OnSelectionChanged(StepItem s)
		{
			// invalid IR?
			// SelectionChanged?.Invoke(s?.step);

			if (s?.step == null)
				SelectionChanged?.Invoke(null);
			else
				SelectionChanged?.Invoke(s.step);
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
			var s = steps_.Selected?.step;
			if (s == null)
				return;

			InputDialog.GetInput(
				GetRoot(), S("Rename step"), S("Step name"), s.Name,
				(v) => { s.UserDefinedName = v; });
		}

		private void OnProgressionChanged(IStepProgression p)
		{
			if (ignore_)
				return;

			Synergy.Instance.Manager.StepProgression = p;
		}

		private void UpdateSteps()
		{
			var items = new List<StepItem>();

			var sel = steps_.Selected?.step;
			StepItem selItem = null;

			foreach (var s in Synergy.Instance.Manager.Steps)
			{
				var si = new StepItem(s);

				items.Add(si);

				if (s == sel)
					selItem = si;
			}

			steps_.SetItems(items, selItem);
		}
	}


	class StepInfo : UI.Panel
	{
		private readonly UI.CheckBox enabled_, paused_, halfMove_;
		private readonly UI.Button disableOthers_, enableAll_;
		private Step step_ = null;

		public StepInfo()
		{
			Layout = new UI.HorizontalFlow(10);

			enabled_ = new UI.CheckBox(S("Enabled"));
			paused_ = new UI.CheckBox(S("Paused"));
			halfMove_ = new UI.CheckBox(S("Half move"));
			disableOthers_ = new UI.Button(S("Disable others"), OnDisableOthers);
			enableAll_ = new UI.Button(S("Enable all"), OnEnableAll);

			enabled_.Tooltip.Text = S("Whether this step is executed");
			paused_.Tooltip.Text = S(
				"Pause the modifiers in their current state and disables " +
				"the step");
			halfMove_.Tooltip.Text = S(
				"Whether this step should stop halfway before executing " +
				"the next step");

			Add(enabled_);
			Add(paused_);
			Add(halfMove_);
			Add(disableOthers_);
			Add(enableAll_);

			enabled_.Changed += OnEnabled;
			paused_.Changed += OnPaused;
			halfMove_.Changed += OnHalfMove;
		}

		public void Set(Step s)
		{
			step_ = s;

			enabled_.Checked = s.Enabled;
			paused_.Checked = s.Paused;
			halfMove_.Checked = s.HalfMove;
		}

		private void OnEnabled(bool b)
		{
			if (step_ != null)
				step_.Enabled = b;
		}

		private void OnPaused(bool b)
		{
			if (step_ != null)
				step_.Paused = b;
		}

		private void OnHalfMove(bool b)
		{
			if (step_ != null)
				step_.HalfMove = b;
		}

		private void OnDisableOthers()
		{
			if (step_ != null)
			{
				Synergy.Instance.Manager.DisableAllExcept(step_);
				enabled_.Checked = step_.Enabled;
			}
		}

		private void OnEnableAll()
		{
			Synergy.Instance.Manager.EnableAllSteps();
			enabled_.Checked = true;
		}
	}
}
