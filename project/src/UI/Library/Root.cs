using System.Collections.Generic;
using UnityEngine;

namespace Synergy.UI
{
	class Root : Widget
	{
		static public Transform PluginParent = null;
		static public Font DefaultFont =
			(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
		static public int DefaultFontSize = 28;
		static public Color DefaultTextColor = new Color(0.84f, 0.84f, 0.84f);

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
				if (b == null) Synergy.LogError("8");
				tg_ = b.buttonText.cachedTextGenerator;
				ts_ = b.buttonText.GetGenerationSettings(new Vector2());
				//Utilities.DumpComponentsAndUp(b.gameObject);
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
			if (a == null) Synergy.LogError("7");
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
