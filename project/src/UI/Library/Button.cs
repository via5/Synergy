using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		public override string TypeName { get { return "button"; } }

		public delegate void Callback();
		public event Callback Clicked;

		private string text_ = "";
		private UIDynamicButton button_ = null;

		public Button(string t = "", Callback clicked = null)
		{
			text_ = t;

			if (clicked != null)
				Clicked += clicked;
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = Object.GetComponent<UIDynamicButton>();
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.text = text_;

			Style.Polish(button_);
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnClicked()
		{
			Root.SetFocus(this);
			Clicked?.Invoke();
		}
	}
}
