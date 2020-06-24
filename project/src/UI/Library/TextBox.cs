using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Synergy.UI
{
	class CustomInputField : InputField
	{
		private static Vector2 NoPos = new Vector2(float.MinValue, float.MinValue);
		private const float DoubleClickTime = 0.5f;

		public delegate void EventCallback(PointerEventData data);
		public event EventCallback Down, Up, Click, DoubleClick, TripleClick;
		public event EventCallback Focused;


		private bool selected_ = false;
		private float lastClick_ = 0;
		private int clickCount_ = 0;
		private Vector2 lastClickPos_ = NoPos;

		public override void OnPointerDown(PointerEventData data)
		{
			Utilities.Handler(() =>
			{
				HandleOnPointerDown(data);
			});
		}

		public override void OnPointerUp(PointerEventData data)
		{
			Utilities.Handler(() =>
			{
				HandleOnPointerUp(data);
			});
		}

		public override void OnDeselect(BaseEventData data)
		{
			Utilities.Handler(() =>
			{
				HandleOnDeselect(data);
			});
		}

		public int CaretPosition(Vector2 pos)
		{
			return GetCharacterIndexFromPosition(pos);
		}

		public void SelectAllText()
		{
			SelectAll();
		}

		private void HandleOnPointerDown(PointerEventData data)
		{
			base.OnPointerDown(data);
			Down?.Invoke(data);

			if (!selected_)
			{
				Focused?.Invoke(data);
				selected_ = true;
			}

			++clickCount_;
			Synergy.LogError(clickCount_.ToString());

			if (clickCount_ == 1)
			{
				lastClick_ = Time.unscaledTime;
				lastClickPos_ = data.position;
			}
			else if (clickCount_ == 2)
			{
				var timeDiff = Time.unscaledTime - lastClick_;
				var posDiff = Vector2.Distance(data.position, lastClickPos_);

				if (timeDiff <= DoubleClickTime && posDiff <= 4)
				{
					Synergy.LogError("double");
					DoubleClick?.Invoke(data);
				}
				else
				{
					lastClick_ = Time.unscaledTime;
					lastClickPos_ = data.position;
					clickCount_ = 1;
				}
			}
			else if (clickCount_ == 3)
			{
				var timeDiff = Time.unscaledTime - lastClick_;
				var posDiff = Vector2.Distance(data.position, lastClickPos_);

				if (timeDiff <= DoubleClickTime && posDiff <= 4)
				{
					TripleClick?.Invoke(data);
					lastClick_ = 0;
					lastClickPos_ = NoPos;
					clickCount_ = 0;
				}
				else
				{
					lastClick_ = Time.unscaledTime;
					lastClickPos_ = data.position;
					clickCount_ = 1;
				}
			}
		}

		private void HandleOnPointerUp(PointerEventData data)
		{
			base.OnPointerUp(data);

			Up?.Invoke(data);

			if (clickCount_ == 1)
				Click?.Invoke(data);
		}

		private void HandleOnDeselect(BaseEventData data)
		{
			selected_ = false;
			base.OnDeselect(data);
		}
	}

	class TextBox : Widget
	{
		public override string TypeName { get { return "textbox"; } }

		public class Validation
		{
			public string text;
			public bool valid;
		}

		public delegate void ValidateCallback(Validation v);
		public delegate void StringCallback(string s);

		public event ValidateCallback Validate;
		public event StringCallback Changed;


		private string text_ = "";
		private CustomInputField input_ = null;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();

		public TextBox(string t = "")
		{
			text_ = t;
			MinimumSize = new Size(100, DontCare);
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				text_ = value;

				if (input_ != null)
				{
					ignore_.Do(() =>
					{
						input_.text = value;
					});
				}
			}
		}

		protected override void DoFocus()
		{
			input_.ActivateInputField();
		}

		protected override GameObject CreateGameObject()
		{
			var go = base.CreateGameObject();

			return go;
		}

		protected override void DoCreate()
		{
			var field = new GameObject();
			field.transform.SetParent(WidgetObject.transform, false);

			var rt = field.AddComponent<RectTransform>();
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(1, 1);
			rt.offsetMin = new Vector2(5, 0);
			rt.offsetMax = new Vector2(0, 0);


			var text = field.AddComponent<Text>();
			text.color = Style.TextColor;
			text.fontSize = Style.FontSize;
			text.font = Style.Font;
			text.alignment = TextAnchor.MiddleLeft;

			input_ = field.AddComponent<CustomInputField>();
			input_.Down += OnMouseDown;
			input_.Focused += OnFocused;
			input_.DoubleClick += OnDoubleClick;
			input_.TripleClick += OnTripleClick;
			input_.textComponent = text;
			input_.text = text_;
			input_.onEndEdit.AddListener(OnEdited);
			input_.lineType = InputField.LineType.SingleLine;

			var image = WidgetObject.AddComponent<Image>();
			image.raycastTarget = false;

			Style.Polish(this);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnMouseDown(PointerEventData data)
		{
			Utilities.Handler(() =>
			{
				Root.SetFocus(this);
			});
		}

		private void OnFocused(PointerEventData data)
		{
			Vector2 pos;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				input_.GetComponent<RectTransform>(), data.position,
				data.pressEventCamera, out pos);

			int c = input_.CaretPosition(pos);

			var old = input_.selectionColor;
			input_.selectionColor = new Color(0, 0, 0, 0);

			Synergy.Instance.CreateTimer(Timer.Immediate, () =>
			{
				input_.caretPosition = c;
				input_.selectionAnchorPosition = c;
				input_.selectionFocusPosition = c;
				input_.selectionColor = old;
				input_.ForceLabelUpdate();
			});
		}

		private void OnDoubleClick(PointerEventData data)
		{
			Vector2 pos;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				input_.GetComponent<RectTransform>(), data.position,
				data.pressEventCamera, out pos);

			int caret = input_.CaretPosition(pos);
			var range = Utilities.WordRange(input_.text, caret);

			input_.caretPosition = range[1];
			input_.selectionAnchorPosition = range[0];
			input_.selectionFocusPosition = range[1];
			input_.ForceLabelUpdate();
		}

		private void OnTripleClick(PointerEventData data)
		{
			input_.SelectAllText();
		}

		private void OnEdited(string s)
		{
			Utilities.Handler(() =>
			{
				if (ignore_)
					return;

				if (Validate != null)
				{
					var v = new Validation();
					v.text = s;
					v.valid = true;

					Validate(v);

					if (!v.valid)
					{
						input_.text = text_;
						return;
					}

					text_ = v.text;
					input_.text = text_;
				}
				else
				{
					text_ = s;
				}

				Changed?.Invoke(text_);
			});
		}
	}
}
