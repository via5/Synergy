using Synergy.UI;
using System.Collections.Generic;

namespace Synergy
{
	class NewUI
	{
		private Root root_ = new Root();
		private TypedComboBox<Step> steps_;

		public NewUI()
		{
			root_.Layout = new UI.BorderLayout();
			root_.Layout.Spacing = 30;

			var top = new UI.Panel();
			top.Layout = new UI.HorizontalFlow(20);
			//top.Add(new Button("button"));
			//top.Add(new ComboBox(new List<string>{ "a", "b" }));
			//top.Add(new Label("label"));
			//
			//var bottom = new Widget();
			//bottom.Layout = new HorizontalFlow(20);
			//bottom.Add(new Button("button"));
			//bottom.Add(new ComboBox(new List<string> { "a", "b" }));
			//bottom.Add(new Label("label"));
			//
			//root_.Add(top, BorderLayout.Top);
			//root_.Add(bottom, BorderLayout.Bottom);


			steps_ = new UI.TypedComboBox<Step>();

			top.Add(new UI.Label("Step:"));
			top.Add(steps_);

			var b = new UI.Button("+");
			b.MinimumSize = new Size(50, UI.Panel.DontCare);
			top.Add(b);
			b.Clicked += () =>
			{
				var s = Synergy.Instance.Manager.AddStep();
				steps_.AddItem(s, true);
			};


			b = new UI.Button("Clone");
			b.MinimumSize = new Size(50, UI.Panel.DontCare);
			top.Add(b);
			b.Clicked += () =>
			{
				var s = steps_.Selected;
				if (s != null)
				{
					var ns = Synergy.Instance.Manager.AddStep(s.Clone());
					steps_.AddItem(s, true);
				}
			};

			b = new UI.Button("Clone 0");
			b.MinimumSize = new Size(50, UI.Panel.DontCare);
			top.Add(b);
			b.Clicked += () =>
			{
				var s = steps_.Selected;
				if (s != null)
				{
					var ns = Synergy.Instance.Manager.AddStep(
						s.Clone(global::Synergy.Utilities.CloneZero));

					steps_.AddItem(s, true);
				}
			};

			b = new UI.Button("\x2013");
			b.MinimumSize = new Size(50, UI.Panel.DontCare);
			top.Add(b);
			b.Clicked += () =>
			{
				var s = steps_.Selected;
				if (s != null)
				{
					Synergy.Instance.Manager.DeleteStep(s);
					steps_.RemoveItem(s);
				}
			};

			top.Add(new UI.Button("\x25b2"));
			top.Add(new UI.Button("\x25bc"));

			root_.Add(top, UI.BorderLayout.Top);

			var tabs = new UI.Tabs();

			var steptab = new UI.Panel();
			steptab.Layout = new UI.BorderLayout();
			steptab.Layout.Spacing = 30;

			var stepcontrols = new UI.Panel();
			stepcontrols.Layout = new UI.HorizontalFlow();
			stepcontrols.Add(new UI.CheckBox("Step enabled"));
			stepcontrols.Add(new UI.CheckBox("Half move"));

			var steptabs = new Tabs();

			var stepduration = new UI.Panel();
			var gl = new UI.GridLayout(2);
			stepduration.Layout = gl;
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			stepduration.Add(new UI.Label("Type"));
			stepduration.Add(new UI.ComboBox());
			AddRandomDuration(stepduration);
			steptabs.AddTab("Duration", stepduration);


			var steprepeat = new UI.Panel();
			gl = new UI.GridLayout(2);
			steprepeat.Layout = gl;
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;

			AddRandomDuration(steprepeat);
			steptabs.AddTab("Repeat", steprepeat);


			var stepdelay = new UI.Panel();

			var controls = new UI.Panel();
			controls.Layout = new UI.HorizontalFlow();
			controls.Add(new UI.CheckBox("Halfway"));
			controls.Add(new UI.CheckBox("End"));

			var duration = new UI.Panel();
			gl = new UI.GridLayout(2);
			gl.HorizontalSpacing = 10;
			gl.VerticalSpacing = 20;
			duration.Layout = gl;

			duration.Add(new UI.Label("Type"));
			duration.Add(new UI.ComboBox());
			AddRampDuration(duration);

			stepdelay.Layout = new UI.VerticalFlow(30);
			stepdelay.Add(controls);
			stepdelay.Add(duration);

			steptabs.AddTab("Delay", stepdelay);

			steptab.Add(stepcontrols, UI.BorderLayout.Top);
			steptab.Add(steptabs, UI.BorderLayout.Center);

			var modifierstab = new UI.Panel();
			modifierstab.Layout = new UI.BorderLayout();

			var list = new UI.ListView();
			var modifier = new UI.Panel();
			modifierstab.Add(list, UI.BorderLayout.Left);
			modifierstab.Add(modifier, UI.BorderLayout.Center);


			tabs.AddTab("Step", steptab);
			tabs.AddTab("Modifiers", modifierstab);

			root_.Add(tabs, UI.BorderLayout.Center);

			//root_.Layout = new BorderLayout();
			//root_.Add(new Label("left"), BorderLayout.Left);
			//root_.Add(new Label("top"), BorderLayout.Top);
			//root_.Add(new Label("right"), BorderLayout.Right);
			//root_.Add(new Label("bottom"), BorderLayout.Bottom);
			//root_.Add(new Label("center"), BorderLayout.Center);

			/*
			var w = new Widget();
			w.Bounds = Rectangle.FromPoints(200, 200, 300, 400);
			w.Borders = new Insets(20);
			root_.Add(w);*/

			root_.DoLayout();
			root_.Create();

			root_.Dump();
		}

		private void AddRandomDuration(UI.Panel parent)
		{
			parent.Add(new UI.Label("Time"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Random range"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Random interval"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Cut-off"));
			parent.Add(new UI.ComboBox());
		}

		private void AddRampDuration(UI.Panel parent)
		{
			parent.Add(new UI.Label("Time"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Minimum duration"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Maximum duration"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Hold maximum"));
			parent.Add(CreateTimeWidgets());

			parent.Add(new UI.Label("Easing"));
			parent.Add(new UI.ComboBox());

			var ramps = new UI.Panel();
			ramps.Layout = new UI.HorizontalFlow();
			ramps.Add(new UI.CheckBox("Ramp up"));
			ramps.Add(new UI.CheckBox("Ramp down"));

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
			w.Add(new UI.Button("Reset"));
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

			steps_.SetItems(items, sel);
		}
	}
}
