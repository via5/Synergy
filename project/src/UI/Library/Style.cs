using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Metrics
	{
		public static float CursorHeight
		{
			get { return 45; }
		}

		public static float TooltipDelay
		{
			get { return 0.5f; }
		}

		public static float MaxTooltipWidth
		{
			get { return 400; }
		}

		public static float TooltipBorderOffset
		{
			get { return 10; }
		}
	}


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

		public static Color DisabledTextColor
		{
			get { return new Color(0.7f, 0.7f, 0.7f); }
		}

		public static Color EditableTextColor
		{
			get { return Color.black; }
		}

		public static Color PlaceholderTextColor
		{
			get { return new Color(0.5f, 0.5f, 0.5f); }
		}

		public static Color EditableBackgroundColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public static Color EditableSelectionBackgroundColor
		{
			get { return new Color(0.6f, 0.6f, 0.6f); }
		}

		public static Color BackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public static Color ButtonBackgroundColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}

		public static Color DisabledButtonBackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
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


		public static void ClampScrollView(GameObject scrollView)
		{
			var sr = scrollView.GetComponent<ScrollRect>();
			sr.movementType = ScrollRect.MovementType.Clamped;
		}

		public static void PolishRoot(Component scriptUI)
		{
			var scrollView = Utilities.FindChildRecursive(
				scriptUI, "Scroll View");

			if (scrollView == null)
			{
				Synergy.LogError("PolishRoot: no scrollview");
			}
			else
			{
				// clamp the whole script ui
				ClampScrollView(scrollView);

				// main background color
				scrollView.GetComponent<Image>().color = BackgroundColor;
			}
		}

		public static void Polish(UIDynamicToggle e)
		{
			// background color of the whole widget
			e.backgroundImage.color = new Color(0, 0, 0, 0);

			// color of the text on the toggle
			e.textColor = TextColor;

			// there doesn't seem to be any way to change the checkmark color,
			// so the box will have to stay white for now
		}

		public static void Polish(UnityEngine.UI.Button e)
		{
			var i = e.GetComponent<Image>();
			i.color = Color.white;

			var st = e.GetComponentInChildren<UIStyleText>();
			if (st != null)
			{
				if (e.interactable)
					st.color = TextColor;
				else
					st.color = DisabledTextColor;

				st.UpdateStyle();
			}

			var sb = e.GetComponent<UIStyleButton>();
			sb.normalColor = ButtonBackgroundColor;
			sb.highlightedColor = HighlightBackgroundColor;
			sb.pressedColor = HighlightBackgroundColor;
			sb.UpdateStyle();
		}

		public static void Polish(UIDynamicButton e)
		{
			Polish(e.button);
		}

		public static void Polish(UIDynamicPopup e)
		{
			// popups normally have a label on the left side and this controls
			// the offset of the popup button; since the label is removed, this
			// must be 0 so the popup button is left aligned
			e.labelWidth = 0;

			// the top and bottom padding in the list, this looks roughly
			// equivalent to what's on the left and right
			e.popup.topBottomBuffer = 3;

			Polish(e.popup);
		}

		public static void Polish(TextBox e)
		{
			// textbox background
			var bg = e.WidgetObject.GetComponentInChildren<Image>();
			if (bg != null)
				bg.color = EditableBackgroundColor;

			// textbox text
			var text = e.WidgetObject.GetComponentInChildren<Text>();
			if (text != null)
			{
				text.alignment = TextAnchor.MiddleLeft;
				text.color = EditableTextColor;
				text.fontSize = Style.FontSize;
				text.font = Style.Font;
			}

			// field
			var input = e.WidgetObject.GetComponentInChildren<InputField>();
			if (input != null)
			{
				input.selectionColor = EditableSelectionBackgroundColor;
				input.caretWidth = 2;
			}

			// placeholder
			var ph = input.placeholder.GetComponent<Text>();
			ph.color = PlaceholderTextColor;
			ph.font = Font;
			ph.fontSize = FontSize;
			ph.fontStyle = FontStyle.Italic;
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
			Polish(e.topButton);

			// popupButtonPrefab is the prefab used to create items in the
			// popup
			Polish(e.popupButtonPrefab.GetComponent<UnityEngine.UI.Button>());

			// there's some empty space at the bottom of the list, remove it
			// by changing the bottom offset of both the viewport and vertical
			// scrollbar; the scrollbar is also one pixel too far to the right
			var rt = viewport.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			rt = scrollbar.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 1, 0);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 1, rt.offsetMax.y);

			// scrollbar background color
			scrollbar.GetComponent<Image>().color = BackgroundColor;

			// scrollbar handle color
			scrollbarHandle.GetComponent<Image>().color = ButtonBackgroundColor;
		}
	}
}
