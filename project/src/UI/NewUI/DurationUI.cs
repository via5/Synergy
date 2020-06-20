namespace Synergy.NewUI
{
	class TimeWidgets : UI.Panel
	{
		public delegate void ValueCallback(float f);
		public event ValueCallback Changed;

		private readonly UI.TextBox text_;
		private float reset_ = 0;
		private float current_ = 0;

		public TimeWidgets(ValueCallback changed = null)
		{
			text_ = new UI.TextBox();
			text_.Validate += OnValidate;
			text_.Changed += OnChanged;

			Layout = new UI.HorizontalFlow(5);

			Add(text_);
			Add(new UI.ToolButton("-1", () => AddValue(-1)));
			Add(new UI.ToolButton("-.1", () => AddValue(-0.1f)));
			Add(new UI.ToolButton("-.01", () => AddValue(-0.01f)));
			Add(new UI.ToolButton("0", () => SetValue(0)));
			Add(new UI.ToolButton("+.01", () => AddValue(+0.01f)));
			Add(new UI.ToolButton("+.1", () => AddValue(+0.1f)));
			Add(new UI.ToolButton("+1", () => AddValue(+1)));
			Add(new UI.ToolButton(S("Reset"), () => Reset()));

			if (changed != null)
				Changed += changed;
		}

		public void Set(float f)
		{
			reset_ = f;
			current_ = f;

			text_.Text = f.ToString("0.00");
		}

		public void AddValue(float d)
		{
			SetValue(current_ + d);
		}

		public void Reset()
		{
			SetValue(reset_);
		}

		private void OnValidate(UI.TextBox.Validation v)
		{
			float r;
			v.valid = float.TryParse(v.text, out r);
		}

		private void OnChanged(string s)
		{
			float r;
			if (float.TryParse(s, out r))
				SetValue(r);
		}

		private void SetValue(float v)
		{
			current_ = v;
			text_.Text = v.ToString();
			Changed?.Invoke(v);
		}
	}


	class RandomizableTimePanel : UI.Panel
	{
		private readonly TimeWidgets time_, range_, interval_;
		private readonly UI.ComboBox cutoff_;
		private RandomizableTime rt_ = null;

		public RandomizableTimePanel(RandomizableTime rt = null)
		{
			time_ = new TimeWidgets(OnInitialChanged);
			range_ = new TimeWidgets(OnRangeChanged);
			interval_ = new TimeWidgets(OnIntervalChanged);
			cutoff_ = new UI.ComboBox(
				RandomizableTime.GetCutoffNames(), OnCutoffChanged);

			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(time_);

			Add(new UI.Label(S("Random range")));
			Add(range_);

			Add(new UI.Label(S("Random interval")));
			Add(interval_);

			Add(new UI.Label(S("Cut-off")));
			Add(cutoff_);

			Set(rt);
		}

		public void Set(RandomizableTime rt)
		{
			rt_ = rt;

			if (rt_ != null)
			{
				time_.Set(rt_.Initial);
				range_.Set(rt_.Range);
				interval_.Set(rt_.Interval);
				cutoff_.Select(RandomizableTime.CutoffToString(rt_.Cutoff));
			}
		}

		private void OnInitialChanged(float f)
		{
			rt_.Initial = f;
		}

		private void OnRangeChanged(float f)
		{
			rt_.Range = f;
		}

		private void OnIntervalChanged(float f)
		{
			rt_.Interval = f;
		}

		private void OnCutoffChanged(string s)
		{
			var c = RandomizableTime.CutoffFromString(s);
			if (c == -1)
			{
				Synergy.LogError("bad cutoff '" + s + "'");
				return;
			}

			rt_.Cutoff = c;
		}
	}


	class DurationPanel : UI.Panel
	{
		public delegate void Callback(IDuration d);
		public event Callback Changed;

		private readonly FactoryComboBox<DurationFactory, IDuration> type_;
		private DurationWidgets widgets_ = null;
		private IDuration duration_ = null;
		private bool ignore_ = false;

		public DurationPanel()
		{
			type_ = new FactoryComboBox<DurationFactory, IDuration>(
				OnTypeChanged);

			Layout = new UI.VerticalFlow(50);

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Duration type")));
			p.Add(type_);

			Add(p);
		}

		public void Set(IDuration d)
		{
			using (new ScopedFlag((b) => ignore_ = b))
			{
				duration_ = d;

				if (widgets_ == null || !widgets_.Set(d))
					SetWidgets(DurationWidgets.Create(d));

				type_.Select(d);
			}
		}

		private void SetWidgets(DurationWidgets p)
		{
			if (widgets_ != null)
				widgets_.Remove();

			widgets_ = p;

			if (widgets_ != null)
				Add(widgets_);
		}

		private void OnTypeChanged(IDuration d)
		{
			if (ignore_)
				return;

			Set(d);
			Changed?.Invoke(d);
		}
	}


	abstract class DurationWidgets : UI.Panel
	{
		public abstract bool Set(IDuration d);

		public static DurationWidgets Create(IDuration d)
		{
			if (d is RandomDuration)
				return new RandomDurationWidgets(d as RandomDuration);
			else if (d is RampDuration)
				return new RampDurationWidgets(d as RampDuration);
			else
				return null;
		}
	}


	class RandomDurationWidgets : DurationWidgets
	{
		private readonly RandomizableTimePanel rt_;
		private RandomDuration duration_ = null;

		public RandomDurationWidgets(RandomDuration d = null)
		{
			rt_ = new RandomizableTimePanel(d?.Time);

			Layout = new UI.BorderLayout();
			Add(rt_, UI.BorderLayout.Center);
		}

		public override bool Set(IDuration d)
		{
			duration_ = d as RandomDuration;
			if (duration_ == null)
				return false;

			rt_.Set(duration_.Time);

			return true;
		}
	}


	class RampDurationWidgets : DurationWidgets
	{
		private readonly TimeWidgets over_, min_, max_, hold_;
		private readonly FactoryComboBox<EasingFactory, IEasing> easing_;
		private readonly UI.CheckBox rampUp_, rampDown_;

		private RampDuration duration_ = null;

		public RampDurationWidgets(RampDuration d = null)
		{
			over_ = new TimeWidgets(OnOverChanged);
			min_ = new TimeWidgets(OnMinimumChanged);
			max_ = new TimeWidgets(OnMaximumChanged);
			hold_ = new TimeWidgets(OnHoldChanged);
			easing_ = new FactoryComboBox<EasingFactory, IEasing>(
				OnEasingChanged);
			rampUp_ = new UI.CheckBox(S("Ramp up"), OnRampUpChanged);
			rampDown_ = new UI.CheckBox(S("Ramp down"), OnRampDownChanged);

			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;
			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(over_);

			Add(new UI.Label(S("Minimum duration")));
			Add(min_);

			Add(new UI.Label(S("Maximum duration")));
			Add(max_);

			Add(new UI.Label(S("Hold maximum")));
			Add(hold_);

			Add(new UI.Label(S("Easing")));
			Add(easing_);

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(rampUp_);
			ramps.Add(rampDown_);

			Add(new UI.Panel());
			Add(ramps);

			Set(d);
		}

		public override bool Set(IDuration d)
		{
			duration_ = (d as RampDuration);
			if (duration_ == null)
				return false;

			over_.Set(duration_.Over);
			min_.Set(duration_.Minimum);
			max_.Set(duration_.Maximum);
			hold_.Set(duration_.Hold);
			easing_.Select(duration_.Easing);
			rampUp_.Checked = duration_.RampUp;
			rampDown_.Checked = duration_.RampDown;

			return true;
		}

		private void OnOverChanged(float f)
		{
			duration_.Over = f;
		}

		private void OnMinimumChanged(float f)
		{
			duration_.Minimum = f;
		}

		private void OnMaximumChanged(float f)
		{
			duration_.Maximum = f;
		}

		private void OnHoldChanged(float f)
		{
			duration_.Hold = f;
		}

		private void OnEasingChanged(IEasing e)
		{
			duration_.Easing = e;
		}

		private void OnRampUpChanged(bool b)
		{
			duration_.RampUp = b;
		}

		private void OnRampDownChanged(bool b)
		{
			duration_.RampDown = b;
		}
	}
}
