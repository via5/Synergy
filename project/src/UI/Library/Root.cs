using Leap.Unity;
using LeapInternal;
using System;
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
			Margins = new Insets(5);
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
		static private TextGenerator tg_ = new TextGenerator();
		static private TextGenerationSettings ts_ = new TextGenerationSettings();

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
		private Insets margins_ = new Insets(5);
		private RootPanel content_;
		private RootPanel floating_;
		private Overlay overlay_ = null;
		private readonly TooltipManager tooltips_;
		private float topOffset_ = 0;
		private bool dirty_ = true;
		private Canvas canvas_;

		public Root()
		{
			content_ = new RootPanel(this);
			floating_ = new RootPanel(this);
			tooltips_ = new TooltipManager(this);

			ts_.font = Style.Font;
			ts_.fontSize = Style.FontSize;

			var scriptUI = Synergy.Instance.UITransform.GetComponentInChildren<MVRScriptUI>();

			AttachTo(scriptUI);
			Style.PolishRoot(scriptUI);
		}

		public void AttachTo(MVRScriptUI scriptUI)
		{
			var scrollView = scriptUI.GetComponentInChildren<ScrollRect>();
			if (scrollView == null)
			{
				Synergy.LogError("no scrollrect in attach");
				return;
			}

			var scrollViewRT = scrollView.GetComponent<RectTransform>();
			topOffset_ = scrollViewRT.offsetMin.y - scrollViewRT.offsetMax.y;

			bounds_ = Rectangle.FromPoints(
				1, 1, scrollViewRT.rect.width - 3, scrollViewRT.rect.height - 3);
			content_.Bounds = new Rectangle(bounds_);
			floating_.Bounds = new Rectangle(bounds_);

			PluginParent = scriptUI.fullWidthUIContent;


			var image = scriptUI.GetComponentInChildren<Image>();
			if (image == null)
				Synergy.LogError("no image in attach");
			else
				canvas_ = image.canvas;

			var text = scriptUI.GetComponentInChildren<Text>();
			if (text == null)
			{
				Synergy.LogError("no text in attach");
			}
			else
			{
				tg_ = text.cachedTextGenerator;
				ts_ = text.GetGenerationSettings(new Vector2());
			}
		}

		public Panel ContentPanel
		{
			get { return content_; }
		}

		public Panel FloatingPanel
		{
			get { return floating_; }
		}

		public TooltipManager Tooltips
		{
			get { return tooltips_; }
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

		public Point MousePosition
		{
			get
			{
				var mp = Input.mousePosition;

				Vector2 pp;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					canvas_.transform as RectTransform, mp,
					canvas_.worldCamera, out pp);

				pp.x = bounds_.Left + bounds_.Width / 2 + pp.x;
				pp.y = bounds_.Top + (bounds_.Height - pp.y + topOffset_);

				return new Point(pp.x, pp.y);
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

		public static Size FitText(string s, float maxWidth)
		{
			var ts = ts_;

			ts.generationExtents = new Vector2(maxWidth, 0);
			ts.horizontalOverflow = HorizontalWrapMode.Wrap;
			ts.verticalOverflow = VerticalWrapMode.Overflow;
			ts.generateOutOfBounds = false;
			ts.resizeTextForBestFit = false;

			var w = tg_.GetPreferredWidth(s, ts);
			var h = tg_.GetPreferredHeight(s, ts);

			return new Size(Math.Min(w, maxWidth), h);
		}
	}
}
