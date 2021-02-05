using System;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		public override string TypeName { get { return "button"; } }

		public event Callback Clicked;

		private string text_ = "";
		private UIDynamicButton button_ = null;
		private int align_ = Label.AlignCenter | Label.AlignVCenter;

		public Button(string t = "", Callback clicked = null)
		{
			text_ = t;

			if (clicked != null)
				Clicked += clicked;
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

				if (button_ != null)
					button_.buttonText.text = value;
			}
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				align_ = value;
				NeedsLayout("alignment changed");
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = WidgetObject.GetComponent<UIDynamicButton>();
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.text = text_;
			button_.buttonText.alignment = Label.ToTextAnchor(align_);

			Style.Setup(this);
		}

		protected override void DoSetEnabled(bool b)
		{
			button_.button.interactable = b;
		}

		protected override void Polish()
		{
			base.Polish();
			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			var rt = button_.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 2, rt.offsetMin.y - 1);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 2, rt.offsetMax.y);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(Root.TextLength(Font, FontSize, text_) + 20, 40);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(150, 40);
		}

		private void OnClicked()
		{
			Utilities.Handler(() =>
			{
				Root.SetFocus(this);
				Clicked?.Invoke();
			});
		}
	}


	class ToolButton : UI.Button
	{
		public ToolButton(string text = "", Callback clicked = null)
			: base(text, clicked)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(Root.TextLength(Font, FontSize, Text) + 20, 40);
		}

		protected override Size DoGetMinimumSize()
		{
			return new UI.Size(50, DontCare);
		}
	}


	class CustomButton : UI.Button
	{
		public CustomButton(string text = "", Callback clicked = null)
			: base(text, clicked)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(0, 0);
		}

		protected override Size DoGetMinimumSize()
		{
			return new UI.Size(DontCare, DontCare);
		}
	}
}
