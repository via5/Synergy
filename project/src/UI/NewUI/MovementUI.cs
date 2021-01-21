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
		private bool ignore_ = false;

		public MovementWidgets(int flags = NoFlags)
		{
			var hf = new UI.HorizontalFlow(5);
			hf.Expand = false;
			Layout = hf;

			Add(text_);
			text_.MinimumSize = new Size(
				Root.TextLength(Font, FontSize, "9999.99") + 20, DontCare);

			if (!Bits.IsSet(flags, SmallMovement))
				Add(CreateButton("-100", -100));

			Add(CreateButton("-10",  -10));
			Add(CreateButton("-1",   -1));
			Add(CreateButton("-.1", -0.1f));
			Add(CreateButton("0",     0));
			Add(CreateButton("+.1", +0.1f));
			Add(CreateButton("+1",   +1));
			Add(CreateButton("+10",  +10));

			if (!Bits.IsSet(flags, SmallMovement))
				Add(CreateButton("+100", +100));

			Add(new ToolButton(S("R"), OnReset));

			text_.Edited += OnTextChanged;
		}

		public void Set(float f)
		{
			using (new ScopedFlag((b) => ignore_ = b))
			{
				reset_ = f;
				last_ = f;
				text_.Text = f.ToString("0.00");
			}
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

		private RandomizableFloat rf_ = null;
		private bool ignore_ = false;

		public MovementPanel(
			string caption, int flags = MovementWidgets.NoFlags)
		{
			value_ = new MovementWidgets(flags);
			range_ = new MovementWidgets(flags);
			interval_ = new MovementWidgets(flags);

			var gl = new UI.GridLayout(2);
			gl.HorizontalStretch = new List<bool>() { false, true };
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;

			var p = new UI.Panel(gl);
			p.Add(new UI.Label(S("Value")));
			p.Add(value_);
			p.Add(new UI.Label(S("Range")));
			p.Add(range_);
			p.Add(new UI.Label(S("Interval")));
			p.Add(interval_);

			Layout = new VerticalFlow(10);
			Add(new UI.Label(caption));
			Add(p);

			value_.Changed += OnValueChanged;
			range_.Changed += OnRangeChanged;
			interval_.Changed += OnIntervalChanged;
		}

		public void Set(RandomizableFloat f)
		{
			rf_ = f;
			value_.Set(f.Initial);
			range_.Set(f.Range);
			interval_.Set(f.Interval);
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
	}
}
