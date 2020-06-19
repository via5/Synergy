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
		private UIDynamicTextField field_ = null;
		private CustomInputField input_ = null;
		private bool ignore_ = false;

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
					try
					{
						ignore_ = true;
						input_.text = value;
					}
					finally
					{
						ignore_ = false;
					}
				}
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
			field_ = WidgetObject.GetComponent<UIDynamicTextField>();
			input_ = WidgetObject.gameObject.AddComponent<CustomInputField>();
			input_.clicked = OnClicked;
			input_.textComponent = field_.UItext;
			input_.text = text_;
			input_.onEndEdit.AddListener(OnEdited);

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
			Utilities.Handler(() =>
			{
				Root.SetFocus(this);
			});
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
