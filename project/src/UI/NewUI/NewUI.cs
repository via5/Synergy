using System.Collections.Generic;

namespace Synergy
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


	class NewUI
	{
		private UI.Root root_ = new UI.Root();
		private StepControls steps_ = new StepControls();

		public static string S(string s)
		{
			return Strings.Get(s);
		}

		public NewUI()
		{
			root_.Layout = new UI.BorderLayout();
			root_.Layout.Spacing = 30;

			root_.Add(steps_, UI.BorderLayout.Top);

			var tabs = new UI.Tabs();

			var steptab = new UI.Panel();
			steptab.Layout = new UI.BorderLayout(20);
			steptab.Layout.Spacing = 30;

			var stepcontrols = new UI.Panel();
			stepcontrols.Layout = new UI.HorizontalFlow(10);
			stepcontrols.Add(new UI.Label(S("Name")));
			stepcontrols.Add(new UI.TextBox("Step 1"));
			stepcontrols.Add(new UI.CheckBox(S("Step enabled")));
			stepcontrols.Add(new UI.CheckBox(S("Half move")));


			var steptabs = new UI.Tabs();

			var stepduration = new UI.Panel();
			var gl = new UI.GridLayout(2);
			stepduration.Layout = gl;
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			stepduration.Add(new UI.Label(S("Duration type")));
			stepduration.Add(new UI.ComboBox());
			AddRandomDuration(stepduration);
			steptabs.AddTab(S("Duration"), stepduration);


			var steprepeat = new UI.Panel();
			gl = new UI.GridLayout(2);
			steprepeat.Layout = gl;
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			AddRandomDuration(steprepeat);
			steptabs.AddTab(S("Repeat"), steprepeat);


			var stepdelay = new UI.Panel();

			var controls = new UI.Panel();
			controls.Layout = new UI.HorizontalFlow();
			controls.Add(new UI.CheckBox(S("Halfway")));
			controls.Add(new UI.CheckBox(S("End")));

			var duration = new UI.Panel();
			gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;
			duration.Layout = gl;

			duration.Add(new UI.Label(S("Duration type")));
			duration.Add(new UI.ComboBox());
			AddRampDuration(duration);

			stepdelay.Layout = new UI.VerticalFlow(30);
			stepdelay.Add(controls);
			stepdelay.Add(duration);

			steptabs.AddTab(S("Delay"), stepdelay);

			steptab.Add(stepcontrols, UI.BorderLayout.Top);
			steptab.Add(steptabs, UI.BorderLayout.Center);

			var modifierstab = new UI.Panel();
			modifierstab.Layout = new UI.BorderLayout();

			var list = new UI.ListView();
			var modifier = new UI.Panel();
			modifier.Margins = new UI.Insets(20, 0, 0, 0);
			modifierstab.Add(list, UI.BorderLayout.Left);
			modifierstab.Add(modifier, UI.BorderLayout.Center);


			modifier.Layout = new UI.BorderLayout();

			var modifiercontrols = new UI.Panel();
			modifiercontrols.Layout = new UI.VerticalFlow(10);

			var modifiercontrols1 = new UI.Panel();
			modifiercontrols1.Layout = new UI.HorizontalFlow(10);
			modifiercontrols1.Add(new UI.Label(S("Name")));
			modifiercontrols1.Add(new UI.TextBox("RT X head Person"));
			modifiercontrols1.Add(new UI.CheckBox(S("Modifier enabled")));

			var modifiercontrols2 = new UI.Panel();
			modifiercontrols2.Layout = new UI.HorizontalFlow(10);
			modifiercontrols2.Add(new UI.Label(S("Modifier type")));
			modifiercontrols2.Add(new UI.ComboBox());

			modifiercontrols.Add(modifiercontrols1);
			modifiercontrols.Add(modifiercontrols2);

			modifier.Add(modifiercontrols, UI.BorderLayout.Top);

			var modifiertabs = new UI.Tabs();

			var sync = new UI.Panel();
			var rigidbody = new UI.Panel();
			var morph = new UI.Panel();

			modifiertabs.AddTab(S("Sync"), sync);
			modifiertabs.AddTab(S("Rigidbody"), rigidbody);
			modifiertabs.AddTab(S("Morph"), morph);

			modifier.Add(modifiertabs, UI.BorderLayout.Center);


			tabs.AddTab(S("Step"), steptab);
			tabs.AddTab(S("Modifiers"), modifierstab);

			root_.Add(tabs, UI.BorderLayout.Center);

			root_.DoLayout();
			root_.Create();
		}

		private void AddRandomDuration(UI.Panel parent)
		{
			parent.Add(new UI.Label(S("Time")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Random range")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Random interval")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Cut-off")));
			parent.Add(new UI.ComboBox());
		}

		private void AddRampDuration(UI.Panel parent)
		{
			parent.Add(new UI.Label(S("Time")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Minimum duration")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Maximum duration")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Hold maximum")));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label(S("Easing")));
			parent.Add(new UI.ComboBox());

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(new UI.CheckBox(S("Ramp up")));
			ramps.Add(new UI.CheckBox(S("Ramp down")));

			parent.Add(new UI.Panel());
			parent.Add(ramps);
		}

		private UI.Panel CreateTimeWidgets()
		{
			var w = new UI.Panel();
			w.Layout = new UI.HorizontalFlow(5);

			w.Add(new UI.TextBox("1"));
			w.Add(new UI.Button("-1"));
			w.Add(new UI.Button("0"));
			w.Add(new UI.Button(S("Reset")));
			w.Add(new UI.Button("+1"));

			return w;
		}

		public void UpdateSteps(Step sel = null)
		{
			if (sel == null)
				sel = steps_.Selected;

			var items = new List<Step>();

			foreach (var s in Synergy.Instance.Manager.Steps)
				items.Add(s);

			//steps_.SetItems(items, sel);
		}
	}
}
