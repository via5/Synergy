using System.Collections.Generic;
using System;

namespace Synergy
{
	class RandomizableFloatWidgets : CompoundWidget
	{
		private RandomizableFloat value_ = null;
		private FloatSlider initial_ = null;
		private FloatSlider range_ = null;
		private readonly FloatSlider interval_;
		private Button randomHalf_;

		public RandomizableFloatWidgets(string name, int flags = 0)
			: base(flags)
		{
			interval_ = new FloatSlider(
				name + " change interval", 0, new FloatRange(0, 10),
				IntervalChanged, flags);

			randomHalf_ = new Button("Randomize half", RandomizeHalf, flags);

			initial_ = new FloatSlider(
				name, 0, new FloatRange(0, 0),
				InitialChanged, flags);

			range_ = new FloatSlider(
				name + " random range", 0, new FloatRange(0, 0),
				RangeChanged, flags);
		}

		public List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				initial_, range_, interval_, randomHalf_
			};
		}

		public void SetValue(RandomizableFloat v, FloatRange preferredRange)
		{
			value_ = v;

			if (value_ != null)
			{
				initial_.Parameter = value_.InitialParameter;
				initial_.Default = value_.Initial;
				initial_.Range = preferredRange;

				range_.Parameter = value_.RangeParameter;
				range_.Default = value_.Range;
				range_.Range = preferredRange;

				interval_.Parameter = value_.IntervalParameter;
				interval_.Default = value_.Interval;
			}
		}

		protected override void DoAddToUI()
		{
			// no-op
		}

		protected override void DoRemoveFromUI()
		{
			// no-op
		}

		private void InitialChanged(float x)
		{
			if (value_ != null)
				value_.Initial = x;
		}

		private void RangeChanged(float x)
		{
			if (value_ != null)
				value_.Range = x;
		}

		private void IntervalChanged(float x)
		{
			if (value_ != null)
				value_.Interval = x;
		}

		private void RandomizeHalf()
		{
			var half = initial_.Value / 2;

			value_.Initial = half;
			value_.Range = Math.Abs(half);

			initial_.Value = half;
			range_.Value = Math.Abs(half);
		}
	}


	class RandomizableTimeWidgets : CompoundWidget
	{
		private RandomizableTime value_ = null;
		private FloatSlider initial_ = null;
		private FloatSlider range_ = null;
		private readonly FloatSlider interval_;
		private Button randomHalf_;
		private readonly StringList cutoff_;

		public RandomizableTimeWidgets(string name, int flags = 0)
			: base(flags)
		{
			interval_ = new FloatSlider(
				name + " change interval", 0, new FloatRange(0, 10),
				IntervalChanged, flags);

			randomHalf_ = new Button("Randomize half", RandomizeHalf, flags);

			initial_ = new FloatSlider(
				name, 0, new FloatRange(0, 0),
				InitialChanged, flags);

			range_ = new FloatSlider(
				name + " random range", 0, new FloatRange(0, 0),
				RangeChanged, flags);

			cutoff_ = new StringList(
				name + " cut-off", "",
				RandomizableTime.GetCutoffNames(), CutoffChanged, flags);
		}

		public List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				initial_, range_, interval_, randomHalf_, cutoff_
			};
		}

		public void SetValue(RandomizableTime v, FloatRange preferredRange)
		{
			value_ = v;

			if (value_ != null)
			{
				initial_.Parameter = value_.InitialParameter;
				initial_.Default = value_.Initial;
				initial_.Range = preferredRange;

				range_.Parameter = value_.RangeParameter;
				range_.Default = value_.Range;
				range_.Range = preferredRange;

				interval_.Parameter = value_.IntervalParameter;
				interval_.Default = value_.Interval;
			}

			if (v != null)
				cutoff_.Value = RandomizableTime.CutoffToString(v.Cutoff);
		}

		private void CutoffChanged(string s)
		{
			var i = RandomizableTime.CutoffFromString(s);
			if (i == -1)
				return;

			if (value_ != null)
				((RandomizableTime)value_).Cutoff = i;
		}

		protected override void DoAddToUI()
		{
			// no-op
		}

		protected override void DoRemoveFromUI()
		{
			// no-op
		}

		private void InitialChanged(float x)
		{
			if (value_ != null)
				value_.Initial = x;
		}

		private void RangeChanged(float x)
		{
			if (value_ != null)
				value_.Range = x;
		}

		private void IntervalChanged(float x)
		{
			if (value_ != null)
				value_.Interval = x;
		}

		private void RandomizeHalf()
		{
			var half = initial_.Value / 2;

			value_.Initial = half;
			value_.Range = Math.Abs(half);

			initial_.Value = half;
			range_.Value = Math.Abs(half);
		}
	}


	class RandomizableIntWidgets : CompoundWidget
	{
		private RandomizableInt value_ = null;

		private IntSlider initial_ = null;
		private IntSlider range_ = null;
		private readonly FloatSlider interval_;
		private Button randomHalf_;

		public RandomizableIntWidgets(string name, int flags = 0)
			: base(flags)
		{
			interval_ = new FloatSlider(
				name + " change interval", 0, new FloatRange(0, 10),
				IntervalChanged, flags);

			randomHalf_ = new Button("Randomize half", RandomizeHalf, flags);

			initial_ = new IntSlider(
				name, 0, new IntRange(0, 0),
				InitialChanged, flags);

			range_ = new IntSlider(
				name + " range", 0, new IntRange(0, 0),
				RangeChanged, flags);
		}

		public void SetValue(RandomizableInt v, IntRange preferredRange)
		{
			value_ = v;

			if (value_ != null)
			{
				initial_.Parameter = value_.InitialParameter;
				initial_.Default = value_.Initial;
				initial_.Range = preferredRange;

				range_.Parameter = value_.RangeParameter;
				range_.Default = value_.Range;
				range_.Range = preferredRange;

				interval_.Parameter = value_.IntervalParameter;
				interval_.Default = value_.Interval;
			}
		}

		public virtual List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				initial_, range_, interval_, randomHalf_
			};
		}

		protected override void DoAddToUI()
		{
			// no-op
		}

		protected override void DoRemoveFromUI()
		{
			// no-op
		}

		protected void InitialChanged(int x)
		{
			if (value_ != null)
				value_.Initial = x;
		}

		protected void RangeChanged(int x)
		{
			if (value_ != null)
				value_.Range = x;
		}

		protected void IntervalChanged(float x)
		{
			if (value_ != null)
				value_.Interval = x;
		}

		private void RandomizeHalf()
		{
			var half = initial_.Value / 2;

			value_.Initial = half;
			value_.Range = Math.Abs(half);

			initial_.Value = half;
			range_.Value = Math.Abs(half);
		}
	}


	abstract class RandomizableValueMonitorWidgets<T, Parameter> : CompoundWidget
		where Parameter : BasicParameter<T>
	{
		private readonly string name_;

		protected BasicSlider<T, Parameter> current_;
		protected readonly FloatSlider next_;

		protected RandomizableValueMonitorWidgets(string name, int flags)
			: base(flags)
		{
			name_ = name;

			next_ = new FloatSlider(
				MakeNextLabel(-1), 0f, new FloatRange(0f, 5f), null,
				flags_ | FloatSlider.Disabled | FloatSlider.Constrained);
		}

		public virtual List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				current_, next_
			};
		}

		protected override void DoAddToUI()
		{
			foreach (var w in GetWidgets())
				w.AddToUI();
		}

		protected override void DoRemoveFromUI()
		{
			foreach (var w in GetWidgets())
				w.RemoveFromUI();
		}

		public virtual void SetValue(BasicRandomizableValue<T, Parameter> value)
		{
			if (value == null)
				return;

			current_.SetFromRange(value.Initial, value.Range, value.Current);

			next_.Text = MakeNextLabel(value.ActualInterval);
			next_.Range = new FloatRange(0, value.ActualInterval);

			if (value.ActualInterval == 0)
				next_.Value = 0;
			else
				next_.Value = value.TotalElapsed;
		}

		public string MakeNextLabel(float next)
		{
			string s = name_ + " next change";

			if (next == 0)
				s += " (always)";
			else
				s += " (" + next.ToString("0.00") + ")";

			return s;
		}
	}


	class RandomizableFloatMonitorWidgets
		: RandomizableValueMonitorWidgets<float, FloatParameter>
	{
		public RandomizableFloatMonitorWidgets(string name, int flags = 0)
			: base(name, flags)
		{
			current_ = new FloatSlider(
				name, 0f, new FloatRange(0f, 5f), null,
				flags_ | FloatSlider.Disabled | FloatSlider.Constrained);
		}
	}


	class RandomizableIntMonitorWidgets
		: RandomizableValueMonitorWidgets<int, IntParameter>
	{
		public RandomizableIntMonitorWidgets(string name, int flags = 0)
			: base(name, flags)
		{
			current_ = new IntSlider(
				name, 0, new IntRange(0, 10), null,
				flags_ | FloatSlider.Disabled | FloatSlider.Constrained);
		}
	}


	class RandomizableTimeMonitorWidgets
		: RandomizableValueMonitorWidgets<float, FloatParameter>
	{
		private readonly FloatSlider progress_;

		public RandomizableTimeMonitorWidgets(string name, int flags = 0)
			: base(name, flags)
		{
			current_ = new FloatSlider(
				name, 0f, new FloatRange(0f, 5f), null,
				flags_ | FloatSlider.Disabled | FloatSlider.Constrained);

			progress_ = new FloatSlider(
				name + " progress", 0f, new FloatRange(0f, 1f), null,
				flags_ | FloatSlider.Disabled | FloatSlider.Constrained);
		}

		public override List<IWidget> GetWidgets()
		{
			var list = base.GetWidgets();
			list.Add(progress_);
			return list;
		}

		public override void SetValue(
			BasicRandomizableValue<float, FloatParameter> value)
		{
			base.SetValue(value);

			if (value == null)
				return;

			if (value.ActualInterval == 0)
			{
				// always
				progress_.Value = 1;
				progress_.Range = new FloatRange(0, 1);
			}
			else
			{
				progress_.Set(0, value.Current, value.Elapsed);
			}
		}
	}
}
