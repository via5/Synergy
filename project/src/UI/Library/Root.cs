using System.Collections.Generic;
using UnityEngine;

namespace Synergy.UI
{
	class Test
	{
		private Root root_ = new Root();

		public Test()
		{
			root_.Layout = new BorderLayout();

			var top = new Widget();
			top.Layout = new HorizontalFlow(20);
			top.Add(new Button("button"));
			top.Add(new ComboBox(new List<string>{ "a", "b" }));
			top.Add(new Label("label"));

			var bottom = new Widget();
			bottom.Layout = new HorizontalFlow(20);
			bottom.Add(new Button("button"));
			bottom.Add(new ComboBox(new List<string> { "a", "b" }));
			bottom.Add(new Label("label"));

			root_.Add(top, BorderLayout.Top);
			//root_.Add(bottom, BorderLayout.Bottom);

			//
			//var cb = new ComboBox();
			//cb.AddItem("Step 1");
			//cb.AddItem("Step 2");
			//cb.AddItem("Step 3");
			//
			//root_.Add(new Label("Step:"));
			//root_.Add(cb);

			//root_.Layout = new BorderLayout();
			//root_.Add(new Label("left"), BorderLayout.Left);
			//root_.Add(new Label("top"), BorderLayout.Top);
			//root_.Add(new Label("right"), BorderLayout.Right);
			//root_.Add(new Label("bottom"), BorderLayout.Bottom);
			//root_.Add(new Label("center"), BorderLayout.Center);

			root_.DoLayout();
			root_.Create();

			root_.Dump();
		}
	}


	class Root : Widget
	{
		static public Transform PluginParent = null;
		static public Font DefaultFont =
			(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
		static public int DefaultFontSize = 28;

		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_;

		public Root()
		{
			Bounds = Rectangle.FromPoints(2, -27, 1077, 1198);

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
