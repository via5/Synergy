using JetBrains.Annotations;
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

			info_.Set(s);
			duration_.Set(s?.Duration);
			repeat_.Set(s?.Repeat);
			delay_.Set(s?.Delay);
		}

		private void OnDurationTypeChanged(IDuration d)
		{
			step_.Duration = d;
			duration_.Set(d);
		}
	}


	class ItemControls : UI.Panel
	{
		public delegate void AddCallback();
		public delegate void CloneCallback(int flags);
		public delegate void RemoveCallback();
		public delegate void MoveCallback(int d);

		public event AddCallback Added;
		public event CloneCallback Cloned;
		public event RemoveCallback Removed;
		public event MoveCallback Moved;

		public const int NoFlags = 0x00;
		public const int AllowMove = 0x01;

		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;

		public ItemControls(int flags = NoFlags)
		{
			Layout = new UI.HorizontalFlow(20);

			add_ = new ToolButton("+", OnAdd);
			clone_ = new ToolButton(S("+*"), OnClone);
			clone0_ = new ToolButton(S("+*0"), OnCloneZero);
			remove_ = new ToolButton("\x2013", OnRemove);  // en dash

			if (Bits.IsSet(flags, AllowMove))
			{
				up_ = new ToolButton("\x25b2", OnMoveUp);      // up arrow
				down_ = new ToolButton("\x25bc", OnMoveDown);  // down arrow
			}

			Add(add_);
			Add(clone_);
			Add(clone0_);
			Add(remove_);

			if (up_ != null)
				Add(up_);

			if (down_ != null)
				Add(down_);
		}

		private void OnAdd()
		{
			Added?.Invoke();
		}

		private void OnClone()
		{
			Cloned?.Invoke(0);
		}

		private void OnCloneZero()
		{
			Cloned?.Invoke(Utilities.CloneZero);
		}

		private void OnRemove()
		{
			Removed?.Invoke();
		}

		private void OnMoveUp()
		{
			Moved?.Invoke(+1);
		}

		private void OnMoveDown()
		{
			Moved?.Invoke(-1);
		}
	}


	class StepControls : UI.Panel
	{
		public delegate void StepCallback(Step s);
		public event StepCallback SelectionChanged;

		private readonly UI.TypedComboBox<Step> steps_;
		private readonly ItemControls controls_;
		private bool ignore_ = false;

		public StepControls()
		{
			steps_ = new UI.TypedComboBox<Step>(OnSelectionChanged);
			controls_ = new ItemControls(ItemControls.AllowMove);

			Layout = new UI.HorizontalFlow(20);
			Add(new UI.Label(S("Step:")));
			Add(steps_);
			Add(controls_);

			controls_.Added += AddStep;
			controls_.Cloned += CloneStep;
			controls_.Removed += RemoveStep;
			controls_.Moved += MoveStep;

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
			using (var sf = new ScopedFlag(b => ignore_ = b))
			{
				var s = Synergy.Instance.Manager.AddStep();
				steps_.AddItem(s, true);
			}
		}

		public void CloneStep(int flags)
		{
			using (var sf = new ScopedFlag(b => ignore_ = b))
			{
				var s = steps_.Selected;
				if (s != null)
				{
					var ns = Synergy.Instance.Manager.AddStep(s.Clone(flags));
					steps_.AddItem(s, true);
				}
			}
		}

		public void RemoveStep()
		{
			var d = new UI.Dialog(GetRoot(), "");
			d.RunDialog();

			using (var sf = new ScopedFlag(b => ignore_ = b))
			{
				var s = steps_.Selected;
				if (s != null)
				{
					Synergy.Instance.Manager.DeleteStep(s);
					steps_.RemoveItem(s);
				}
			}
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

		private void UpdateSteps()
		{
			steps_.SetItems(
				new List<Step>(Synergy.Instance.Manager.Steps),
				steps_.Selected);
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

			name_.MinimumSize = new UI.Size(300, DontCare);
			name_.Changed += OnNameChanged;

			enabled_.Changed += OnEnabled;
			halfMove_.Changed += OnHalfMove;
		}

		public void Set(Step s)
		{
			step_ = s;

			name_.Text = s.Name;
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
