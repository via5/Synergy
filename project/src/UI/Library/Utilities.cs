namespace Synergy.UI
{
	class Size
	{
		public float width, height;

		public Size()
			: this(0, 0)
		{
		}

		public Size(float w, float h)
		{
			width = w;
			height = h;
		}
	}

	class Rectangle
	{
		public float left, top, right, bottom;

		public Rectangle()
		{
		}

		public Rectangle(Rectangle r)
			: this(r.left, r.top, r.size)
		{
		}

		public Rectangle(float xx, float yy, Size s)
		{
			left = xx;
			top = yy;
			right = left + s.width;
			bottom = top + s.height;
		}

		static public Rectangle FromSize(float x, float y, float w, float h)
		{
			return new Rectangle(x, y, new Size(w, h));
		}

		static public Rectangle FromPoints(float x1, float y1, float x2, float y2)
		{
			return new Rectangle(x1, y1, new Size(x2 - x1, y2 - y1));
		}

		public float width
		{
			get { return right - left; }
		}

		public float height
		{
			get { return bottom - top; }
		}

		public Size size
		{
			get { return new Size(width, height); }
		}
	}
}
