using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		private string text_ = "";
		private UIDynamicButton button_ = null;

		public Button(string t = "")
		{
			text_ = t;
		}

		protected override GameObject CreateGameObject()
		{
			var t = UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab);

			button_ = t.GetComponent<UIDynamicButton>();

			return button_.gameObject;
		}

		protected override void DoCreate()
		{
			button_.buttonText.text = text_;
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		public override string TypeName
		{
			get
			{
				return "button";
			}
		}
	}
}
