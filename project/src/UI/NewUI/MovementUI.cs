using Synergy.UI;
using System;
using System.Collections.Generic;

namespace Synergy.NewUI
{
	class MovementWidgets : UI.Panel
	{
		public delegate void ValueCallback(float f);
		public event ValueCallback Changed;

		private readonly TextBox text_ = new TextBox();
		private float reset_ = 0;
		private float last_ = 0;
		private bool ignore_ = false;

		public MovementWidgets()
		{
			Layout = new UI.HorizontalFlow(5);

			Add(text_);
			text_.MinimumSize = new Size(Root.TextLength("9999.99") + 20, DontCare);

			Add(CreateButton("-10",  -10));
			Add(CreateButton("-1",   -1));
			Add(CreateButton("-0.1", -0.1f));
			Add(CreateButton("0",     0));
			Add(CreateButton("+0.1", +0.1f));
			Add(CreateButton("+1",   +1));
			Add(CreateButton("+10",  +10));

			Add(new ToolButton(S("Reset"), OnReset));

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

		private ToolButton CreateButton(string t, float d)
		{
			if (d == 0)
				return new ToolButton(t, OnZero);
			else
				return new ToolButton(t, () => OnAdd(d));
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
		private readonly MovementWidgets value_ = new MovementWidgets();
		private readonly MovementWidgets range_ = new MovementWidgets();
		private readonly MovementWidgets interval_ = new MovementWidgets();

		private RandomizableFloat rf_ = null;
		private bool ignore_ = false;

		public MovementPanel(string caption)
		{
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
