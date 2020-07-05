﻿using System.Net.Security;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label : Widget
	{
		public override string TypeName { get { return "label"; } }

		public const int AlignLeft = 0x01;
		public const int AlignCenter = 0x02;
		public const int AlignRight = 0x04;
		public const int AlignTop = 0x08;
		public const int AlignVCenter = 0x10;
		public const int AlignBottom = 0x20;

		private string text_;
		private int align_;
		private Text textObject_ = null;

		public Label(string t = "", int align = AlignLeft|AlignVCenter)
		{
			text_ = t;
			align_ = align;
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				if (text_ != value)
				{
					text_ = value;

					if (Root.TextLength(Font, FontSize, text_) > Bounds.Width)
						NeedsLayout("text changed");
				}

				if (textObject_ != null)
					textObject_.text = value;
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


		protected override void DoCreate()
		{
			textObject_ = WidgetObject.AddComponent<Text>();
			textObject_.text = text_;
			textObject_.horizontalOverflow = HorizontalWrapMode.Overflow;
			textObject_.maskable = true;

			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			textObject_.alignment = ToTextAnchor(align_);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(Root.FitText(
				Font, FontSize, text_, new Size(maxWidth, maxHeight)).Width, 40);
		}

		protected override Size DoGetMinimumSize()
		{
			return new Size(Root.TextLength(Font, FontSize, text_), 40);
		}

		public static TextAnchor ToTextAnchor(int a)
		{
			if (a == (AlignLeft | AlignTop))
				return TextAnchor.UpperLeft;
			else if (a == (AlignLeft | AlignVCenter))
				return TextAnchor.MiddleLeft;
			else if (a == (AlignLeft | AlignBottom))
				return TextAnchor.LowerLeft;
			else if (a == (AlignCenter | AlignTop))
				return TextAnchor.UpperCenter;
			else if (a == (AlignCenter | AlignVCenter))
				return TextAnchor.MiddleCenter;
			else if (a == (AlignCenter | AlignBottom))
				return TextAnchor.LowerCenter;
			else if (a == (AlignRight | AlignTop))
				return TextAnchor.UpperRight;
			else if (a == (AlignRight | AlignVCenter))
				return TextAnchor.MiddleRight;
			else if (a == (AlignRight | AlignBottom))
				return TextAnchor.LowerRight;
			else
				return TextAnchor.MiddleLeft;
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
