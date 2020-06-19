using Leap.Unity;
using System;
using UnityEngine;

namespace Synergy.UI
{
	class Utilities
	{
		public static void Handler(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Synergy.LogError(e.ToString());
			}
		}


		public static GameObject FindChildRecursive(Component c, string name)
		{
			return FindChildRecursive(c.gameObject, name);
		}

		public static GameObject FindChildRecursive(GameObject o, string name)
		{
			if (o == null)
				return null;

			if (o.name == name)
				return o;

			foreach (Transform c in o.transform)
			{
				var r = FindChildRecursive(c.gameObject, name);
				if (r != null)
					return r;
			}

			return null;
		}

		public static void DumpComponents(GameObject o, int indent = 0)
		{
			foreach (var c in o.GetComponents(typeof(Component)))
				Synergy.LogError(new string(' ', indent * 2) + c.ToString());
		}

		public static void DumpComponentsAndUp(Component c)
		{
			DumpComponentsAndUp(c.gameObject);
		}

		public static void DumpComponentsAndUp(GameObject o)
		{
			Synergy.LogError(o.name);
			DumpComponents(o);
			Synergy.LogError("---");

			var parent = o?.transform?.parent?.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}

		public static void DumpComponentsAndDown(Component c)
		{
			DumpComponentsAndDown(c.gameObject);
		}

		public static void DumpComponentsAndDown(GameObject o, int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + o.name);
			DumpComponents(o, indent);

			foreach (Transform c in o.transform)
				DumpComponentsAndDown(c.gameObject, indent + 1);
		}

		public static void DumpRectsAndDown(Component c)
		{
			DumpRectsAndDown(c.gameObject);
		}

		public static void DumpRectsAndDown(GameObject o, int indent = 0)
		{
			if (o == null)
				return;

			var rt = o.GetComponent<RectTransform>();

			if (rt == null)
			{
				Synergy.LogError(new string(' ', indent * 2) + o.name);
			}
			else
			{
				Synergy.LogError(new string(' ', indent * 2) + o.name + " " +
					"omin=" + rt.offsetMin.ToString() + " " +
					"omax=" + rt.offsetMax.ToString() + " " +
					"amin=" + rt.anchorMin.ToString() + " " +
					"amxn=" + rt.anchorMax.ToString() + " " +
					"ap=" + rt.anchoredPosition.ToString());
			}

			foreach (Transform c in o.transform)
				DumpRectsAndDown(c.gameObject, indent + 1);
		}

		public static void DumpChildren(GameObject o, int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + o.name);

			foreach (Transform c in o.transform)
				DumpChildren(c.gameObject, indent + 1);
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

		public static Point operator -(Point p)
		{
			return new Point(-p.X, -p.Y);
		}
	}

	class Size
	{
		public float Width, Height;

		public Size()
			: this(0, 0)
		{
		}

		public Size(float w, float h)
		{
			Width = w;
			Height = h;
		}

		public static Size Max(Size a, Size b)
		{
			return new Size(
				Math.Max(a.Width, b.Width),
				Math.Max(a.Height, b.Height));
		}

		public override string ToString()
		{
			return Width.ToString() + "*" + Height.ToString();
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
			Right = Left + s.Width;
			Bottom = Top + s.Height;
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

		public Rectangle TranslateCopy(Point p)
		{
			var r = new Rectangle(this);

			if (p != null)
				r.Translate(p.X, p.Y);

			return r;
		}

		public Rectangle TranslateCopy(float dx, float dy)
		{
			var r = new Rectangle(this);
			r.Translate(dx, dy);
			return r;
		}

		public void Translate(float dx, float dy)
		{
			Left += dx;
			Right += dx;

			Top += dy;
			Bottom += dy;
		}

		public void MoveTo(float x, float y)
		{
			Translate(x - Left, y - Top);
		}

		public void Deflate(Insets i)
		{
			Left += i.Left;
			Top += i.Top;
			Right -= i.Right;
			Bottom -= i.Bottom;
		}

		public override string ToString()
		{
			return
				"(" + Left.ToString() + "," + Top.ToString() + ")-" +
				"(" + Right.ToString() + "," + Bottom.ToString() + ")";
		}
	}


	class Insets
	{
		public float Left, Top, Right, Bottom;

		public Insets()
			: this(0, 0, 0, 0)
		{
		}

		public Insets(float all)
			: this(all, all, all, all)
		{
		}

		public Insets(float left, float top, float right, float bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public override string ToString()
		{
			return
				Left.ToString() + "," + Top.ToString() + "," +
				Right.ToString() + "," + Bottom.ToString();
		}
	}
}
