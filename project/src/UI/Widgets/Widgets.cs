using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UI = SynergyUI;

namespace Synergy
{
	interface IWidget
	{
		bool Enabled { get; set; }
		void AddToUI();
		void RemoveFromUI();
		Selectable GetSelectable();
		UIDynamic DynamicElement { get; }
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
		public const int NavButtons     = 0x10;  // string list
		public const int Filterable     = 0x20;  // string list
		public const int AllowNone      = 0x40;  // atom, link lists

		public const int LineHeight = 60;

		public abstract bool Enabled { get; set; }
		public abstract void AddToUI();
		public abstract void RemoveFromUI();
		public abstract Selectable GetSelectable();
		public abstract UIDynamic DynamicElement { get; }

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
		private bool enabled_ = true;

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

		public override UIDynamic DynamicElement
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

			var s = GetSelectable();
			if (s != null)
				s.interactable = enabled_;
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
					return enabled_;
			}

			set
			{
				enabled_ = value;

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


	abstract class BasicSlider<T, ParameterType> :
		BasicWidget<JSONStorableFloat, UIDynamicSlider>
	{
		public BasicSlider(int flags)
			: base(flags)
		{
		}

		public abstract T Value { get; set; }
		public abstract ParameterType Parameter { get; set; }
		public abstract T Default { get; set; }

		public abstract void Set(T min, T max, T value);
		public abstract void SetFromRange(T initial, T range, T current);
	}


	class FloatSlider : BasicSlider<float, FloatParameter>
	{
		public delegate void Callback(float value);

		private readonly Callback callback_;
		private FloatParameter parameter_ = null;
		private SubCheckbox animatable_ = null;

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

			if (parameter_ != null)
			{
				if (Synergy.Instance.Options.PickAnimatable)
				{
					animatable_ = new SubCheckbox(
						this, "Animatable", parameter_.Registered,
						AnimatableChanged);

					animatable_.AddToUI();
				}

				Value = parameter_.Value;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveSlider(element_);
				element_ = null;
			}

			if (animatable_ != null)
				animatable_.RemoveFromUI();
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

		public void Set(FloatRange range, float value)
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

		public override FloatParameter Parameter
		{
			get { return parameter_; }
			set { parameter_ = value; }
		}

		public override float Default
		{
			get { return storable_.defaultVal; }
			set { storable_.defaultVal = 0; }
		}

		public FloatRange Range
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

		private void AnimatableChanged(bool b)
		{
			Utilities.Handler(() =>
			{
				if (parameter_ != null)
				{
					if (b)
					{
						parameter_.SpecificName = storable_.name;
						parameter_.Register();
					}
					else
					{
						parameter_.Unregister();
					}
				}
			});
		}
	}


	class IntSlider : BasicSlider<int, IntParameter>
	{
		public delegate void Callback(int value);

		private readonly Callback callback_;
		private IntParameter parameter_ = null;
		private SubCheckbox animatable_ = null;

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

			if (parameter_ != null)
			{
				if (Synergy.Instance.Options.PickAnimatable)
				{
					animatable_ = new SubCheckbox(
						this, "Animatable", parameter_.Registered,
						AnimatableChanged);

					animatable_.AddToUI();
				}

				Value = parameter_.Value;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveSlider(element_);
				element_ = null;
			}

			if (animatable_ != null)
				animatable_.RemoveFromUI();
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

		public void Set(IntRange range, int value)
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

		public override IntParameter Parameter
		{
			get { return parameter_; }
			set { parameter_ = value; }
		}

		public override int Default
		{
			get { return (int)storable_.defaultVal; }
			set { storable_.defaultVal = value; }
		}

		public IntRange Range
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

		private void AnimatableChanged(bool b)
		{
			Utilities.Handler(() =>
			{
				if (parameter_ != null)
				{
					if (b)
					{
						parameter_.SpecificName = storable_.name;
						parameter_.Register();
					}
					else
					{
						parameter_.Unregister();
					}
				}
			});
		}
	}


	class Textbox : BasicWidget<JSONStorableString, UIDynamicTextField>
	{
		public delegate void Callback(string value);

		private Callback changedCallback_;
		private Callback afterEditCallback_;
		private InputField input_ = null;

		private int oldCaret_ = -1;
		private int oldAnchor_ = -1;
		private int oldFocus_ = -1;

		private bool inCallback_ = false;
		private IgnoreFlag ignore_ = new IgnoreFlag();
		private string placeholder_ = "";


		public Textbox(string name, string def="", Callback callback=null, int flags=0)
			: base(flags)
		{
			changedCallback_ = callback;
			storable_ = new JSONStorableString(name, def);
		}

		public Callback Changed
		{
			get { return changedCallback_; }
			set { changedCallback_ = value; }
		}

		public Callback AfterEdit
		{
			get { return afterEditCallback_; }
			set { afterEditCallback_ = value; }
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateTextField(
				storable_, Bits.IsSet(flags_, Right));

			input_ = element_.gameObject.AddComponent<InputField>();
			input_.textComponent = element_.UItext;
			storable_.inputField = input_;
			input_.onValueChanged.AddListener(OnChanged);
			input_.onEndEdit.AddListener(OnEndEdit);

			var ly = element_.GetComponent<LayoutElement>();
			ly.minHeight = 50;
			element_.height = 50;


			var go = new GameObject();
			go.transform.SetParent(input_.transform, false);

			var text = go.AddComponent<Text>();
			text.supportRichText = false;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			text.raycastTarget = false;

			var rt = text.rectTransform;
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.offsetMin = new Vector2(6, 0);
			rt.offsetMax = new Vector2(0, -6);


			text.color = new Color(0.5f, 0.5f, 0.5f);
			text.font = input_.textComponent.font;
			text.fontSize = input_.textComponent.fontSize;
			text.fontStyle = input_.textComponent.fontStyle;

			input_.placeholder = text;
			input_.placeholder.GetComponent<Text>().text = placeholder_;
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
				if (inCallback_)
				{
					Synergy.Instance.CreateTimer(UI.Timer.Immediate, () =>
					{
						Value = value;
					});
				}

				ignore_.Do(() =>
				{
					storable_.valNoCallback = value;

					if (input_ != null)
						input_.text = value;
				});
			}
		}

		public string Placeholder
		{
			get
			{
				if (input_ == null)
					return placeholder_;
				else
					return input_.placeholder.GetComponent<Text>().text;
			}

			set
			{
				placeholder_ = value;

				if (input_ != null)
					input_.placeholder.GetComponent<Text>().text = value;
			}
		}

		public void Focus()
		{
			if (input_ == null)
				return;

			input_.ActivateInputField();

			if (oldCaret_ != -1)
			{
				Synergy.Instance.CreateTimer(UI.Timer.Immediate, () =>
				{
					input_.caretPosition = oldCaret_;
					input_.selectionAnchorPosition = oldAnchor_;
					input_.selectionFocusPosition = oldFocus_;
					input_.ForceLabelUpdate();
				});
			}
		}

		private void OnChanged(string v)
		{
			if (ignore_)
				return;

			inCallback_ = true;

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

				changedCallback_?.Invoke(v);
			});

