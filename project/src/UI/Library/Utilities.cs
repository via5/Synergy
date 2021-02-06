﻿using Leap.Unity;
using System;
using UnityEngine;

namespace Synergy.UI
{
	class Utilities
	{
		public const string AddSymbol = "+";
		public const string CloneSymbol = "+*";
		public const string CloneZeroSymbol = "+*0";
		public const string RemoveSymbol = "\x2013";  // en dash
		public const string UpArrow = "\x25b2";
		public const string DownArrow = "\x25bc";

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

		public static void BringToTop(GameObject o)
		{
			BringToTop(o.transform);
		}

		public static void BringToTop(Transform t)
		{
			while (t != null)
			{
				t.SetAsLastSibling();
				t = t.transform.parent;
			}
		}

		public static int[] WordRange(string text, int caret)
		{
			if (text.Length == 0)
				return new int[2] { 0, 0 };

			int begin = caret;

			if (caret >= text.Length)
			{
				// double-clicked past the end of the text
				--begin;
			}


			{
				var startedOnWs = char.IsWhiteSpace(text, begin);

				while (begin > 0)
				{
					--begin;

					var ws = char.IsWhiteSpace(text, begin);
					if (ws != startedOnWs)
					{
						++begin;
						break;
					}
				}
			}


			int end = caret;

			if (end >= text.Length)
			{
				// double-clicked past the end of the text
			}
			else
			{
				var startedOnWs = char.IsWhiteSpace(text, end);

				while (end < text.Length)
				{
					++end;

					if (end >= text.Length)
						break;

					var ws = char.IsWhiteSpace(text, end);
					if (ws != startedOnWs)
					{
						break;
					}
				}
			}

			return new int[2] { begin, end };
		}

		public static void SetRectTransform(RectTransform rt, Rectangle r)
		{
			rt.offsetMin = new Vector2(r.Left, r.Top);
			rt.offsetMax = new Vector2(r.Right, r.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(r.Center.X, -r.Center.Y);
		}

		public static void SetRectTransform(Component c, Rectangle r)
		{
			SetRectTransform(c.GetComponent<RectTransform>(), r);
		}

		public static void SetRectTransform(GameObject o, Rectangle r)
		{
			SetRectTransform(o.GetComponent<RectTransform>(), r);
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

			var rt = o.GetComponent<RectTransform>();
			if (rt != null)
			{
				Synergy.LogError("  rect: " + rt.rect.ToString());
				Synergy.LogError("  offsetMin: " + rt.offsetMin.ToString());
				Synergy.LogError("  offsetMax: " + rt.offsetMax.ToString());
				Synergy.LogError("  anchorMin: " + rt.anchorMin.ToString());
				Synergy.LogError("  anchorMax: " + rt.anchorMax.ToString());
				Synergy.LogError("  anchorPos: " + rt.anchoredPosition.ToString());
			}

			DumpComponents(o);
			Synergy.LogError("---");

			var parent = o?.transform?.parent?.gameObject;
			if (parent != null)
				DumpComponentsAndUp(parent);
		}

		public static void DumpComponentsAndDown(Component c, bool dumpRt = false)
		{
			DumpComponentsAndDown(c.gameObject, dumpRt);
		}

		public static void DumpComponentsAndDown(
			GameObject o, bool dumpRt = false, int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + o.name);

			if (dumpRt)
			{
				var rt = o.GetComponent<RectTransform>();
				if (rt != null)
				{
					Synergy.LogError(new string(' ', indent * 2) + "->rect: " + rt.rect.ToString());
					Synergy.LogError(new string(' ', indent * 2) + "->offsetMin: " + rt.offsetMin.ToString());
					Synergy.LogError(new string(' ', indent * 2) + "->offsetMax: " + rt.offsetMax.ToString());
					Synergy.LogError(new string(' ', indent * 2) + "->anchorMin: " + rt.anchorMin.ToString());
					Synergy.LogError(new string(' ', indent * 2) + "->anchorMax: " + rt.anchorMax.ToString());
					Synergy.LogError(new string(' ', indent * 2) + "->anchorPos: " + rt.anchoredPosition.ToString());
				}
			}

			DumpComponents(o, indent);

			foreach (Transform c in o.transform)
				DumpComponentsAndDown(c.gameObject, dumpRt, indent + 1);
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

	struct Point
	{
		public float X, Y;

		public static Point Zero
		{
			get { return new Point(0, 0); }
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

	struct Size
	{
		public float Width, Height;

		public static Size Zero
		{
			get { return new Size(0, 0); }
		}

		public Size(float w, float h)
		{
			Width = w;
			Height = h;
		}

		public static Size Min(Size a, Size b)
		{
			return new Size(
				Math.Min(a.Width, b.Width),
				Math.Min(a.Height, b.Height));
		}

		public static Size Max(Size a, Size b)
		{
			return new Size(
				Math.Max(a.Width, b.Width),
				Math.Max(a.Height, b.Height));
		}

		public static Size operator +(Size a, Size b)
		{
			return new Size(a.Width + b.Width, a.Height + b.Height);
		}

		public override string ToString()
		{
			return Width.ToString() + "*" + Height.ToString();
		}
	}


	struct Rectangle
	{
		public float Left, Top, Right, Bottom;

		public static Rectangle Zero
		{
			get { return FromPoints(0, 0, 0, 0); }
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
			set { Right = Left + value; }
		}

		public float Height
		{
			get { return Bottom - Top; }
			set { Bottom = Top + value; }
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
			r.Translate(p.X, p.Y);
			return r;
		}

		public Rectangle TranslateCopy(float dx, float dy)
		{
			var r = new Rectangle(this);
			r.Translate(dx, dy);
			return r;
		}

		public void Translate(Point p)
		{
			Translate(p.X, p.Y);
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

		public void Deflate(float f)
		{
			Left += f;
			Top += f;
			Right -= f;
			Bottom -= f;
		}

		public Rectangle DeflateCopy(Insets i)
		{
			var r = new Rectangle(this);
			r.Deflate(i);
			return r;
		}

		public Rectangle DeflateCopy(float f)
		{
			var r = new Rectangle(this);
			r.Deflate(f);
			return r;
		}

		public override string ToString()
		{
			return
				"(" + Left.ToString() + "," + Top.ToString() + ")-" +
				"(" + Right.ToString() + "," + Bottom.ToString() + ")";
		}
	}


	struct Insets
	{
		public float Left, Top, Right, Bottom;

		public static Insets Zero
		{
			get { return new Insets(0); }
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

		public Size Size
		{
			get { return new Size(Left + Right, Top + Bottom); }
		}

		public static Insets operator +(Insets a, Insets b)
		{
			return new Insets(
				a.Left + b.Left,
				a.Top + b.Top,
				a.Right + b.Right,
				a.Bottom + b.Bottom);
		}

		public override string ToString()
		{
			return
				Left.ToString() + "," + Top.ToString() + "," +
				Right.ToString() + "," + Bottom.ToString();
		}
	}
}
