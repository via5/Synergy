﻿using UnityEngine;
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
		private readonly JSONStorableString ss_ = new JSONStorableString("", "");

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
				ss_.valNoCallback = value;
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
			var input = Object.gameObject.AddComponent<CustomInputField>();
			input.clicked = OnClicked;
			input.textComponent = field_.UItext;
			ss_.inputField = input;
			field_.backgroundColor = Color.white;
			ss_.valNoCallback = text_;

			field_.UItext.alignment = TextAnchor.MiddleLeft;
			field_.UItext.color = Color.black;
			field_.UItext.raycastTarget = false;
			field_.UItext.fontSize = Style.FontSize;
			field_.UItext.font = Style.Font;

			var tr = field_.UItext.GetComponent<RectTransform>();
			tr.offsetMax = new Vector2(tr.offsetMax.x, tr.offsetMax.y - 5);

			ss_.dynamicText = field_;
			field_.text = text_;
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
