﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace Synergy
{
	interface IWidget
	{
		bool Enabled { get; set; }
		void AddToUI();
		void RemoveFromUI();
		Selectable GetSelectable();
	}

	class WidgetList
	{
		private readonly List<IWidget> widgets_ = new List<IWidget>();

		public void AddToUI(IWidget w)
		{
			widgets_.Add(w);
			w.AddToUI();
		}

		public void RemoveFromUI()
		{
			foreach (var w in widgets_)
				w.RemoveFromUI();

			widgets_.Clear();
		}
	}


	abstract class Widget : IWidget
	{
		public const int Disabled       = 0x01;
		public const int Right          = 0x02;
		public const int Constrained    = 0x04;  // slider
		public const int Tall           = 0x08;  // button, checkbox

		public const int LineHeight = 60;

		public abstract bool Enabled { get; set; }
		public abstract void AddToUI();
		public abstract void RemoveFromUI();
		public abstract Selectable GetSelectable();

		protected Synergy sc_ = Synergy.Instance;
	}

	abstract class BasicWidget<StorableType, UIElement> : Widget
		where StorableType : class
		where UIElement : UIDynamic
	{
		protected StorableType storable_ = null;
		protected UIElement element_ = null;
		protected readonly int flags_ = 0;

		private float height_ = -1;

		protected BasicWidget(int flags)
		{
			flags_ = flags;
		}

		public StorableType Storable
		{
			get { return storable_; }
		}

		public UIElement Element
		{
			get
			{
				return element_;
			}
		}

		public override void AddToUI()
		{
			DoAddToUI();

			if (height_ >= 0 && element_ != null)
				element_.height = height_;
		}

		public override void RemoveFromUI()
		{
			DoRemoveFromUI();
		}

		protected abstract void DoAddToUI();
		protected abstract void DoRemoveFromUI();

		public override Selectable GetSelectable()
		{
			return null;
		}

		public override bool Enabled
		{
			get
			{
				var s = GetSelectable();
				if (s != null)
					return s.interactable;
				else
					return false;
			}

			set
			{
				var s = GetSelectable();
				if (s != null)
					s.interactable = value;
			}
		}

		public float Height
		{
			get
			{
				if (element_ != null)
					return element_.height;
				else
					return height_;
			}

			set
			{
				height_ = value;

				if (element_ != null)
					element_.height = value;
			}
		}
	}

	abstract class CompoundWidget : BasicWidget<object, UIDynamic>
	{
		protected CompoundWidget(int flags = 0)
			: base(flags)
		{
		}
	}

	class Spacer : BasicWidget<object, UIDynamic>
	{
		public Spacer(int height, int flags = 0)
			: base(flags)
		{
			Height = height;
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateSpacer(Bits.IsSet(flags_, Right));
			element_.height = 20;
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveSpacer(element_);
				element_ = null;
			}
		}
	}

	class SmallSpacer : Spacer
	{
		public SmallSpacer(int flags = 0)
			: base(50, flags)
		{
		}
	}

	class LargeSpacer : Spacer
	{
		public LargeSpacer(int flags = 0)
			: base(100, flags)
		{
		}
	}


	abstract class BasicSlider<T> :
		BasicWidget<JSONStorableFloat, UIDynamicSlider>
	{
		public BasicSlider(int flags)
			: base(flags)
		{
		}

		public abstract T Value { get; set; }
		public abstract T Default { get; set; }
		public abstract Range<T> Range { get; set; }

		public abstract void Set(T min, T max, T value);
		public abstract void Set(Range<T> range, T value);
		public abstract void SetFromRange(T initial, T range, T current);
	}


	class FloatSlider : BasicSlider<float>
	{
		public delegate void Callback(float value);

		private readonly Callback callback_;

		public FloatSlider(string name, Callback callback=null, int flags=0)
			: this(name, 0, new FloatRange(0, 0), callback, flags)
		{
		}

		public FloatSlider(
			string name, float value, FloatRange range,
			Callback callback=null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;

			storable_ = new JSONStorableFloat(
				name, value, Changed, range.Minimum, range.Maximum,
				Bits.IsSet(flags_, Constrained),
				!Bits.IsSet(flags_, Disabled));
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateSlider(storable_, Bits.IsSet(flags_, Right));

			if (Bits.IsSet(flags_, Disabled))
			{
				element_.defaultButtonEnabled = false;
				element_.quickButtonsEnabled = false;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveSlider(element_);
				element_ = null;
			}
		}

		public override Selectable GetSelectable()
		{
			return element_?.slider;
		}

		public override void Set(float min, float max, float value)
		{
			storable_.valNoCallback = value;
			storable_.min = Math.Min(min, value);
			storable_.max = Math.Max(max, value);
		}

		public override void Set(Range<float> range, float value)
		{
			Set(range.Minimum, range.Maximum, value);
		}

		public override void SetFromRange(
			float initial, float range, float current)
		{
			Set(initial - range, initial + range, current);
		}

		public override float Value
		{
			get { return storable_.val; }
			set { Set(storable_.min, storable_.max, value); }
		}

		public override float Default
		{
			get { return storable_.defaultVal; }
			//set { storable_.defaultVal = value; }
			set { storable_.defaultVal = 0; }
		}

		public override Range<float> Range
		{
			get
			{
				return new FloatRange(storable_.min, storable_.max);
			}

			set
			{
				Set(value.Minimum, value.Maximum, Value);
			}
		}

		public string Text
		{
			set
			{
				if (element_ != null)
					element_.label = value;
			}
		}

		private void Changed(float v)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke(v);
			});
		}
	}


	class IntSlider : BasicSlider<int>
	{
		public delegate void Callback(int value);

		private readonly Callback callback_;

		public IntSlider(
			string name, int value, IntRange range,
			Callback callback=null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;

			storable_ = new JSONStorableFloat(
				name, value, Changed, range.Minimum, range.Maximum,
				Bits.IsSet(flags_, Constrained),
				!Bits.IsSet(flags_, Disabled));
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateSlider(storable_, Bits.IsSet(flags_, Right));
			element_.slider.wholeNumbers = true;

			if (Bits.IsSet(flags_, Disabled))
			{
				element_.defaultButtonEnabled = false;
				element_.quickButtonsEnabled = false;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveSlider(element_);
				element_ = null;
			}
		}

		public override Selectable GetSelectable()
		{
			return element_?.slider;
		}

		public override void Set(int min, int max, int value)
		{
			storable_.min = min;
			storable_.max = max;
			storable_.valNoCallback = value;
		}

		public override void Set(Range<int> range, int value)
		{
			Set(range.Minimum, range.Maximum, value);
		}

		public override void SetFromRange(
			int initial, int range, int current)
		{
			storable_.min = initial - range;
			storable_.max = initial + range;
			storable_.valNoCallback = current;
		}

		public override int Value
		{
			get { return (int)storable_.val; }
			set { storable_.valNoCallback = value; }
		}

		public override int Default
		{
			get { return (int)storable_.defaultVal; }
			set { storable_.defaultVal = value; }
		}

		public override Range<int> Range
		{
			get
			{
				return new IntRange((int)storable_.min, (int)storable_.max);
			}

			set
			{
				storable_.min = value.Minimum;
				storable_.max = value.Maximum;
			}
		}

		public string Text
		{
			set
			{
				if (element_ != null)
					element_.label = value;
			}
		}

		private void Changed(float v)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke((int)v);
			});
		}
	}


	class Textbox : BasicWidget<JSONStorableString, UIDynamicTextField>
	{
		public delegate void Callback(string value);

		private readonly Callback callback_;
		private InputField input_ = null;

		private int oldCaret_ = -1;
		private int oldAnchor_ = -1;
		private int oldFocus_ = -1;

		public Textbox(string name, string def="", Callback callback=null, int flags=0)
			: base(flags)
		{
			callback_ = callback;
			storable_ = new JSONStorableString(name, def);
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateTextField(
				storable_, Bits.IsSet(flags_, Right));

			input_ = element_.gameObject.AddComponent<InputField>();
			input_.textComponent = element_.UItext;
			storable_.inputField = input_;
			input_.onValueChanged.AddListener(Changed);
			element_.height = 30;
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveTextField(element_);
				element_ = null;
			}
		}

		public string Value
		{
			get
			{
				if (input_ == null)
					return storable_.val;
				else
					return input_.text;
			}

			set
			{
				storable_.valNoCallback = value;

				if (input_ != null)
					input_.text = value;
			}
		}

		public void Focus()
		{
			if (input_ == null)
				return;

			input_.ActivateInputField();

			if (oldCaret_ != -1)
			{
				Synergy.Instance.CreateTimer(0.001f, () =>
				{
					input_.caretPosition = oldCaret_;
					input_.selectionAnchorPosition = oldAnchor_;
					input_.selectionFocusPosition = oldFocus_;
					input_.ForceLabelUpdate();
				});
			}
		}

		private void Changed(string v)
		{
			Utilities.Handler(() =>
			{
				if (input_ == null)
				{
					oldCaret_ = -1;
					oldAnchor_ = -1;
					oldFocus_ = -1;
				}
				else
				{
					oldCaret_ = input_.caretPosition;
					oldAnchor_ = input_.selectionAnchorPosition;
					oldFocus_ = input_.selectionFocusPosition;
				}

				callback_?.Invoke(v);
			});
		}
	}


	class StringList :
		BasicWidget<JSONStorableStringChooser, UIDynamicPopup>
	{
		public delegate void Callback(string value);

		private readonly Callback callback_;

		public StringList(
			string name, string def, List<string> entries,
			Callback callback=null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			CreateStorable(name, def, entries, null, Changed);
		}

		public StringList(
			string name, string def, List<string> entries, List<string> tags,
			Callback callback = null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			CreateStorable(name, def, entries, tags, Changed);
		}

		protected StringList(int flags)
			: base(flags)
		{
			callback_ = null;
		}

		protected void CreateStorable(
			string name, string def, List<string> entries, List<string> tags,
			JSONStorableStringChooser.SetStringCallback callback)
		{
			if (tags == null)
			{
				storable_ = new JSONStorableStringChooser(
					name, entries, def, name, callback);
			}
			else
			{
				storable_ = new JSONStorableStringChooser(
					name, tags, entries, def, name, callback);
			}
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateScrollablePopup(
				storable_, Bits.IsSet(flags_, Right));
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemovePopup(element_);
				element_ = null;
			}
		}

		public string Text
		{
			get { return storable_.label; }
			set { storable_.label = value; }
		}

		public string Value
		{
			get { return storable_.val; }
			set { storable_.valNoCallback = value; }
		}

		public List<string> Choices
		{
			get
			{
				return storable_.choices;
			}

			set
			{
				storable_.choices = value;
			}
		}

		private void Changed(string v)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke(v);
			});
		}
	}

	class Checkbox : BasicWidget<JSONStorableBool, UIDynamicToggle>
	{
		public delegate void Callback(bool b);

		private readonly Callback callback_;

		public Checkbox(string name, Callback callback=null, int flags = 0)
			: this(name, false, callback, flags)
		{
		}

		public Checkbox(string name, bool value, Callback callback, int flags=0)
			: base(flags)
		{
			storable_ = new JSONStorableBool(name, value, Changed);
			callback_ = callback;
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateToggle(storable_, Bits.IsSet(flags_, Right));

			if (Bits.IsSet(flags_, Tall))
				element_.height = LineHeight * 2;

			element_.toggle.interactable = !Bits.IsSet(flags_, Disabled);
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveToggle(element_);
				element_ = null;
			}
		}

		public override Selectable GetSelectable()
		{
			return element_?.toggle;
		}

		public bool Value
		{
			get { return storable_.val; }
			set { storable_.valNoCallback = value; }
		}

		private void Changed(bool b)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke(b);
			});
		}
	}

	class Button : BasicWidget<object, UIDynamicButton>
	{
		public delegate void Callback();

		private readonly Callback callback_;
		private string text_;
		private Color bgColor_ = Utilities.DefaultButtonColor;
		private Color textColor_ = Color.black;

		public Button(string text, Callback callback, int flags = 0)
			: base(flags)
		{
			text_ = text;
			callback_ = callback;
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateButton(text_, Bits.IsSet(flags_, Right));

			if (Bits.IsSet(flags_, Tall))
				element_.height = LineHeight * 2;

			if (callback_ != null)
				element_.button.onClick.AddListener(Clicked);

			if (bgColor_ != null)
				element_.buttonColor = bgColor_;

			if (textColor_ != null)
				element_.textColor = textColor_;
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveButton(element_);
				element_ = null;
			}
		}

		public override Selectable GetSelectable()
		{
			return element_?.button;
		}

		public string Text
		{
			get
			{
				if (element_)
					return element_.label;
				else
					return text_;
			}

			set
			{
				text_ = value;

				if (element_)
					element_.label = value;
			}
		}

		public Color BackgroundColor
		{
			get
			{
				if (element_)
					return element_.buttonColor;
				else
					return bgColor_;
			}

			set
			{
				bgColor_ = value;

				if (element_)
					element_.buttonColor = value;
			}
		}

		public Color TextColor
		{
			get
			{
				if (element_)
					return element_.textColor;
				else
					return textColor_;
			}

			set
			{
				textColor_ = value;

				if (element_)
					element_.textColor = value;
			}
		}

		private void Clicked()
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke();
			});
		}
	}

	class Label : Button
	{
		public const TextAnchor DefaultAlign = TextAnchor.MiddleLeft;

		private TextAnchor align_;

		public Label(string name = "", int flags = 0, TextAnchor align=DefaultAlign)
			: base(name, null, flags)
		{
			align_ = align;
		}

		protected override void DoAddToUI()
		{
			base.DoAddToUI();

			BackgroundColor = new Color(0, 0, 0, 0);
			TextColor = Color.black;
			Enabled = false;

			element_.buttonText.alignment = align_;

			int newLines = 0;
			foreach (char c in Text)
			{
				if (c == '\n')
					++newLines;
			}

			Height = 30 + (30 * newLines);
		}
	}

	class Header : Label
	{
		public Header(string name="", int flags = 0)
			: base(name, flags, TextAnchor.MiddleCenter)
		{
		}

		protected override void DoAddToUI()
		{
			base.DoAddToUI();

			BackgroundColor = Color.black;
			TextColor = Color.white;
		}
	}

	class ColorPicker :
		BasicWidget<JSONStorableColor, UIDynamicColorPicker>
	{
		public delegate void Callback(Color c);

		private readonly Callback callback_;

		public ColorPicker(string name, Color value, Callback callback, int flags=0)
			: base(flags)
		{
			callback_ = callback;

			storable_ = new JSONStorableColor(
				name, ColorToHSV(value), ColorChanged);
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateColorPicker(
				storable_, Bits.IsSet(flags_, Right));
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveColorPicker(element_);
				element_ = null;
			}
		}

		public Color Value
		{
			get
			{
				return ColorFromHSV(storable_.val);
			}

			set
			{
				storable_.valNoCallback = ColorToHSV(value);
			}
		}

		private void ColorChanged(float h, float s, float v)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke(Color.HSVToRGB(h, s, v));
			});
		}

		private static Color ColorFromHSV(HSVColor c)
		{
			return Color.HSVToRGB(c.H, c.S, c.V);
		}

		private static HSVColor ColorToHSV(Color c)
		{
			var hsv = new HSVColor();
			Color.RGBToHSV(c, out hsv.H, out hsv.S, out hsv.V);
			return hsv;
		}
	}


	class ConfirmableButton : CompoundWidget
	{
		public delegate void ClickHandler();

		private readonly ClickHandler handler_;
		private readonly Checkbox confirm_;
		private readonly Button button_;

		public ConfirmableButton(string name, ClickHandler callback, int flags=0)
			: base(flags)
		{
			handler_ = callback;

			confirm_ = new Checkbox(
				name + " (confirm)", false, ConfirmToggled, flags);

			button_ = new Button(name, Clicked, flags);
		}

		protected override void DoAddToUI()
		{
			confirm_.AddToUI();
			button_.AddToUI();
			button_.Enabled = false;
		}

		protected override void DoRemoveFromUI()
		{
			confirm_.RemoveFromUI();
			button_.RemoveFromUI();
		}

		private void ConfirmToggled(bool b)
		{
			button_.Enabled = b;
		}

		private void Clicked()
		{
			if (confirm_.Value)
			{
				confirm_.Value = false;
				button_.Enabled = false;
				handler_?.Invoke();
			}
		}
	}

	class FactoryStringList<Factory, T> : StringList
		where Factory : IFactory<T>, new()
		where T : class, IFactoryObject
	{
		public delegate void ObjectCallback(T v);

		private readonly ObjectCallback callback_;

		public FactoryStringList(string name, ObjectCallback callback, int flags=0)
			: this(name, "", callback, flags)
		{
		}

		public FactoryStringList(
			string name, string def, ObjectCallback callback, int flags=0)
				: base(flags)
		{
			callback_ = callback;

			var names = new Factory().GetAllDisplayNames();
			var types = new Factory().GetAllFactoryTypeNames();

			CreateStorable(name, def, names, types, Changed);
		}

		public new T Value
		{
			get
			{
				if (base.Value == "")
					return null;
				else
					return new Factory().Create(base.Value);
			}

			set
			{
				if (value == null)
					base.Value = "";
				else
					base.Value = value.GetDisplayName();
			}
		}

		private void Changed(string s)
		{
			if (callback_ == null)
				return;

			callback_(new Factory().Create(s));
		}
	}
}
