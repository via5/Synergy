using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Synergy.UI
{
	class CustomInputField : InputField
	{
		public delegate void ClickedCallback();

		public ClickedCallback clicked;

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			clicked?.Invoke();
		}
	}

	class TextBox : Widget
	{
		public override string TypeName { get { return "textbox"; } }

		private string text_ = "";
		private UIDynamicTextField field_ = null;
		private CustomInputField input_ = null;

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
				input_.text = value;
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableTextFieldPrefab)
					.gameObject;
		}

		protected override void DoCreate()
		{
			field_ = Object.GetComponent<UIDynamicTextField>();

			input_ = Object.gameObject.AddComponent<CustomInputField>();
			input_.clicked = OnClicked;
			input_.textComponent = field_.UItext;
			input_.text = text_;

			var tr = field_.UItext.GetComponent<RectTransform>();
			tr.offsetMax = new Vector2(tr.offsetMax.x, tr.offsetMax.y - 5);

			Style.Polish(field_);
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnClicked()
		{
			Root.SetFocus(this);
		}
	}
}
