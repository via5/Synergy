using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label : Widget
	{
		private GameObject object_ = null;
		private string text_ = "";

		public Label(string t = "")
		{
			text_ = t;
		}

		protected override void DoCreate()
		{
			object_ = new GameObject(text_);
			object_.transform.SetParent(Root.PluginParent, false);

			var rect = object_.AddComponent<RectTransform>();
			rect.transform.SetParent(Root.PluginParent, false);
			rect.offsetMin = new Vector2(Bounds.Left, Bounds.Top);
			rect.offsetMax = new Vector2(Bounds.Right, Bounds.Bottom);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Center.X, -Bounds.Center.Y);

			var text = object_.AddComponent<Text>();

			text.alignment = TextAnchor.MiddleCenter;
			text.color = new Color(0.85f, 0.85f, 0.85f);
			text.raycastTarget = false;
			text.text = text_;
			text.fontSize = Root.DefaultFontSize;
			text.font = Root.DefaultFont;

			var layoutElement = object_.AddComponent<LayoutElement>();
			layoutElement.minWidth = Bounds.Width;
			layoutElement.preferredWidth = Bounds.Width;
			layoutElement.flexibleWidth = Bounds.Width;
			layoutElement.ignoreLayout = true;
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_), 40);
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
