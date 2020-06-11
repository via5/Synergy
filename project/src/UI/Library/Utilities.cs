using Leap.Unity;
using System;
using UnityEngine;

namespace Synergy.UI
{
	class Utilities
	{
		public static void DumpComponents(GameObject o)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
				Synergy.LogError(c.ToString());
		}

		public static void DumpComponentsAndUp(GameObject o)
		{
			Synergy.LogError(o.name);

			foreach (var c in o.GetComponents(typeof(Component)))
				Synergy.LogError(c.ToString());

			Synergy.LogError("---");

			var parent = o.transform.parent.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}
	}

	class Point
	{
		public float X, Y;

		public Point()
			: this(0, 0)
		{
		}

		public Point(float x, float y)
		{
			X = x;
			Y = y;
		}
	}

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

		public static Size Max(Size a, Size b)
		{
			return new Size(
				Math.Max(a.width, b.width),
				Math.Max(a.height, b.height));
		}

		public override string ToString()
		{
			return width.ToString() + "*" + height.ToString();
		}
	}

	class Rectangle
	{
		public float Left, Top, Right, Bottom;

		public Rectangle()
		{
		}

		public Rectangle(Rectangle r)
			: this(r.Left, r.Top, r.Size)
		{
		}

		public Rectangle(Point p, Size s)
			: this(p.X, p.Y, s)
		{
		}

		public Rectangle(float x, float y, Size s)
		{
			Left = x;
			Top = y;
			Right = Left + s.width;
			Bottom = Top + s.height;
		}

		static public Rectangle FromSize(float x, float y, float w, float h)
		{
			return new Rectangle(x, y, new Size(w, h));
		}

		static public Rectangle FromPoints(float x1, float y1, float x2, float y2)
		{
			return new Rectangle(x1, y1, new Size(x2 - x1, y2 - y1));
		}

		public float Width
		{
			get { return Right - Left; }
		}

		public float Height
		{
			get { return Bottom - Top; }
		}

		public Point TopLeft
		{
			get { return new Point(Left, Top); }
		}

		public Point TopRight
		{
			get { return new Point(Right, Top); }
		}

		public Point BottomLeft
		{
			get { return new Point(Left, Bottom); }
		}

		public Point BottomRight
		{
			get { return new Point(Right, Bottom); }
		}

		public Point Center
		{
			get { return new Point(Left + Width / 2, Top + Height / 2); }
		}

		public Size Size
		{
			get { return new Size(Width, Height); }
		}

		public override string ToString()
		{
			return
				"(" + Left.ToString() + "," + Top.ToString() + ")-" +
				"(" + Right.ToString() + "," + Bottom.ToString() + ")";
		}
	}
}
