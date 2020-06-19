using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Overlay : Widget
	{
		private GameObject go_ = null;
		private Image graphics_ = null;

		public Overlay(Rectangle b)
		{
			Bounds = b;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			go_ = new GameObject();
			go_.transform.parent = WidgetObject.transform;

			graphics_ = go_.AddComponent<Image>();
			graphics_.color = new Color(0, 0, 0, 0.9f);

			var rt = graphics_.rectTransform;
			rt.offsetMin = new Vector2(Bounds.Left, Bounds.Top);
			rt.offsetMax = new Vector2(Bounds.Right, Bounds.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				Bounds.Center.X, -Bounds.Center.Y);
		}
	}


	class Root : Widget
	{
		public override string TypeName { get { return "root"; } }

		static public Transform PluginParent = null;
		static private TextGenerator tg_ = null;
		static private TextGenerationSettings ts_;

		static public UIPopup openedPopup_ = null;
		static private Widget focused_ = null;
		static private Overlay overlay_ = null;

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
				UpdateBounds();

				dirty_ = false;
			}
		}

		public override void NeedsLayout()
		{
			dirty_ = true;
		}

		public override Root GetRoot()
		{
			return this;
		}

		public bool OverlayVisible
		{
			set
			{
				if (value)
					ShowOverlay();
				else
					HideOverlay();
			}
		}

		private void ShowOverlay()
		{
			if (overlay_ == null)
			{
				overlay_ = new Overlay(Bounds);
				overlay_.Create();
				overlay_.UpdateBounds();
			}

			overlay_.Visible = true;
			overlay_.DoLayout();
			overlay_.WidgetObject.transform.SetAsLastSibling();
		}

		private void HideOverlay()
		{
			if (overlay_ != null)
				overlay_.Visible = false;
		}

		public static float TextLength(string s)
		{
			return tg_.GetPreferredWidth(s, ts_);
		}
	}
}
