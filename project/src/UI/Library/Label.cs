using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label : Widget
	{
		private string text_ = "";

		private Text textObject_ = null;

		public Label(string t = "")
		{
			text_ = t;
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

				if (textObject_ != null)
					textObject_.text = value;
			}
		}

		protected override void DoCreate()
		{
			textObject_ = Object.AddComponent<Text>();
			textObject_.alignment = TextAnchor.MiddleLeft;
			textObject_.color = Root.DefaultTextColor;
			textObject_.raycastTarget = false;
			textObject_.text = text_;
			textObject_.fontSize = Root.DefaultFontSize;
			textObject_.font = Root.DefaultFont;
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_), 40);
		}

		public override string TypeName
		{
			get
			{
				return "label";
			}
		}

		public override string DebugLine
		{
			get
			{
				return base.DebugLine + " '" + text_ + "'";
			}
		}
	}
}
