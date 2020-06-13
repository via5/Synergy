using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
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

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableTextFieldPrefab)
					.gameObject;
		}

		protected override void DoCreate()
		{
			field_ = Object.GetComponent<UIDynamicTextField>();
			var input = Object.gameObject.AddComponent<InputField>();
			input.textComponent = field_.UItext;
			ss_.inputField = input;
			field_.backgroundColor = Color.white;
			ss_.valNoCallback = text_;

			field_.UItext.alignment = TextAnchor.MiddleLeft;
			field_.UItext.color = Color.black;
			field_.UItext.raycastTarget = false;
			field_.UItext.fontSize = Root.DefaultFontSize;
			field_.UItext.font = Root.DefaultFont;

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
