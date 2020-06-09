using UnityEngine;

namespace Synergy.UI
{
	class Test
	{
		private Root root_ = new Root();

		public Test()
		{
			root_.Layout = new BorderLayout();
			root_.Add(new Label2(), BorderLayout.Top);
			root_.Add(new Label2(), BorderLayout.Bottom);
			root_.Add(new Label2(), BorderLayout.Left);
			root_.Add(new Label2(), BorderLayout.Right);
			root_.Add(new Label2(), BorderLayout.Center);
			root_.DoLayout();
			root_.Create();
		}
	}


	class Root : Widget
	{
		static public Transform PluginParent = null;
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
