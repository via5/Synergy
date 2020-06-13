using System.Collections.Generic;
using UnityEngine;

namespace Synergy.UI
{
	class Root : Widget
	{
		public override string TypeName { get { return "root"; } }

		static public Transform PluginParent = null;
		static public Font DefaultFont =
			(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
		static public int DefaultFontSize = 28;
		static public Color DefaultTextColor = new Color(0.84f, 0.84f, 0.84f);

		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_;

		static public UIPopup OpenedPopup = null;
		static private Widget focused_ = null;

		static public void SetFocus(Widget w)
		{
			if (focused_ == w)
				return;

			focused_ = w;

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

			{
				var b = Synergy.Instance.CreateButton("b");
				tg_ = b.buttonText.cachedTextGenerator;
				ts_ = b.buttonText.GetGenerationSettings(new Vector2());
				PluginParent = b.transform.parent;
				Synergy.Instance.RemoveButton(b);
			}

			var content = PluginParent.parent;
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
	}
}
