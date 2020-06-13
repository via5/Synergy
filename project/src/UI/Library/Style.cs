using UnityEngine;

namespace Synergy.UI
{
	class Style
	{
		private static Font font_ = null;

		public static Font Font
		{
			get
			{
				if (font_ == null)
				{
					font_ = (Font)Resources.GetBuiltinResource(
						typeof(Font), "Arial.ttf");
				}

				return font_;
			}
		}

		public static Color TextColor
		{
			get { return new Color(0.84f, 0.84f, 0.84f); }
		}

		public static Color InverseTextColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public static Color BackgroundColor
		{
			get { return new Color(0.15f, 0.15f, 0.15f); }
		}

		public static Color ButtonBackgroundColor
		{
			get { return new Color(0.25f, 0.25f, 0.25f); }
		}

		public static Color HighlightBackgroundColor
		{
			get { return new Color(0.35f, 0.35f, 0.35f); }
		}

		public static Color SelectionBackgroundColor
		{
			get { return new Color(0.4f, 0.4f, 0.4f); }
		}

		public static int FontSize
		{
			get { return 28; }
		}
	}
}
