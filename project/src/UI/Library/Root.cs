﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Root : Widget
	{
		public override string TypeName { get { return "root"; } }

		static public Transform PluginParent = null;
		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_;

		static public UIPopup openedPopup_ = null;
		static private Widget focused_ = null;

		static public void SetOpenedPopup(UIPopup p)
		{
			openedPopup_ = p;
		}

		static public void SetFocus(Widget w)
		{
			if (focused_ == w)
				return;

			focused_ = w;

			if (openedPopup_ != null)
			{
				if (openedPopup_.visible)
					openedPopup_.Toggle();

				openedPopup_ = null;
			}
		}


		private bool dirty_ = true;

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

			Style.PolishRoot(scriptui);
		}

		public void DoLayoutIfNeeded()
		{
			if (dirty_)
			{
				DoLayout();
				Create();
				dirty_ = false;
			}
		}

		public override void NeedsLayout()
		{
			dirty_ = true;
		}

		public static float TextLength(string s)
		{
			return tg_.GetPreferredWidth(s, ts_);
		}
	}
}
