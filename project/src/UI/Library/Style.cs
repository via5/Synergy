using System.Collections.Generic;
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

		public static Font DefaultFont
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

		public static int DefaultFontSize
		{
			get { return 28; }
		}


		public static Color TextColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public static Color DisabledTextColor
		{
			get { return new Color(0.6f, 0.6f, 0.6f); }
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

		public static Color SliderBackgroundColor
		{
			get { return new Color(0.20f, 0.20f, 0.20f); }
		}

		public static Color DisabledButtonBackgroundColor
		{
			get { return new Color(0.20f, 0.20f, 0.20f); }
		}

		public static Color HighlightBackgroundColor
		{
			get { return new Color(0.35f, 0.35f, 0.35f); }
		}

		public static Color SelectionBackgroundColor
		{
			get { return new Color(0.4f, 0.4f, 0.4f); }
		}

		public static int SliderTextSize
		{
			get { return 20; }
		}

		public static int ComboBoxNavTextSize
		{
			get { return 12; }
		}

		public static void ClampScrollView(GameObject scrollView)
		{
			var sr = scrollView.GetComponent<ScrollRect>();
			sr.movementType = ScrollRect.MovementType.Clamped;
		}


		public static void SetupRoot(Component scriptUI)
		{
			AdjustRoot(scriptUI);
			PolishRoot(scriptUI);
		}

		public static void AdjustRoot(Component scriptUI)
		{
			var scrollView = Utilities.FindChildRecursive(
				scriptUI, "Scroll View");

			if (scrollView == null)
			{
				Synergy.LogError("AdjustRoot: no scrollview");
			}
			else
			{
				// clamp the whole script ui
				ClampScrollView(scrollView);
			}
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
				// main background color
				scrollView.GetComponent<Image>().color = BackgroundColor;
			}
		}


		public static void Setup(ColorPicker e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(ColorPicker e)
		{
			var picker = e.WidgetObject.GetComponent<UIDynamicColorPicker>();

			var sliders = new List<UnityEngine.UI.Slider>()
			{
				picker.colorPicker.redSlider,
				picker.colorPicker.greenSlider,
				picker.colorPicker.blueSlider
			};

			foreach (var slider in sliders)
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider.transform.parent;

				var text = Utilities.FindChildRecursive(parent, "Text")
					?.GetComponent<Text>();

				if (text != null)
				{
					var rt = text.GetComponent<RectTransform>();
					rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);

					Adjust(text, e.Font, SliderTextSize);
				}

				var input = Utilities.FindChildRecursive(parent, "InputField")
					?.GetComponent<InputField>();

				if (input != null)
				{
					var rt = input.GetComponent<RectTransform>();
					rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
				}

				{
					var rt = slider.GetComponent<RectTransform>();
					rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y + 10);
					rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
				}

				// adjust the slider itself
				Adjust(slider);
			}

			{
				// moving all the sliders down to make space for the color
				// sample at the top

				// blue
				var rt = picker.colorPicker.blueSlider.transform.parent.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 10);
				rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y - 10);

				// green
				rt = picker.colorPicker.greenSlider.transform.parent.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 30);
				rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y - 30);

				// red
				rt = picker.colorPicker.redSlider.transform.parent.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 50);
				rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y - 50);

				// sample
				rt = picker.colorPicker.colorSample.GetComponent<RectTransform>();
				rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 50);
			}

			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				var b = Utilities.FindChildRecursive(picker, name)
					?.GetComponent<UnityEngine.UI.Button>();

				if (b != null)
					Adjust(b, e.Font, e.FontSize, false);
			}
		}

		public static void Polish(ColorPicker e)
		{
			var picker = e.WidgetObject.GetComponent<UIDynamicColorPicker>();

			// background
			picker.GetComponent<Image>().color = new Color(0, 0, 0, 0);

			// label on top
			picker.labelText.color = TextColor;
			picker.labelText.alignment = TextAnchor.MiddleLeft;

			var sliders = new List<UnityEngine.UI.Slider>()
			{
				picker.colorPicker.redSlider,
				picker.colorPicker.greenSlider,
				picker.colorPicker.blueSlider
			};

			foreach (var slider in sliders)
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider.transform.parent;

				var panel = Utilities.FindChildRecursive(parent, "Panel");
				if (panel != null)
				{
					panel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
				}

				var text = Utilities.FindChildRecursive(parent, "Text")
					?.GetComponent<Text>();

				if (text != null)
					Polish(text, e.Font, SliderTextSize);

				var input = Utilities.FindChildRecursive(parent, "InputField")
					?.GetComponent<InputField>();

				if (input != null)
				{
					// that input doesn't seem to get styled properly, can't
					// get the background color to change, so just change the
					// text color
					//Polish(input, font, fontSize, false);

					input.textComponent.color = TextColor;
					input.textComponent.fontSize = SliderTextSize;
				}

				// polish the slider itself
				Polish(slider);
			}

			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				var b = Utilities.FindChildRecursive(picker, name)
					?.GetComponent<UnityEngine.UI.Button>();

				if (b != null)
					Polish(b, e.Font, e.FontSize, false);
			}
		}


		public static void Setup(Slider e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(Slider e)
		{
			Adjust(e.WidgetObject.GetComponent<UIDynamicSlider>());
		}

		public static void Polish(Slider e)
		{
			Polish(e.WidgetObject.GetComponent<UIDynamicSlider>());
		}


		private static void Setup(UIDynamicSlider e)
		{
			Adjust(e);
			Polish(e);
		}

		private static void Adjust(UIDynamicSlider e)
		{
			Adjust(e.slider);
		}

		private static void Polish(UIDynamicSlider e)
		{
			Polish(e.slider);
		}


		private static void Setup(UnityEngine.UI.Slider e)
		{
			Adjust(e);
			Polish(e);
		}

		private static void Adjust(UnityEngine.UI.Slider e)
		{
			var fill = Utilities.FindChildRecursive(e, "Fill");
			var rt = fill.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 4, rt.offsetMin.y);
		}

		private static void Polish(UnityEngine.UI.Slider e)
		{
			// slider background color
			e.GetComponent<UnityEngine.UI.Image>().color = SliderBackgroundColor;

			var ss = e.GetComponent<UIStyleSlider>();
			ss.normalColor = ButtonBackgroundColor;
			ss.highlightedColor = HighlightBackgroundColor;
			ss.pressedColor = HighlightBackgroundColor;
			ss.UpdateStyle();

			var fill = Utilities.FindChildRecursive(e, "Fill");
			fill.GetComponent<Image>().color = new Color(0, 0, 0, 0);
		}


		public static void Setup(CheckBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(CheckBox e)
		{
			var toggle = e.WidgetObject.GetComponent<UIDynamicToggle>();
			Adjust(toggle);
		}

		public static void Polish(CheckBox e)
		{
			var toggle = e.WidgetObject.GetComponent<UIDynamicToggle>();
			Polish(toggle);
		}


		public static void Setup<ItemType>(TypedListImpl<ItemType> list)
			where ItemType : class
		{
			Adjust(list);
			Polish(list);
		}

		public static void Adjust<ItemType>(TypedListImpl<ItemType> list)
			where ItemType : class
		{
			Adjust(
				list.WidgetObject.GetComponent<UIDynamicPopup>(),
				list.Font, list.FontSize);
		}

		public static void Polish<ItemType>(TypedListImpl<ItemType> list)
			where ItemType : class
		{
			Polish(
				list.WidgetObject.GetComponent<UIDynamicPopup>(),
				list.Font, list.FontSize);
		}


		public static void Setup(Button b)
		{
			Adjust(b);
			Polish(b);
		}

		public static void Adjust(Button b)
		{
			Adjust(
				b.WidgetObject.GetComponent<UIDynamicButton>(),
				b.Font, b.FontSize);
		}

		public static void Polish(Button b)
		{
			Polish(
				b.WidgetObject.GetComponent<UIDynamicButton>(),
				b.Font, b.FontSize);
		}


		public static void Setup(Label e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(Label e)
		{
			var text = e.WidgetObject.GetComponent<Text>();
			Adjust(text, e.Font, e.FontSize);
		}

		public static void Polish(Label e)
		{
			var text = e.WidgetObject.GetComponent<Text>();
			Polish(text, e.Font, e.FontSize);
		}


		public static void Setup(TextBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(TextBox e)
		{
			var input = e.WidgetObject.GetComponentInChildren<InputField>();
			if (input != null)
				Adjust(input, e.Font, e.FontSize);
		}

		public static void Polish(TextBox e)
		{
			var input = e.WidgetObject.GetComponentInChildren<InputField>();
			if (input != null)
				Polish(input, e.Font, e.FontSize);
		}


		private static void Adjust(UIDynamicToggle e)
		{
			// no-op
		}

		private static void Polish(UIDynamicToggle e)
		{
			// background color of the whole widget
			e.backgroundImage.color = new Color(0, 0, 0, 0);

			// color of the text on the toggle
			e.textColor = TextColor;

			// there doesn't seem to be any way to change the checkmark color,
			// so the box will have to stay white for now
		}

		private static void Adjust(
			UnityEngine.UI.Button e, Font font, int fontSize,
			bool changeFont = true)
		{
			// no-op
		}

		private static void Polish(
			UnityEngine.UI.Button e, Font font, int fontSize,
			bool changeFont = true)
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

				if (changeFont)
					st.fontSize = (fontSize < 0 ? DefaultFontSize : fontSize);

				st.UpdateStyle();
			}

			var sb = e.GetComponent<UIStyleButton>();
			sb.normalColor = ButtonBackgroundColor;
			sb.highlightedColor = HighlightBackgroundColor;
			sb.pressedColor = HighlightBackgroundColor;
			sb.disabledColor = DisabledButtonBackgroundColor;
			sb.UpdateStyle();
		}


		private static void Adjust(UIDynamicButton e, Font font, int fontSize)
		{
			Adjust(e.button, font, fontSize);
		}

		private static void Polish(UIDynamicButton e, Font font, int fontSize)
		{
			Polish(e.button, font, fontSize);

			e.buttonText.font = font ?? DefaultFont;
			e.buttonText.fontSize = (fontSize < 0 ? DefaultFontSize : fontSize);
		}


		private static void Adjust(UIDynamicPopup e, Font font, int fontSize)
		{
			// popups normally have a label on the left side and this controls
			// the offset of the popup button; since the label is removed, this
			// must be 0 so the popup button is left aligned
			e.labelWidth = 0;

			// the top and bottom padding in the list, this looks roughly
			// equivalent to what's on the left and right
			e.popup.topBottomBuffer = 3;
		}

		private static void Polish(UIDynamicPopup e, Font font, int fontSize)
		{
			Polish(e.popup, font, fontSize);
		}


		private static void Adjust(
			Text e, Font font, int fontSize, bool changeFont = true)
		{
			// no-op
		}

		private static void Polish(
			Text e, Font font, int fontSize, bool changeFont=true)
		{
			e.color = Style.TextColor;

			if (changeFont)
			{
				e.fontSize = (fontSize < 0 ? DefaultFontSize : fontSize);
				e.font = font ?? DefaultFont;
			}
		}


		private static void Adjust(
			InputField input, Font font, int fontSize, bool changeFont = true)
		{
			// field
			input.caretWidth = 2;
		}

		private static void Polish(
			InputField input, Font font, int fontSize, bool changeFont=true)
		{
			// textbox background
			var bg = input.GetComponentInChildren<Image>();
			if (bg != null)
				bg.color = EditableBackgroundColor;

			// textbox text
			var text = input.textComponent;//.GetComponentInChildren<Text>();
			if (text != null)
			{
				//text.alignment = TextAnchor.MiddleLeft;
				text.color = EditableTextColor;

				if (changeFont)
				{
					text.fontSize = (fontSize < 0 ? DefaultFontSize : fontSize);
					text.font = font ?? DefaultFont;
				}
			}

			// field
			input.selectionColor = EditableSelectionBackgroundColor;

			// placeholder
			var ph = input.placeholder.GetComponent<Text>();
			ph.color = PlaceholderTextColor;

			if (changeFont)
			{
				ph.font = font ?? DefaultFont;
				ph.fontSize = (fontSize < 0 ? DefaultFontSize : fontSize);
				ph.fontStyle = FontStyle.Italic;
			}
		}


		private static void Adjust(UIPopup e, Font font, int fontSize)
		{
			var scrollView = Utilities.FindChildRecursive(e, "Scroll View");
			var viewport = Utilities.FindChildRecursive(e, "Viewport");
			var scrollbar = Utilities.FindChildRecursive(e, "Scrollbar Vertical");
			var scrollbarHandle = Utilities.FindChildRecursive(scrollbar, "Handle");

			ClampScrollView(scrollView);

			// topButton is the actual combobox the user clicks to open the
			// popup
			Adjust(e.topButton, font, fontSize);

			// popupButtonPrefab is the prefab used to create items in the
			// popup
			Adjust(
				e.popupButtonPrefab.GetComponent<UnityEngine.UI.Button>(),
				font, fontSize);

			// there's some empty space at the bottom of the list, remove it
			// by changing the bottom offset of both the viewport and vertical
			// scrollbar; the scrollbar is also one pixel too far to the right
			var rt = viewport.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			rt = scrollbar.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 1, 0);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 1, rt.offsetMax.y);
		}

		private static void Polish(UIPopup e, Font font, int fontSize)
		{
			var scrollView = Utilities.FindChildRecursive(e, "Scroll View");
			var viewport = Utilities.FindChildRecursive(e, "Viewport");
			var scrollbar = Utilities.FindChildRecursive(e, "Scrollbar Vertical");
			var scrollbarHandle = Utilities.FindChildRecursive(scrollbar, "Handle");

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
			Polish(e.topButton, font, fontSize);

			// popupButtonPrefab is the prefab used to create items in the
			// popup
			Polish(
				e.popupButtonPrefab.GetComponent<UnityEngine.UI.Button>(),
				font, fontSize);

			// scrollbar background color
			scrollbar.GetComponent<Image>().color = SliderBackgroundColor;

			// scrollbar handle color
			scrollbarHandle.GetComponent<Image>().color = ButtonBackgroundColor;
		}
	}
}
