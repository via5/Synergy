namespace Synergy.NewUI
{
	public class Strings
	{
		public static string Get(string s)
		{
			return s;
		}
	}


	class ToolButton : UI.Button
	{
		public ToolButton(string text = "")
			: base(text)
		{
			MinimumSize = new UI.Size(50, DontCare);
		}
	}

	class StepControls : UI.Panel
	{
		private readonly UI.TypedComboBox<Step> steps_;
		private readonly UI.Button add_, clone_, clone0_, remove_, up_, down_;

		public StepControls()
		{
			Layout = new UI.HorizontalFlow(20);

			steps_ = new UI.TypedComboBox<Step>();
			add_ = new ToolButton("+");
			clone_ = new ToolButton(S("Clone"));
			clone0_ = new ToolButton(S("Clone 0"));
			remove_ = new ToolButton("\x2013");  // en dash
			up_ = new ToolButton("\x25b2");      // up arrow
			down_ = new ToolButton("\x25bc");    // down arrow

			Add(new UI.Label(S("Step:")));
			Add(steps_);
			Add(add_);
			Add(clone_);
			Add(clone0_);
			Add(remove_);
			Add(up_);
			Add(down_);

			add_.Clicked += AddStep;
			clone_.Clicked += CloneStep;
			clone0_.Clicked += CloneStepZero;
			remove_.Clicked += RemoveStep;
			up_.Clicked += MoveStepUp;
			down_.Clicked += MoveStepDown;
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
	}


	class StepInfo : UI.Panel
	{
		private UI.TextBox name_;
		private UI.CheckBox enabled_, halfMove_;

		public StepInfo()
		{
			Layout = new UI.HorizontalFlow(10);

			name_ = new UI.TextBox("Step 1");
			enabled_ = new UI.CheckBox(S("Step enabled"));
			halfMove_ = new UI.CheckBox(S("Half move"));

			Add(new UI.Label(S("Name")));
			Add(name_);
			Add(enabled_);
			Add(halfMove_);
		}
	}


	class TimeWidgets : UI.Panel
	{
		public TimeWidgets()
		{
			Layout = new UI.HorizontalFlow(5);

			Add(new UI.TextBox("1"));
			Add(new UI.Button("-1"));
			Add(new UI.Button("0"));
			Add(new UI.Button(S("Reset")));
			Add(new UI.Button("+1"));
		}
	}


	class RandomDurationWidgets : UI.Panel
	{
		public RandomDurationWidgets()
		{
			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Random range")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Random interval")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Cut-off")));
			Add(new UI.ComboBox());
		}
	}


	class RampDurationWidgets : UI.Panel
	{
		public RampDurationWidgets()
		{
			var gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			Layout = gl;

			Add(new UI.Label(S("Time")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Minimum duration")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Maximum duration")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Hold maximum")));
			Add(new TimeWidgets());

			Add(new UI.Label(S("Easing")));
			Add(new UI.ComboBox());

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(new UI.CheckBox(S("Ramp up")));
			ramps.Add(new UI.CheckBox(S("Ramp down")));

			Add(new UI.Panel());
			Add(ramps);
		}
	}


	class DurationWidgets : UI.Panel
	{
		private UI.Panel widgets_ = new RandomDurationWidgets();

		public DurationWidgets()
		{
			Layout = new UI.VerticalFlow(50);

			var p = new UI.Panel(new UI.HorizontalFlow(20));
			p.Add(new UI.Label(S("Duration type")));
			p.Add(new UI.ComboBox());

			Add(p);
			Add(widgets_);
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


	class DelayWidgets : UI.Panel
	{
		private readonly UI.CheckBox halfWay_, end_;
		private readonly DurationWidgets duration_ = new DurationWidgets();

		public DelayWidgets()
		{
			Layout = new UI.VerticalFlow(30);

			halfWay_ = new UI.CheckBox(S("Halfway"));
			end_ = new UI.CheckBox(S("End"));

			var p = new UI.Panel(new UI.HorizontalFlow());
			p.Add(halfWay_);
			p.Add(end_);

			Add(p);
			Add(duration_);
		}
	}


	class StepTab : UI.Panel
	{
		private readonly StepInfo info_ = new StepInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();

		public StepTab()
		{
			Layout = new UI.BorderLayout(20);
			Layout.Spacing = 30;

			tabs_.AddTab(S("Duration"), new DurationWidgets());
			tabs_.AddTab(S("Repeat"), new RepeatWidgets());
			tabs_.AddTab(S("Delay"), new DelayWidgets());

			Add(tabs_, UI.BorderLayout.Center);
		}
	}

	class ModifierInfo : UI.Panel
	{
		public ModifierInfo()
		{
			Layout = new UI.VerticalFlow(20);

			var p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Name")));
			p.Add(new UI.TextBox("RT X head Person"));
			p.Add(new UI.CheckBox(S("Modifier enabled")));
			Add(p);

			p = new UI.Panel(new UI.HorizontalFlow(10));
			p.Add(new UI.Label(S("Modifier type")));
			p.Add(new UI.ComboBox());
			Add(p);
		}
	}


	class ModifierSyncPanel : UI.Panel
	{
		public ModifierSyncPanel()
		{
		}
	}


	class RigidbodyPanel : UI.Panel
	{
	}


	class MorphPanel : UI.Panel
	{
	}


	class ModifierPanel : UI.Panel
	{
		private readonly ModifierInfo info_ = new ModifierInfo();
		private readonly UI.Tabs tabs_ = new UI.Tabs();

		public ModifierPanel()
		{
			Layout = new UI.BorderLayout(30);

			var sync = new UI.Panel();
			var rigidbody = new UI.Panel();
			var morph = new UI.Panel();

			tabs_.AddTab(S("Sync"), new ModifierSyncPanel());
			tabs_.AddTab(S("Rigidbody"), new RigidbodyPanel());
			tabs_.AddTab(S("Morph"), new MorphPanel());

			Add(info_, UI.BorderLayout.Top);
			Add(tabs_, UI.BorderLayout.Center);
		}
	}


	class ModifiersTab : UI.Panel
	{
		private readonly UI.ListView list_ = new UI.ListView();
		private readonly ModifierPanel modifier_ = new ModifierPanel();

		public ModifiersTab()
		{
			Layout = new UI.BorderLayout(20);

			Add(list_, UI.BorderLayout.Left);
			Add(modifier_, UI.BorderLayout.Center);
		}
	}


	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private StepControls steps_ = new StepControls();
		private StepTab stepTab_ = new StepTab();
		private ModifiersTab modifiersTab_ = new ModifiersTab();

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			var tabs = new UI.Tabs();
			tabs.AddTab(S("Step"), stepTab_);
			tabs.AddTab(S("Modifiers"), modifiersTab_);

			root_.Layout = new UI.BorderLayout(30);
			root_.Add(steps_, UI.BorderLayout.Top);
			root_.Add(tabs, UI.BorderLayout.Center);

			root_.DoLayout();
			root_.Create();
		}
	}
}
