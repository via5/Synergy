using System.Collections.Generic;
using UnityEngine;

namespace Synergy.UI
{
	class Test
	{
		private Root root_ = new Root();
		private TypedComboBox<Step> steps_;

		public Test()
		{
			root_.Layout = new BorderLayout();
			root_.Layout.Spacing = 30;

			var top = new Widget();
			top.Layout = new HorizontalFlow(20);
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


			steps_ = new TypedComboBox<Step>();
			//cb.AddItem("Step 1");
			//cb.AddItem("Step 2");
			//cb.AddItem("Step 3");

			top.Add(new Label("Step:"));
			top.Add(steps_);

			var b = new Button("+");
			b.MinimumSize = new Size(50, Widget.DontCare);
			top.Add(b);
			b.Clicked += () =>
			{
				var s = Synergy.Instance.Manager.AddStep();
				steps_.AddItem(s, true);
			};


			b = new Button("Clone");
			b.MinimumSize = new Size(50, Widget.DontCare);
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

			b = new Button("Clone 0");
			b.MinimumSize = new Size(50, Widget.DontCare);
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

			b = new Button("\x2013");
			b.MinimumSize = new Size(50, Widget.DontCare);
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

			top.Add(new Button("\x25b2"));
			top.Add(new Button("\x25bc"));

			root_.Add(top, BorderLayout.Top);

			var tabs = new Tabs();

			var steptab = new Widget();
			steptab.Layout = new BorderLayout();
			steptab.Layout.Spacing = 30;

			var stepcontrols = new Widget();
			stepcontrols.Layout = new HorizontalFlow();
			stepcontrols.Add(new CheckBox("Step enabled"));
			stepcontrols.Add(new CheckBox("Half move"));

			var steptabs = new Tabs();

			var stepduration = new Widget();
			var steprepeat = new Widget();
			var stepdelay = new Widget();

			steptabs.AddTab("Duration", stepduration);
			steptabs.AddTab("Repeat", stepduration);
			steptabs.AddTab("Delay", stepduration);

			steptab.Add(stepcontrols, BorderLayout.Top);
			steptab.Add(steptabs, BorderLayout.Center);

			var modifierstab = new Widget();
			modifierstab.Layout = new BorderLayout();
			modifierstab.Add(new Label("modifiers tab"), BorderLayout.Top);

			tabs.AddTab("Step", steptab);
			tabs.AddTab("Modifiers", modifierstab);

			root_.Add(tabs, BorderLayout.Center);

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
			/*
			root_.Dump();*/
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


	class Root : Widget
	{
		static public Transform PluginParent = null;
		static public Font DefaultFont =
			(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
		static public int DefaultFontSize = 28;
		static public Color DefaultTextColor = new Color(0.85f, 0.85f, 0.85f);

		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_;

		static public UIPopup OpenedPopup = null;

		static public void SetFocus(Widget w)
		{
			if (OpenedPopup != null)
			{
				if (OpenedPopup.visible)
					OpenedPopup.Toggle();

				OpenedPopup = null;
			}
		}

		public Root()
		{
			Bounds = Rectangle.FromPoints(2, 1, 1078, 1228);
			Margins = new Insets(5);
			//Borders = new Insets(20);

			{
				var b = Synergy.Instance.CreateButton("b");
				tg_ = b.buttonText.cachedTextGenerator;
				ts_ = b.buttonText.GetGenerationSettings(new Vector2());
				PluginParent = b.transform.parent;
				Synergy.Instance.RemoveButton(b);
			}

			var p = PluginParent;

			var content = p.parent;
			if (PluginParent == content) Synergy.LogError("1");
			var viewport = content.parent;
			if (PluginParent == viewport) Synergy.LogError("2");
			var scrollview = viewport.parent;
			if (PluginParent == scrollview) Synergy.LogError("3");
			var scriptui = scrollview.parent;
			if (PluginParent == scriptui) Synergy.LogError("4");

			var pp = scriptui.parent;
			if (PluginParent == pp) Synergy.LogError("5");

			pp = pp.parent;
			if (PluginParent == pp) Synergy.LogError("6");

			var a = scrollview.GetComponent<UnityEngine.UI.Image>();
			a.color = new Color(0.15f, 0.15f, 0.15f);
		}

		public static float TextLength(string s)
		{
			return tg_.GetPreferredWidth(s, ts_);
		}

		public override string TypeName
		{
			get
			{
				return "root";
			}
		}
	}
}