			inCallback_ = false;
		}

		private void OnEndEdit(string v)
		{
			if (ignore_)
				return;

			inCallback_ = true;

			Utilities.Handler(() =>
			{
				afterEditCallback_?.Invoke(v);
			});

			inCallback_ = false;
		}
	}


	class StringList :
		BasicWidget<JSONStorableStringChooser, UIDynamicPopup>
	{
		public delegate void Callback(string value);
		public delegate void IndexCallback(int i);
		public delegate void OpenCallback();

		public event OpenCallback OnOpen;
		public event Callback SelectionChanged;
		public event IndexCallback SelectionIndexChanged;

		private float popupHeight_ = -1;


		public StringList(string name, Callback callback = null, int flags = 0)
			: this(name, "", new List<string>(), callback, flags)
		{
		}

		public StringList(
			string name, string def, List<string> entries,
			Callback callback=null, int flags = 0)
				: base(flags)
		{
			SelectionChanged += callback;
			CreateStorable(name, def, entries, null, Changed);
		}

		public StringList(
			string name, string def, List<string> entries, List<string> tags,
			Callback callback = null, int flags = 0)
				: base(flags)
		{
			SelectionChanged += callback;
			CreateStorable(name, def, entries, tags, Changed);
		}

		protected StringList(int flags)
			: base(flags)
		{
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

		public override Selectable GetSelectable()
		{
			return element_?.popup.topButton;
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			if (Bits.IsSet(flags_, Filterable))
			{
				element_ = sc_.CreateFilterablePopup(
					storable_, Bits.IsSet(flags_, Right));
			}
			else
			{
				element_ = sc_.CreateScrollablePopup(
					storable_, Bits.IsSet(flags_, Right));
			}

			if (popupHeight_ >= 0)
				element_.popupPanelHeight = popupHeight_;

			element_.popup.onOpenPopupHandlers += () =>
			{
				OnOpen?.Invoke();
			};

			if (Bits.IsSet(flags_, NavButtons))
			{
				// thank mr acidbubbles

				element_.popup.labelText.alignment = TextAnchor.UpperCenter;
				element_.popup.labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.89f);

				{
					var btn = UnityEngine.Object.Instantiate(
						Synergy.Instance.manager.configurableButtonPrefab);

					btn.SetParent(element_.transform, false);
					UnityEngine.Object.Destroy(btn.GetComponent<LayoutElement>());
					btn.GetComponent<UIDynamicButton>().label = "<";
					btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() =>
					{
						element_.popup.SetPreviousValue();
					});

					var prevBtnRect = btn.GetComponent<RectTransform>();
					prevBtnRect.pivot = new Vector2(0, 0);
					prevBtnRect.anchoredPosition = new Vector2(10f, 0);
					prevBtnRect.sizeDelta = new Vector2(0f, 0f);
					prevBtnRect.offsetMin = new Vector2(5f, 5f);
					prevBtnRect.offsetMax = new Vector2(80f, 50f);
					prevBtnRect.anchorMin = new Vector2(0f, 0f);
					prevBtnRect.anchorMax = new Vector2(0f, 0f);
				}

				{
					var btn = UnityEngine.Object.Instantiate(
						Synergy.Instance.manager.configurableButtonPrefab);

					btn.SetParent(element_.transform, false);
					UnityEngine.Object.Destroy(btn.GetComponent<LayoutElement>());
					btn.GetComponent<UIDynamicButton>().label = ">";
					btn.GetComponent<UIDynamicButton>().button.onClick.AddListener(() =>
					{
						element_.popup.SetNextValue();
					});

					var prevBtnRect = btn.GetComponent<RectTransform>();
					prevBtnRect.pivot = new Vector2(0, 0);
					prevBtnRect.anchoredPosition = new Vector2(10f, 0);
					prevBtnRect.sizeDelta = new Vector2(0f, 0f);
					prevBtnRect.offsetMin = new Vector2(82f, 5f);
					prevBtnRect.offsetMax = new Vector2(157f, 50f);
					prevBtnRect.anchorMin = new Vector2(0f, 0f);
					prevBtnRect.anchorMax = new Vector2(0f, 0f);
				}
			}
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

		public float PopupHeight
		{
			get
			{
				if (element_ == null)
					return popupHeight_;
				else
					return element_.popupPanelHeight;
			}

			set
			{
				popupHeight_ = value;

				if (element_ != null)
					element_.popupPanelHeight = value;
			}
		}

		public List<string> Choices
		{
			get
			{
				return storable_.choices;
			}

			set
			{
				storable_.choices = new List<string>(value);
			}
		}

		public List<string> DisplayChoices
		{
			get
			{
				return storable_.displayChoices;
			}

			set
			{
				storable_.displayChoices = new List<string>(value);
			}
		}

		private void Changed(string v)
		{
			Utilities.Handler(() =>
			{
				SelectionChanged?.Invoke(v);

				if (SelectionIndexChanged != null)
					SelectionIndexChanged(storable_.choices.IndexOf(v));
			});
		}
	}


	class Checkbox : BasicWidget<JSONStorableBool, UIDynamicToggle>
	{
		public delegate void Callback(bool b);

		private readonly Callback callback_;
		private BoolParameter parameter_ = null;
		private SubCheckbox animatable_ = null;

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

			if (parameter_ != null)
			{
				if (Synergy.Instance.Options.PickAnimatable)
				{
					animatable_ = new SubCheckbox(
						this, "Animatable", parameter_.Registered,
						AnimatableChanged);

					animatable_.AddToUI();
				}

				Value = parameter_.Value;
			}
		}

		protected override void DoRemoveFromUI()
		{
			if (element_)
			{
				sc_.RemoveToggle(element_);
				element_ = null;
			}

			if (animatable_ != null)
				animatable_.RemoveFromUI();
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

		public BoolParameter Parameter
		{
			get { return parameter_; }
			set { parameter_ = value; }
		}

		private void Changed(bool b)
		{
			Utilities.Handler(() =>
			{
				callback_?.Invoke(b);
			});
		}

		private void AnimatableChanged(bool b)
		{
			Utilities.Handler(() =>
			{
				if (parameter_ != null)
				{
					if (b)
					{
						parameter_.SpecificName = storable_.name;
						parameter_.Register();
					}
					else
					{
						parameter_.Unregister();
					}
				}
			});
		}
	}


	class SubCheckbox : BasicWidget<JSONStorableBool, UIDynamicToggle>
	{
		public delegate void Callback(bool b);

		private readonly IWidget parent_;
		private readonly Callback callback_;

		public SubCheckbox(IWidget parent, string name, Callback callback = null, int flags = 0)
			: this(parent, name, false, callback, flags)
		{
		}

		public SubCheckbox(IWidget parent, string name, bool value, Callback callback, int flags = 0)
			: base(flags)
		{
			parent_ = parent;
			storable_ = new JSONStorableBool(name, value, Changed);
			callback_ = callback;
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			element_ = sc_.CreateToggle(storable_, Bits.IsSet(flags_, Right));
			element_.toggle.interactable = !Bits.IsSet(flags_, Disabled);

			element_.transform.SetParent(parent_.DynamicElement.transform, false);
			element_.transform.GetComponent<RectTransform>().offsetMin = new Vector2(305, -50);
			element_.transform.GetComponent<RectTransform>().offsetMax = new Vector2(30, 0);
			element_.labelText.fontSize = 32;
			element_.toggle.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
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

			// add some padding when right aligned, it's too close to the edge
			if (element_.label != "" && IsRightAligned)
				element_.label += " ";

			int newLines = 0;
			foreach (char c in Text)
			{
				if (c == '\n')
					++newLines;
			}

			Height = 30 + (LineHeight * newLines);
		}

		private bool IsRightAligned
		{
			get
			{
				return
					(align_ == TextAnchor.UpperRight) ||
					(align_ == TextAnchor.MiddleRight) ||
					(align_ == TextAnchor.LowerRight);
			}
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
