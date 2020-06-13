using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		public override string TypeName { get { return "button"; } }

		public delegate void ClickHandler();

		private string text_ = "";
		private UIDynamicButton button_ = null;
		private event ClickHandler clicked_;

		public Button(string t = "")
		{
			text_ = t;
		}

		public ClickHandler Clicked
		{
			get { return clicked_; }
			set { clicked_ = value; }
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = Object.GetComponent<UIDynamicButton>();
			button_.buttonColor = Style.ButtonBackgroundColor;
			button_.buttonText.color = Style.TextColor;
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.text = text_;

			var sb = button_.GetComponent<UIStyleButton>();
			sb.normalColor = Style.ButtonBackgroundColor;
			sb.highlightedColor = Style.HighlightBackgroundColor;
			sb.colorMultiplier = 3.89f;
			sb.UpdateStyle();
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnClicked()
		{
			Root.SetFocus(this);
			clicked_();
		}
	}
}
