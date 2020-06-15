using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Style
	{
		private static Font font_ = null;

		public static Font Font
		{
			get
			{
				if (font_ == null)
				{
					font_ = (Font)Resources.GetBuiltinResource(
						typeof(Font), "Arial.ttf");
				}

				return font_;
			}
		}

		public static Color TextColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public static Color InverseTextColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public static Color BackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public static Color ButtonBackgroundColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}

		public static Color HighlightBackgroundColor
		{
			get { return new Color(0.35f, 0.35f, 0.35f); }
		}

		public static Color SelectionBackgroundColor
		{
			get { return new Color(0.4f, 0.4f, 0.4f); }
		}

		public static int FontSize
		{
			get { return 28; }
		}


		private static void PolishButton(Component e)
		{
			var i = e.GetComponent<Image>();
			i.color = Color.white;

			var st = e.GetComponentInChildren<UIStyleText>();
			if (st != null)
			{
				st.color = TextColor;
				st.UpdateStyle();
			}

			var sb = e.GetComponent<UIStyleButton>();
			sb.normalColor = ButtonBackgroundColor;
			sb.highlightedColor = HighlightBackgroundColor;
			sb.pressedColor = HighlightBackgroundColor;
			sb.UpdateStyle();
		}

		public static void ClampScrollView(GameObject scrollView)
		{
			var sr = scrollView.GetComponent<ScrollRect>();
			sr.movementType = ScrollRect.MovementType.Clamped;
		}

		public static void Polish(UIDynamicButton e)
		{
			PolishButton(e);
		}

		public static void Polish(UIDynamicPopup e)
		{
			e.labelWidth = 0;
			e.labelSpacingRight = 0;
			e.popup.topBottomBuffer = 3;

			Polish(e.popup);
		}

		public static void Polish(UIPopup e)
		{
			var scrollView = Utilities.FindChildRecursive(e, "Scroll View");
			var viewport = Utilities.FindChildRecursive(e, "Viewport");
			var scrollbar = Utilities.FindChildRecursive(e, "Scrollbar Vertical");
			var scrollbarHandle = Utilities.FindChildRecursive(scrollbar, "Handle");

			ClampScrollView(scrollView);

			// background color for items in the popup; to have items be the
			// same color as the background of the popup, this must be
			// transparent instead of BackgroundColor because darker borders
			// would be added automatically and they can't be configured
			e.normalColor = new Color(0, 0, 0, 0);

			// background color for a selected item inside the popup
			e.selectColor = SelectionBackgroundColor;

			// background color of the popup, behind the items
			e.popupPanel.GetComponent<Image>().color = BackgroundColor;

			// background color of the scroll view inside the popup; this must
			// be transparent for the background color set above to appear
			// correctly
			scrollView.GetComponent<Image>().color = new Color(0, 0, 0, 0);

			// topButton is the actual combobox the user clicks to open the
			// popup
			PolishButton(e.topButton);

			// popupButtonPrefab is the prefab used to create items in the
			// popup
			PolishButton(e.popupButtonPrefab);

			// there's some empty space at the bottom of the list, remove it
			// by changing the bottom offset of both the viewport and vertical
			// scrollbar
			var rt = viewport.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			rt = scrollbar.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			// scrollbar background color
			scrollbar.GetComponent<Image>().color = BackgroundColor;

			// scrollbar handle color
			scrollbarHandle.GetComponent<Image>().color = ButtonBackgroundColor;
		}
	}
}
