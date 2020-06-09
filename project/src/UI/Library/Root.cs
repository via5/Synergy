using System.Security.Permissions;
using UnityEngine;

namespace Synergy.UI
{
	class Test
	{
		private Root root_ = new Root();

		public Test()
		{
			//root_.Layout = new HorizontalFlow();
			//root_.Add(new Label());
			//root_.Add(new Label());
			//root_.Add(new LabelLabel());

			root_.Layout = new BorderLayout();
			root_.Add(new Label("left"), BorderLayout.Left);
			root_.Add(new Label("top"), BorderLayout.Top);
			root_.Add(new Label("right"), BorderLayout.Right);
			root_.Add(new Label("bottom"), BorderLayout.Bottom);
			root_.Add(new Label("center"), BorderLayout.Center);

			root_.DoLayout();
			root_.Create();
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
			var viewport = content.parent;
			var scrollview = viewport.parent;
			var scriptui = scrollview.parent;

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
