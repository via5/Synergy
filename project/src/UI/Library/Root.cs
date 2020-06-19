using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Synergy.UI
{
	class ClickEater : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
	{
		public void OnPointerClick(PointerEventData data)
		{
		}

		public void OnPointerDown(PointerEventData data)
		{
		}

		public void OnPointerUp(PointerEventData data)
		{
		}
	}

	class Overlay : Widget
	{
		private Image graphics_ = null;

		public Overlay(Rectangle b)
		{
			Bounds = b;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			graphics_ = MainObject.AddComponent<Image>();
			graphics_.color = new Color(0, 0, 0, 0.9f);
			graphics_.raycastTarget = true;

			MainObject.AddComponent<ClickEater>();
		}
	}


	class RootPanel : Panel
	{
		private readonly Root root_;

		public RootPanel(Root r)
		{
			root_ = r;
		}

		public override void NeedsLayout()
		{
			root_.SetDirty();
		}

		public override Root GetRoot()
		{
			return root_;
		}
	}


	class Root
	{
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


		private Rectangle bounds_;
		private Insets margins_;
		private RootPanel content_;
		private RootPanel floating_;
		private Overlay overlay_ = null;
		private bool dirty_ = true;

		public Root()
		{
			bounds_ = Rectangle.FromPoints(2, 1, 1078, 1228);
			margins_ = new Insets(5);

			content_ = new RootPanel(this);
			content_.Bounds = new Rectangle(bounds_);

			floating_ = new RootPanel(this);
			floating_.Bounds = new Rectangle(bounds_);

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

		public Panel ContentPanel
		{
			get { return content_; }
		}

		public Rectangle Bounds
		{
			get { return bounds_; }
		}

		public void DoLayoutIfNeeded()
		{
			if (dirty_)
			{
				content_.DoLayout();
				content_.Create();
				content_.UpdateBounds();

				floating_.DoLayout();
				floating_.Create();
				floating_.UpdateBounds();

				dirty_ = false;
			}
		}

		public void SetDirty()
		{
			dirty_ = true;
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
				overlay_ = new Overlay(bounds_);
				floating_.Add(overlay_);
				overlay_.Create();
				overlay_.UpdateBounds();
			}

			overlay_.Visible = true;
			overlay_.DoLayout();
			overlay_.MainObject.transform.SetAsLastSibling();
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
