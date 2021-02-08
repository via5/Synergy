using Synergy.UI;
using System;
using System.Collections.Generic;

namespace Synergy.NewUI
{
	class SmallButton : UI.Button
	{
		public SmallButton(string text = "", Callback clicked = null)
			: base(text, clicked)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return DoGetMinimumSize();
		}

		protected override Size DoGetMinimumSize()
		{
			var s = Root.TextSize(Font, FontSize, Text);
			s.Width += 10;
			s.Height = 40;
			return s;
		}
	}


	class MovementWidgets : UI.Panel
	{
		public const int NoFlags       = 0x00;
		public const int SmallMovement = 0x01;

		public delegate void ValueCallback(float f);
		public event ValueCallback Changed;

		private readonly TextBox text_ = new TextBox();
		private float reset_ = 0;
		private float last_ = 0;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public MovementWidgets(int flags = NoFlags)
			: this(null, flags)
		{
		}

		public MovementWidgets(ValueCallback callback, int flags = NoFlags)
		{
			var hf = new UI.HorizontalFlow(5);
			hf.Expand = false;
			Layout = hf;

			Add(text_);

			string minText;

			if (Bits.IsSet(flags, SmallMovement))
				minText = "9.99";
			else
				minText = "9999.99";

			text_.MinimumSize = new Size(
				Root.TextLength(Font, FontSize, minText) + 20, DontCare);

			if (!Bits.IsSet(flags, SmallMovement))
			{
				Add(CreateButton("-100", -100));
				Add(CreateButton("-10", -10));
			}

			Add(CreateButton("-1", -1));
			Add(CreateButton("-.1", -0.1f));

			if (Bits.IsSet(flags, SmallMovement))
				Add(CreateButton("-.01", -0.01f));

			Add(CreateButton("0", 0));

			if (Bits.IsSet(flags, SmallMovement))
				Add(CreateButton("+.01", +0.01f));

			Add(CreateButton("+.1", 0.1f));
			Add(CreateButton("+1", 1));

			if (!Bits.IsSet(flags, SmallMovement))
			{
				Add(CreateButton("+10", 10));
				Add(CreateButton("+100", +100));
			}

			Add(new ToolButton(S("R"), OnReset));

			text_.Edited += OnTextChanged;

			if (callback != null)
				Changed += callback;
		}

		public void Set(float f)
		{
			ignore_.Do(() =>
			{
				reset_ = f;
				last_ = f;
				text_.Text = f.ToString("0.00");
			});
		}

		private SmallButton CreateButton(string t, float d)
		{
			SmallButton b;

			if (d == 0)
				b = new SmallButton(t, OnZero);
			else
				b = new SmallButton(t, () => OnAdd(d));

			return b;
		}

		private void OnAdd(float d)
		{
			float f;
			if (float.TryParse(text_.Text, out f))
				Change(f + d);
		}

		private void OnZero()
		{
			Change(0);
		}

		private void OnReset()
		{
			Change(reset_);
		}

		private void OnTextChanged(string s)
		{
			if (ignore_)
				return;

			float f;
			if (float.TryParse(s, out f))
				Change(f);
			else
				Change(last_);
		}

		private void Change(float f)
		{
			last_ = f;
			text_.Text = f.ToString("0.00");
			Changed?.Invoke(f);
		}
	}


	class MovementPanel : UI.Panel
	{
		private readonly MovementWidgets value_;
		private readonly MovementWidgets range_;
		private readonly MovementWidgets interval_;
		private readonly UI.Panel buttonsPanel_;
		private readonly UI.Button randomizeHalf_;

		private RandomizableFloat rf_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();

		public MovementPanel(
			string caption, int flags = MovementWidgets.NoFlags)
		{
			value_ = new MovementWidgets(flags);
			range_ = new MovementWidgets(flags);
			interval_ = new MovementWidgets(flags);
			buttonsPanel_ = new Panel(new UI.HorizontalFlow(10));
			randomizeHalf_ = new UI.Button(S("Randomize half"), OnRandomizeHalf);

			var gl = new UI.GridLayout(2);
			gl.HorizontalStretch = new List<bool>() { false, true };
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 10;

			buttonsPanel_.Add(new UI.Label(caption));
			buttonsPanel_.Add(randomizeHalf_);

			var p = new UI.Panel(gl);
			p.Add(new UI.Label(S("Value")));
			p.Add(value_);
			p.Add(new UI.Label(S("Range")));
			p.Add(range_);
			p.Add(new UI.Label(S("Interval")));
			p.Add(interval_);

			Layout = new VerticalFlow(10);
			Add(buttonsPanel_);
			Add(p);

			value_.Changed += OnValueChanged;
			range_.Changed += OnRangeChanged;
			interval_.Changed += OnIntervalChanged;
		}

		public UI.Panel ButtonsPanel
		{
			get { return buttonsPanel_; }
		}

		public void Set(RandomizableFloat f)
		{
			rf_ = f;

			ignore_.Do(() =>
			{
				value_.Set(f.Initial);
				range_.Set(f.Range);
				interval_.Set(f.Interval);
			});
		}

		private void OnValueChanged(float f)
		{
			if (ignore_)
				return;

			rf_.Initial = f;
		}

		private void OnRangeChanged(float f)
		{
			if (ignore_)
				return;

			rf_.Range = f;
		}

		private void OnIntervalChanged(float f)
		{
			if (ignore_)
				return;

			rf_.Interval = f;
		}

		private void OnRandomizeHalf()
		{
			var half = rf_.Initial / 2;

			rf_.Initial = half;
			rf_.Range = Math.Abs(half);

			value_.Set(rf_.Initial);
			range_.Set(rf_.Range);
		}
	}


	class MovementUI : UI.Panel
	{
		private Movement m_ = null;

		private readonly IgnoreFlag ignore_ = new IgnoreFlag();
		private readonly MovementPanel min_;
		private readonly MovementPanel max_;
		private readonly FactoryComboBox<EasingFactory, IEasing> easing_;

		public MovementUI(int flags = MovementWidgets.NoFlags)
		{
			min_ = new MovementPanel(S("Min"), flags);
			max_ = new MovementPanel(S("Max"), flags);
			easing_ = new FactoryComboBox<EasingFactory, IEasing>();

			Layout = new UI.VerticalFlow(20);

			var easing = new UI.Panel(new UI.HorizontalFlow(10));
			easing.Add(new UI.Label(S("Easing")));
			easing.Add(easing_);

			Add(easing);
			Add(min_);
			Add(max_);

			easing_.FactoryTypeChanged += OnEasingChanged;
		}

		public MovementPanel MinimumPanel
		{
			get { return min_; }
		}

		public MovementPanel MaximumPanel
		{
			get { return max_; }
		}

		public void Set(Movement m)
		{
			m_ = m;

			ignore_.Do(() =>
			{
				min_.Set(m_.Minimum);
				max_.Set(m_.Maximum);
				easing_.Select(m_.Easing);
			});
		}

		private void OnEasingChanged(IEasing easing)
		{
			if (ignore_ || m_ == null)
				return;

			m_.Easing = easing;
		}
	}
}
