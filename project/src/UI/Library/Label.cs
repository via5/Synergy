using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label : Widget
	{
		public override string TypeName { get { return "label"; } }

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
			textObject_.color = Style.TextColor;
			textObject_.raycastTarget = false;
			textObject_.text = text_;
			textObject_.fontSize = Style.FontSize;
			textObject_.font = Style.Font;
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_), 40);
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
