﻿using System;
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


	class Theme
	{
		Font font_ = null;

		public Font DefaultFont
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

		public int DefaultFontSize
		{
			get { return 28; }
		}


		public Color TextColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public Color DisabledTextColor
		{
			get { return new Color(0.3f, 0.3f, 0.3f); }
		}

		public Color EditableTextColor
		{
			get { return Color.black; }
		}

		public Color DisabledEditableTextColor
		{
			get { return Color.black; }
		}

		public Color PlaceholderTextColor
		{
			get { return new Color(0.5f, 0.5f, 0.5f); }
		}

		public Color DisabledPlaceholderTextColor
		{
			get { return new Color(0.3f, 0.3f, 0.3f); }
		}

		public Color EditableBackgroundColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public Color DisabledEditableBackgroundColor
		{
			get { return new Color(0.60f, 0.60f, 0.60f); }
		}

		public Color EditableSelectionBackgroundColor
		{
			get { return new Color(0.6f, 0.6f, 0.6f); }
		}

		public Color BackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public Color ButtonBackgroundColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}

		public Color SliderBackgroundColor
		{
			get { return new Color(0.20f, 0.20f, 0.20f); }
		}

		public Color DisabledButtonBackgroundColor
		{
			get { return new Color(0.20f, 0.20f, 0.20f); }
		}

		public Color HighlightBackgroundColor
		{
			get { return new Color(0.35f, 0.35f, 0.35f); }
		}

		public Color SelectionBackgroundColor
		{
			get { return new Color(0.4f, 0.4f, 0.4f); }
		}

		public int SliderTextSize
		{
			get { return 20; }
		}

		public int ComboBoxNavTextSize
		{
			get { return 12; }
		}
	}


	class Style
	{
		private static Theme theme_ = new Theme();

		private class Info
		{
			private bool setFont_ = true;
			private bool enabled_ = true;
			private Font font_ = null;
			private int fontSize_ = -1;

			public Info(Widget w, bool setFont = true)
				: this(setFont, w.Enabled, w.Font, w.FontSize)
			{
			}

			public Info(bool setFont, bool enabled, Font font, int fontSize)
			{
				setFont_ = setFont;
				enabled_ = enabled;
				font_ = font;
				fontSize_ = fontSize;
			}

			public bool SetFont
			{
				get { return setFont_; }
			}

			public bool Enabled
			{
				get { return enabled_; }
			}

			public Font Font
			{
				get { return font_ ?? theme_.DefaultFont; }
			}

			public int FontSize
			{
				get { return fontSize_ < 0 ? theme_.DefaultFontSize : fontSize_; }
			}

			public Info WithFontSize(int size)
			{
				return new Info(setFont_, enabled_, font_, size);
			}

			public Info WithSetFont(bool b)
			{
				return new Info(b, enabled_, font_, fontSize_);
			}
		}


		static public Theme Theme
		{
			get { return theme_; }
		}


		private static void ForComponent<T>(Component o, Action<T> f)
		{
			if (o == null)
			{
				Synergy.LogError("ForComponent null");
				return;
			}

			ForComponent<T>(o.gameObject, f);
		}

		private static void ForComponent<T>(GameObject o, Action<T> f)
		{
			if (o == null)
			{
				Synergy.LogError("ForComponent null");
				return;
			}

			var c = o.GetComponent<T>();

			if (c == null)
			{
				Synergy.LogError(
					"component " + typeof(T).ToString() + " not found " +
					"in " + o.name);

				return;
			}

			f(c);
		}

		private static void ForComponentInChildren<T>(Component o, Action<T> f)
		{
			if (o == null)
			{
				Synergy.LogError("ForComponentInChildren null");
				return;
			}

			ForComponentInChildren<T>(o.gameObject, f);
		}

		private static void ForComponentInChildren<T>(GameObject o, Action<T> f)
		{
			if (o == null)
			{
				Synergy.LogError("ForComponentInChildren null");
				return;
			}

			var c = o.GetComponentInChildren<T>();

			if (c == null)
			{
				Synergy.LogError(
					"component " + typeof(T).ToString() + " not found in " +
					"children of " + o.name);

				return;
			}

			f(c);
		}

		private static void ForChildRecursive(Component parent, string name, Action<GameObject> f)
		{
			if (parent == null)
			{
				Synergy.LogError("ForChildRecursive null");
				return;
			}

			var c = Utilities.FindChildRecursive(parent, name);

			if (c == null)
			{
				Synergy.LogError(
					"child " + name + " not found in " + parent.name);

				return;
			}

			f(c);
		}

		private static GameObject RequireChildRecursive(Component parent, string name)
		{
			if (parent == null)
				throw new Exception("RequireChildRecursive parent null");

			return RequireChildRecursive(parent.gameObject, name);
		}

		private static GameObject RequireChildRecursive(GameObject parent, string name)
		{
			if (parent == null)
				throw new Exception("RequireChildRecursive parent null");

			var child = Utilities.FindChildRecursive(parent, name);
			if (child == null)
				throw new Exception("child " + name + " not found in " + parent.name);

			return child;
		}


		public static void ClampScrollView(GameObject scrollView)
		{
			ForComponent<ScrollRect>(scrollView, (sr) =>
			{
				sr.movementType = ScrollRect.MovementType.Clamped;
			});
		}


		public static void SetupRoot(Component scriptUI)
		{
			AdjustRoot(scriptUI);
			PolishRoot(scriptUI);
		}

		public static void AdjustRoot(Component scriptUI)
		{
			ForChildRecursive(scriptUI, "Scroll View", (scrollView) =>
			{
				// clamp the whole script ui
				ClampScrollView(scrollView);
			});
		}

		public static void PolishRoot(Component scriptUI)
		{
			ForChildRecursive(scriptUI, "Scroll View", (scrollView) =>
			{
				// main background color
				scrollView.GetComponent<Image>().color = theme_.BackgroundColor;
			});
		}


		public static void Setup(ColorPicker e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(ColorPicker e)
		{
			ForComponent<UIDynamicColorPicker>(e.WidgetObject, (picker) =>
			{
				Adjust(picker, new Info(e));
			});
		}

		public static void Polish(ColorPicker e)
		{
			ForComponent<UIDynamicColorPicker>(e.WidgetObject, (picker) =>
			{
				Polish(picker, new Info(e));
			});
		}


		public static void Setup(Slider e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(Slider e)
		{
			ForComponent<UIDynamicSlider>(e.WidgetObject, (slider) =>
			{
				Adjust(slider, new Info(e));
			});
		}

		public static void Polish(Slider e)
		{
			ForComponent<UIDynamicSlider>(e.WidgetObject, (slider) =>
			{
				Polish(slider, new Info(e));
			});
		}


		public static void Setup(CheckBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(CheckBox e)
		{
			ForComponent<UIDynamicToggle>(e.WidgetObject, (toggle) =>
			{
				Adjust(toggle, new Info(e));
			});
		}

		public static void Polish(CheckBox e)
		{
			ForComponent<UIDynamicToggle>(e.WidgetObject, (toggle) =>
			{
				Polish(toggle, new Info(e));
			});
		}


		public static void Setup<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			Adjust(cb);
			Polish(cb);
		}

		public static void Adjust<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(cb.WidgetObject, (popup) =>
			{
				Adjust(popup, new Info(cb));
			});


			var labelTextParent = cb.Popup?.popup?.labelText?.transform?.parent;

			if (labelTextParent == null)
			{
				Synergy.LogError("ComboBox has no labelText parent");
			}
			else
			{
				ForComponent<RectTransform>(labelTextParent.gameObject, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
					rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y);
					rt.anchorMin = new Vector2(0, 1);
					rt.anchorMax = new Vector2(0, 1);
					rt.anchoredPosition = new Vector2(
						rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
						rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
				});
			}


			if (cb.Popup?.popup?.topButton == null)
			{
				Synergy.LogError("ComboBox has no topButton");
			}
			else
			{
				// topButton is the actual combobox the user clicks to open the
				// popup

				// make it take the exact size as the parent, it normally has
				// an offset all around it
				ForComponent<RectTransform>(cb.Popup.popup.topButton.gameObject, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 12, rt.offsetMin.y - 6);
					rt.offsetMax = new Vector2(rt.offsetMax.x + 8, rt.offsetMax.y + 6);
					rt.anchoredPosition = new Vector2(
						rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
						rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
				});

				ForComponentInChildren<Text>(cb.Popup.popup.topButton, (text) =>
				{
					// avoid overlap with arrow
					text.rectTransform.offsetMax = new Vector2(
						text.rectTransform.offsetMax.x - 25,
						text.rectTransform.offsetMax.y);
				});
			}


			if (cb.Popup.popup.popupPanel == null)
			{
				Synergy.LogError("ComboBox has no popupPanel");
			}
			else
			{
				var rt = cb.Popup.popup.popupPanel;
				rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);
				rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 5);
			}
		}

		public static void Polish<ItemType>(ComboBoxList<ItemType> cb)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(cb.WidgetObject, (popup) =>
			{
				Polish(popup, new Info(cb));
			});

			ForComponent<Text>(cb.Arrow, (text) =>
			{
				if (cb.Enabled)
					text.color = theme_.TextColor;
				else
					text.color = theme_.DisabledTextColor;

				text.fontSize = (cb.FontSize > 0 ? cb.FontSize :theme_.DefaultFontSize);
				text.font = cb.Font ? cb.Font : theme_.DefaultFont;
			});
		}


		public static void Setup<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			Adjust(list);
			Polish(list);
		}

		public static void Adjust<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(list.WidgetObject, (popup) =>
			{
				Adjust(popup, new Info(list));
			});
		}

		public static void Polish<ItemType>(ListView<ItemType> list)
			where ItemType : class
		{
			ForComponent<UIDynamicPopup>(list.WidgetObject, (popup) =>
			{
				Polish(popup, new Info(list));
			});
		}

		public static void Setup(Button b)
		{
			Adjust(b);
			Polish(b);
		}

		public static void Adjust(Button b)
		{
			ForComponent<UIDynamicButton>(b.WidgetObject, (button) =>
			{
				Adjust(button, new Info(b));
			});
		}

		public static void Polish(Button b)
		{
			ForComponent<UIDynamicButton>(b.WidgetObject, (button) =>
			{
				Polish(button, new Info(b));
			});
		}


		public static void Setup(Label e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(Label e)
		{
			ForComponent<Text>(e.WidgetObject, (text) =>
			{
				Adjust(text, new Info(e));
			});
		}

		public static void Polish(Label e)
		{
			ForComponent<Text>(e.WidgetObject, (text) =>
			{
				Polish(text, new Info(e));
			});
		}


		public static void Setup(TextBox e)
		{
			Adjust(e);
			Polish(e);
		}

		public static void Adjust(TextBox e)
		{
			var input = e.InputField;
			if (input == null)
				Synergy.LogError("TextBox has no InputField");
			else
				Adjust(input, new Info(e));
		}

		public static void Polish(TextBox e)
		{
			// textbox background
			ForComponentInChildren<Image>(e.WidgetObject, (bg) =>
			{
				if (e.Enabled)
					bg.color = theme_.EditableBackgroundColor;
				else
					bg.color = theme_.DisabledEditableBackgroundColor;
			});

			var input = e.InputField;
			if (input == null)
				Synergy.LogError("TextBox has no InputField");
			else
				Polish(input, new Info(e));
		}


		private static void Adjust(UIDynamicToggle e, Info info)
		{
			// no-op
		}

		private static void Polish(UIDynamicToggle e, Info info)
		{
			// background color of the whole widget
			e.backgroundImage.color = new Color(0, 0, 0, 0);

			// color of the text on the toggle
			e.textColor = theme_.TextColor;

			// there doesn't seem to be any way to change the checkmark color,
			// so the box will have to stay white for now
		}

		private static void Adjust(UnityEngine.UI.Button e, Info info)
		{
			// no-op
		}

		private static void Polish(UnityEngine.UI.Button e, Info info)
		{
			ForComponent<Image>(e, (i) =>
			{
				i.color = Color.white;
			});

			ForComponentInChildren<UIStyleText>(e, (st) =>
			{
				if (info.Enabled)
					st.color = theme_.TextColor;
				else
					st.color = theme_.DisabledTextColor;

				if (info.SetFont)
					st.fontSize = info.FontSize;

				st.UpdateStyle();
			});

			ForComponent<UIStyleButton>(e, (sb) =>
			{
				sb.normalColor = theme_.ButtonBackgroundColor;
				sb.highlightedColor = theme_.HighlightBackgroundColor;
				sb.pressedColor = theme_.HighlightBackgroundColor;
				sb.disabledColor = theme_.DisabledButtonBackgroundColor;
				sb.UpdateStyle();
			});
		}


		private static void Adjust(UIDynamicButton e, Info info)
		{
			Adjust(e.button, info);
		}

		private static void Polish(UIDynamicButton e, Info info)
		{
			Polish(e.button, info);

			if (e.buttonText == null)
			{
				Synergy.LogError("UIDynamicButton has no buttonText");
			}
			else
			{
				e.buttonText.font = info.Font;
				e.buttonText.fontSize = info.FontSize;
			}
		}


		private static void Adjust(UIDynamicPopup e, Info info)
		{
			// popups normally have a label on the left side and this controls
			// the offset of the popup button; since the label is removed, this
			// must be 0 so the popup button is left aligned
			e.labelWidth = 0;

			if (e.popup == null)
			{
				Synergy.LogError("UIDynamicPopup has no popup");
			}
			else
			{
				// the top and bottom padding in the list, this looks roughly
				// equivalent to what's on the left and right
				e.popup.topBottomBuffer = 3;

				Adjust(e.popup, info);
			}
		}

		private static void Polish(UIDynamicPopup e, Info info)
		{
			if (e.popup == null)
				Synergy.LogError("UIDynamicPopup has no popup");
			else
				Polish(e.popup, info);
		}


		private static void Adjust(Text e, Info info)
		{
			// no-op
		}

		private static void Polish(Text e, Info info)
		{
			e.color = theme_.TextColor;

			if (info.SetFont)
			{
				e.fontSize = info.FontSize;
				e.font = info.Font;
			}
		}


		private static void Adjust(InputField input, Info info)
		{
			// field
			input.caretWidth = 2;
		}

		private static void Polish(InputField input, Info info)
		{
			// textbox text
			var text = input.textComponent;
			if (text == null)
			{
				Synergy.LogError("InputField has no textComponent");
			}
			else
			{
				if (info.Enabled)
					text.color = theme_.EditableTextColor;
				else
					text.color = theme_.DisabledEditableTextColor;

				if (info.SetFont)
				{
					text.fontSize = info.FontSize;
					text.font = info.Font;
				}
			}

			// field
			input.selectionColor = theme_.EditableSelectionBackgroundColor;

			// placeholder
			if (input.placeholder == null)
			{
				Synergy.LogError("InputField has no placeholder");
			}
			else
			{
				ForComponent<Text>(input.placeholder, (ph) =>
				{
					if (info.Enabled)
						ph.color = theme_.PlaceholderTextColor;
					else
						ph.color = theme_.DisabledPlaceholderTextColor;

					if (info.SetFont)
					{
						ph.font = info.Font;
						ph.fontSize = info.FontSize;
						ph.fontStyle = FontStyle.Italic;
					}
				});
			}
		}


		private static void Adjust(UIPopup e, Info info)
		{
			try
			{
				var scrollView = RequireChildRecursive(e, "Scroll View");
				var viewport = RequireChildRecursive(e, "Viewport");
				var scrollbar = RequireChildRecursive(e, "Scrollbar Vertical");

				ClampScrollView(scrollView);

				// topButton is the actual combobox the user clicks to open the
				// popup
				if (e.topButton == null)
				{
					Synergy.LogError("UIPopup has no topButton");
				}
				else
				{
					Adjust(e.topButton, info);
				}

				// popupButtonPrefab is the prefab used to create items in the
				// popup
				if (e.popupButtonPrefab == null)
				{
					Synergy.LogError("UIPopup has no popupButtonPrefab");
				}
				else
				{
					ForComponent<UnityEngine.UI.Button>(e.popupButtonPrefab, (prefab) =>
					{
						Adjust(prefab, info);
					});
				}

				// there's some empty space at the bottom of the list, remove it
				// by changing the bottom offset of both the viewport and vertical
				// scrollbar; the scrollbar is also one pixel too far to the right
				ForComponent<RectTransform>(viewport, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, 0);
				});

				ForComponent<RectTransform>(scrollbar, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 1, 0);
					rt.offsetMax = new Vector2(rt.offsetMax.x - 1, rt.offsetMax.y);
				});

				// for filterable popups, hide the filter, there's a custom textbox
				// already visible
				if (e.filterField != null)
					e.filterField.gameObject.SetActive(false);
			}
			catch (Exception)
			{
				// eat it
			}
		}

		private static void Polish(UIPopup e, Info info)
		{
			try
			{
				var scrollView = RequireChildRecursive(e, "Scroll View");
				var viewport = RequireChildRecursive(e, "Viewport");
				var scrollbar = RequireChildRecursive(e, "Scrollbar Vertical");
				var scrollbarHandle = RequireChildRecursive(scrollbar, "Handle");

				// background color for items in the popup; to have items be the
				// same color as the background of the popup, this must be
				// transparent instead of BackgroundColor because darker borders
				// would be added automatically and they can't be configured
				e.normalColor = new Color(0, 0, 0, 0);

				// background color for a selected item inside the popup
				e.selectColor = theme_.SelectionBackgroundColor;

				// background color of the popup, behind the items
				if (e.popupPanel == null)
				{
					Synergy.LogError("UIPopup has no popupPanel");
				}
				else
				{
					ForComponent<Image>(e.popupPanel, (bg) =>
					{
						bg.color = theme_.BackgroundColor;
					});
				}

				// background color of the scroll view inside the popup; this must
				// be transparent for the background color set above to appear
				// correctly
				ForComponent<Image>(scrollView, (bg) =>
				{
					bg.color = new Color(0, 0, 0, 0);
				});

				// topButton is the actual combobox the user clicks to open the
				// popup
				if (e.topButton == null)
					Synergy.LogError("UIPopup has no topButton");
				else
					Polish(e.topButton, info);

				// popupButtonPrefab is the prefab used to create items in the
				// popup
				if (e.popupButtonPrefab == null)
				{
					Synergy.LogError("UIPopup has no popupButtonPrefab");
				}
				else
				{
					ForComponent<UnityEngine.UI.Button>(e.popupButtonPrefab, (prefab) =>
					{
						Polish(prefab, info);
					});
				}

				// scrollbar background color
				ForComponent<Image>(scrollbar, (bg) =>
				{
					bg.color = theme_.SliderBackgroundColor;
				});

				// scrollbar handle color
				ForComponent<Image>(scrollbarHandle, (i) =>
				{
					i.color = theme_.ButtonBackgroundColor;
				});
			}
			catch (Exception)
			{
				// eat it
			}
		}

		private static void Adjust(UIDynamicColorPicker picker, Info info)
		{
			if (picker.colorPicker == null)
				Synergy.LogError("UIDynamicColorPicker has no colorPicker");
			else
				Adjust(picker.colorPicker, info);

			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				ForChildRecursive(picker, name, (c) =>
				{
					ForComponent<UnityEngine.UI.Button>(c, (button) =>
					{
						Adjust(button, info);
					});
				});
			}
		}

		private static void Polish(UIDynamicColorPicker picker, Info info)
		{
			// background
			ForComponent<Image>(picker, (bg) =>
			{
				bg.color = new Color(0, 0, 0, 0);
			});


			// label on top
			if (picker.labelText == null)
			{
				Synergy.LogError("UIDynamicColorPicker has no labelText");
			}
			else
			{
				picker.labelText.color = theme_.TextColor;
				picker.labelText.alignment = TextAnchor.MiddleLeft;
			}


			if (picker.colorPicker == null)
				Synergy.LogError("UIDynamicColorPicker has no colorPicker");
			else
				Polish(picker.colorPicker, info);


			// buttons at the bottom
			var buttons = new List<string>()
			{
				"DefaultValueButton",
				"CopyToClipboardButton",
				"PasteFromClipboardButton"
			};

			foreach (var name in buttons)
			{
				ForChildRecursive(picker, name, (c) =>
				{
					ForComponent<UnityEngine.UI.Button>(c, (button) =>
					{
						Polish(button, info.WithSetFont(false));
					});
				});
			}
		}

		private static List<UnityEngine.UI.Slider> GetPickerSliders(
			HSVColorPicker picker)
		{
			var sliders = new List<UnityEngine.UI.Slider>();


			if (picker.redSlider == null)
				Synergy.LogError("HSVColorPIcker has no redSlider");
			else
				sliders.Add(picker.redSlider);

			if (picker.greenSlider == null)
				Synergy.LogError("HSVColorPIcker has no greenSlider");
			else
				sliders.Add(picker.greenSlider);

			if (picker.blueSlider == null)
				Synergy.LogError("HSVColorPIcker has no blueSlider");
			else
				sliders.Add(picker.blueSlider);


			return sliders;
		}

		private static void Adjust(HSVColorPicker picker, Info info)
		{
			foreach (var slider in GetPickerSliders(picker))
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider?.transform?.parent;

				if (parent == null)
				{
					Synergy.LogError("color picker slider " + slider.name + " has no parent");
				}
				else
				{
					ForChildRecursive(parent, "Text", (textObject) =>
					{
						ForComponent<Text>(textObject, (text) =>
						{
							var rt = text.GetComponent<RectTransform>();
							rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);

							Adjust(text, info.WithFontSize(theme_.SliderTextSize));
						});
					});

					ForChildRecursive(parent, "InputField", (inputObject) =>
					{
						ForComponent<InputField>(inputObject, (input) =>
						{
							var rt = input.GetComponent<RectTransform>();
							rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
						});
					});

					ForComponent<RectTransform>(slider, (rt) =>
					{
						rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y + 10);
						rt.offsetMax = new Vector2(rt.offsetMax.x + 10, rt.offsetMax.y);
					});
				}

				// adjust the slider itself
				Adjust(slider, info);
			}


			Action<UnityEngine.UI.Slider, int> moveSlider = (slider, yDelta) =>
			{
				var parent = slider?.transform?.parent;
				if (parent == null)
					return;

				ForComponent<RectTransform>(parent, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + yDelta);
					rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + yDelta);
				});
			};


			// moving all the sliders down to make space for the color
			// sample at the top
			moveSlider(picker.blueSlider, -10);
			moveSlider(picker.greenSlider, -30);
			moveSlider(picker.redSlider, -50);

			if (picker.colorSample != null)
			{
				ForComponent<RectTransform>(picker.colorSample, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 50);
				});
			}
		}

		private static void Polish(HSVColorPicker picker, Info info)
		{
			foreach (var slider in GetPickerSliders(picker))
			{
				// sliders are actually in a parent that has the panel, label,
				// input and slider
				var parent = slider?.transform?.parent;

				if (parent == null)
				{
					Synergy.LogError("color picker slider " + slider.name + " has no parent");
				}
				else
				{
					ForChildRecursive(parent, "Panel", (panel) =>
					{
						ForComponent<Image>(parent, (bg) =>
						{
							bg.color = new Color(0, 0, 0, 0);
						});
					});

					ForChildRecursive(parent, "Text", (textObject) =>
					{
						ForComponent<Text>(textObject, (text) =>
						{
							Polish(text, new Info(
								false, info.Enabled, info.Font,
								theme_.SliderTextSize));
						});
					});

					ForChildRecursive(parent, "InputField", (input) =>
					{
						ForComponent<InputField>(input, (field) =>
						{
							// that input doesn't seem to get styled properly, can't
							// get the background color to change, so just change the
							// text color
							//Polish(input, font, fontSize, false);

							if (field.textComponent == null)
							{
								Synergy.LogError("InputField has no textComponent");
							}
							else
							{
								field.textComponent.color = theme_.TextColor;
								field.textComponent.fontSize = theme_.SliderTextSize;
							}
						});
					});
				}

				// polish the slider itself
				Polish(slider, info);
			}
		}

		private static void Setup(UIDynamicSlider e, Info info)
		{
			Adjust(e, info);
			Polish(e, info);
		}

		private static void Adjust(UIDynamicSlider e, Info info)
		{
			Adjust(e.slider, info);
		}

		private static void Polish(UIDynamicSlider e, Info info)
		{
			Polish(e.slider, info);
		}


		private static void Setup(UnityEngine.UI.Slider e, Info info)
		{
			Adjust(e, info);
			Polish(e, info);
		}

		private static void Adjust(UnityEngine.UI.Slider e, Info info)
		{
			ForChildRecursive(e, "Fill", (fill) =>
			{
				ForComponent<RectTransform>(fill, (rt) =>
				{
					rt.offsetMin = new Vector2(rt.offsetMin.x - 4, rt.offsetMin.y);
				});
			});
		}

		private static void Polish(UnityEngine.UI.Slider e, Info info)
		{
			// slider background color
			ForComponent<Image>(e, (bg) =>
			{
				bg.color = theme_.SliderBackgroundColor;
			});

			ForComponent<UIStyleSlider>(e, (ss) =>
			{
				ss.normalColor = theme_.ButtonBackgroundColor;
				ss.highlightedColor = theme_.HighlightBackgroundColor;
				ss.pressedColor = theme_.HighlightBackgroundColor;
				ss.UpdateStyle();
			});

			ForChildRecursive(e, "Fill", (fill) =>
			{
				ForComponent<Image>(fill, (bg) =>
				{
					bg.color = new Color(0, 0, 0, 0);
				});
			});
		}
	}
}
