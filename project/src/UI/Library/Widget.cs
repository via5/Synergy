using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class WidgetGraphics : Graphic
	{
		private Widget widget_ = null;

		public WidgetGraphics()
		{
		}

		public Widget Widget
		{
			get { return widget_; }
			set { widget_ = value; }
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			var rt = rectTransform;

			// left
			Line(vh,
				new Point(rt.rect.left, -rt.rect.top),
				new Point(rt.rect.left+widget_.Borders.Left, -rt.rect.bottom),
				widget_.BorderColor);

			// top
			Line(vh,
				new Point(rt.rect.left, -rt.rect.top),
				new Point(rt.rect.right, -rt.rect.top - widget_.Borders.Top),
				widget_.BorderColor);

			// right
			Line(vh,
				new Point(rt.rect.right - widget_.Borders.Right, -rt.rect.top),
				new Point(rt.rect.right, -rt.rect.bottom),
				widget_.BorderColor);

			// bottom
			Line(vh,
				new Point(rt.rect.left, -rt.rect.bottom + widget_.Borders.Bottom),
				new Point(rt.rect.right, -rt.rect.bottom),
				widget_.BorderColor);
		}

		private void Line(VertexHelper vh, Point a, Point b, Color c)
		{
			Color32 c32 = c;
			var i = vh.currentVertCount;

			vh.AddVert(new Vector3(a.X, a.Y), c32, new Vector2(0f, 0f));
			vh.AddVert(new Vector3(a.X, b.Y), c32, new Vector2(0f, 1f));
			vh.AddVert(new Vector3(b.X, b.Y), c32, new Vector2(1f, 1f));
			vh.AddVert(new Vector3(b.X, a.Y), c32, new Vector2(1f, 0f));

			vh.AddTriangle(i+0, i+1, i+2);
			vh.AddTriangle(i+2, i+3, i+0);
		}
	}

	class Widget
	{
		static public float DontCare = -1;

		private Widget parent_ = null;
		private string name_ = "";
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rectangle bounds_ = new Rectangle();
		private Size minSize_ = new Size(0, 0);
		private GameObject object_ = null;
		private WidgetGraphics graphic_ = null;
		private bool visible_ = true;
		private Insets margins_ = new Insets();
		private Insets borders_ = new Insets();
		private Insets padding_ = new Insets();
		private Color borderColor_ = Root.DefaultTextColor;

		public Widget(string name = "")
		{
			name_ = name;
		}

		public Layout Layout
		{
			get
			{
				return layout_;
			}

			set
			{
				layout_ = value;

				if (layout_ != null)
					layout_.Parent = this;
			}
		}

		public GameObject Object
		{
			get { return object_; }
		}

		public bool StrictlyVisible
		{
			get { return visible_; }
		}

		public bool Visible
		{
			get
			{
				if (!visible_)
					return false;

				var p = parent_;
				while (p != null)
				{
					if (!p.visible_)
						return false;

					p = p.parent_;
				}

				return true;
			}

			set
			{
				visible_ = value;
				UpdateVisibility();
			}
		}

		public Insets Margins
		{
			get { return margins_; }
			set { margins_ = value; }
		}

		public Insets Borders
		{
			get { return borders_; }
			set { borders_ = value; }
		}

		public Insets Padding
		{
			get { return padding_; }
			set { padding_ = value; }
		}

		public Color BorderColor
		{
			get { return borderColor_; }
			set { borderColor_ = value; }
		}

		public Rectangle Bounds
		{
			get { return bounds_; }
			set { bounds_ = value; }
		}

		public Rectangle ContentBounds
		{
			get
			{
				return Rectangle.FromPoints(
					Bounds.Left + Margins.Left + Borders.Left + Padding.Left,
					Bounds.Top + Margins.Top + Borders.Top + Padding.Top,
					Bounds.Right - (Margins.Right + Borders.Right + Padding.Right),
					Bounds.Bottom - (Margins.Bottom + Borders.Bottom + Padding.Bottom));
			}
		}

		public Rectangle ClientBounds
		{
			get
			{
				var r = new Rectangle(ContentBounds);
				r.Translate(-Bounds.Left, -Bounds.Top);
				return r;
			}
		}

		public Rectangle RelativeBounds
		{
			get
			{
				var r = new Rectangle(bounds_);

				if (parent_ != null)
					r.Translate(-parent_.Bounds.Left, -parent_.Bounds.Top);

				return r;
			}
		}

		public T Add<T>(T w, LayoutData d = null)
			where T : Widget
		{
			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
			return w;
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Synergy.LogError(
					"can't remove widget '" + w.Name + "' from " +
					"'" + Name + "', not found");

				return;
			}

			layout_.Remove(w);
			w.parent_ = null;
		}

		public void DoLayout()
		{
			layout_?.DoLayout();

			foreach (var w in children_)
				w.DoLayout();
		}

		public void Create()
		{
			object_ = CreateGameObject();
			object_.SetActive(Visible);

			SetupGameObject();
			DoCreate();

			var g = new GameObject();
			g.transform.SetParent(object_.transform, false);

			var br = Rectangle.FromPoints(
				-(Borders.Left + Padding.Left),
				-(Borders.Top + Padding.Top),
				ContentBounds.Width + (Borders.Right + Padding.Right),
				ContentBounds.Height + (Borders.Bottom + Padding.Bottom));

			graphic_ = g.AddComponent<WidgetGraphics>();
			graphic_.Widget = this;
			graphic_.raycastTarget = false;

			var rt = graphic_.rectTransform;
			rt.offsetMin = new Vector2(br.Left, br.Top);
			rt.offsetMax = new Vector2(br.Right, br.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(br.Center.X, -br.Center.Y);

			foreach (var w in children_)
				w.Create();
		}

		public Size PreferredSize
		{
			get
			{
				var s = new Size();

				if (layout_ != null)
					s = layout_.PreferredSize;

				s = Size.Max(s, GetPreferredSize());
				s = Size.Max(s, MinimumSize);

				return s;
			}
		}

		public Size MinimumSize
		{
			get { return minSize_; }
			set { minSize_ = value; }
		}


		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual Size GetPreferredSize()
		{
			return new Size(DontCare, DontCare);
		}

		protected virtual GameObject CreateGameObject()
		{
			var o = new GameObject();

			o.AddComponent<RectTransform>();
			o.AddComponent<LayoutElement>();

			return o;
		}

		protected virtual void SetupGameObject()
		{
			object_.transform.SetParent(Root.PluginParent, false);

			var wr = ContentBounds;

			var rect = object_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(wr.Left, wr.Top);
			rect.offsetMax = new Vector2(wr.Right, wr.Bottom);
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = new Vector2(0, 1);
			rect.anchoredPosition = new Vector2(wr.Center.X, -wr.Center.Y);

			Synergy.LogError(wr.ToString());

			var layoutElement = object_.GetComponent<LayoutElement>();
			layoutElement.minWidth = wr.Width;
			layoutElement.preferredWidth = wr.Width;
			layoutElement.flexibleWidth = wr.Width;
			layoutElement.minHeight = wr.Height;
			layoutElement.preferredHeight = wr.Height;
			layoutElement.flexibleHeight = wr.Height;
			layoutElement.ignoreLayout = true;
		}

		public void Dump(int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + DebugLine);

			foreach (var w in children_)
				w.Dump(indent + 1);
		}

		public string Name
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
			}
		}

		public virtual string DebugLine
		{
			get
			{
				var list = new List<string>();

				list.Add(TypeName);
				list.Add(name_);
				list.Add("b=" + Bounds.ToString());
				list.Add("rb=" + RelativeBounds.ToString());
				list.Add("ps=" + PreferredSize.ToString());

				return string.Join(" ", list.ToArray());
			}
		}

		public virtual string TypeName
		{
			get
			{
				return "widget";
			}
		}

		private void UpdateVisibility()
		{
			if (object_ != null)
				object_.SetActive(Visible);

			foreach (var w in children_)
				w.UpdateVisibility();
		}
	}
}
