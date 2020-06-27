﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Synergy.UI
{
	class BorderGraphics : MaskableGraphic
	{
		private Insets borders_ = new Insets();
		private Color color_ = new Color(0, 0, 0, 0);

		public BorderGraphics()
		{
			raycastTarget = false;
		}

		public Insets Borders
		{
			get
			{
				return borders_;
			}

			set
			{
				borders_ = value;
				SetVerticesDirty();
			}
		}

		public Color Color
		{
			get
			{
				return color_;
			}

			set
			{
				color_ = value;
				SetVerticesDirty();
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			var rt = rectTransform;

			// left
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMin),
				new Point(rt.rect.xMin + borders_.Left, -rt.rect.yMax),
				color_);

			// top
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMin),
				new Point(rt.rect.xMax, -rt.rect.yMin - borders_.Top),
				color_);

			// right
			Line(vh,
				new Point(rt.rect.xMax - borders_.Right, -rt.rect.yMin),
				new Point(rt.rect.xMax, -rt.rect.yMax),
				color_);

			// bottom
			Line(vh,
				new Point(rt.rect.xMin, -rt.rect.yMax + borders_.Bottom),
				new Point(rt.rect.xMax, -rt.rect.yMax),
				color_);
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


	class MouseCallbacks : MonoBehaviour,
		IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
	{
		private Widget widget_ = null;

		public Widget Widget
		{
			get { return widget_; }
			set { widget_ = value; }
		}

		public void OnPointerEnter(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerEnter(d);
			});
		}

		public void OnPointerExit(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerExit(d);
			});
		}

		public void OnPointerDown(PointerEventData d)
		{
			Utilities.Handler(() =>
			{
				if (widget_ != null)
					widget_.OnPointerDown(d);
			});
		}
	}


	interface IWidget
	{
		void Remove();
	}


	abstract class Widget : IDisposable, IWidget
	{
		public virtual string TypeName { get { return "widget"; } }

		public delegate void Callback();
		public event Callback Created;

		public const float DontCare = -1;

		private Widget parent_ = null;
		private string name_ = "";
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rectangle bounds_ = new Rectangle();
		private Size minSize_ = new Size(0, 0);

		private GameObject mainObject_ = null;
		private GameObject widgetObject_ = null;
		private GameObject graphicsObject_ = null;
		private GameObject bgObject_ = null;
		private BorderGraphics borderGraphics_ = null;

		private bool visible_ = true;
		private bool enabled_ = true;
		private Insets margins_ = new Insets();
		private Insets borders_ = new Insets();
		private Insets padding_ = new Insets();
		private Color borderColor_ = Style.TextColor;
		private Color bgColor_ = new Color(0, 0, 0, 0);
		private readonly Tooltip tooltip_;


		public Widget(string name = "")
		{
			name_ = name;
			tooltip_ = new Tooltip();
		}

		public virtual void Dispose()
		{
			Destroy();
		}

		protected virtual void Destroy()
		{
			foreach (var c in children_)
				c.Destroy();

			if (mainObject_ != null)
			{
				UnityEngine.Object.Destroy(mainObject_);
				mainObject_ = null;
				widgetObject_ = null;
				graphicsObject_ = null;
				bgObject_ = null;
				borderGraphics_ = null;
			}
		}

		public static string S(string s, params object[] ps)
		{
			return Strings.Get(s, ps);
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

				NeedsLayout();
			}
		}

		public GameObject MainObject
		{
			get { return mainObject_; }
		}

		public GameObject WidgetObject
		{
			get { return widgetObject_; }
		}

		public bool Visible
		{
			get
			{
				return visible_;
			}

			set
			{
				if (visible_ != value)
				{
					visible_ = value;
					NeedsLayout();
				}
			}
		}

		public bool IsVisibleOnScreen()
		{
			if (mainObject_ == null)
				return visible_;
			else
				return mainObject_.activeInHierarchy;
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;

				if (widgetObject_ != null)
					DoSetEnabled(enabled_);
			}
		}

		public Insets Margins
		{
			get
			{
				return margins_;
			}

			set
			{
				margins_ = value ?? new Insets(0);
				NeedsLayout();
			}
		}

		public Insets Borders
		{
			get
			{
				return borders_;
			}

			set
			{
				borders_ = value ?? new Insets(0);

				if (borderGraphics_ != null)
					borderGraphics_.Borders = value ?? new Insets(0);

				NeedsLayout();
			}
		}

		public Insets Padding
		{
			get
			{
				return padding_;
			}

			set
			{
				padding_ = value ?? new Insets(0);
				NeedsLayout();
			}
		}

		public Insets Insets
		{
			get { return margins_ + borders_ + padding_; }
		}

		public Color BorderColor
		{
			get
			{
				return borderColor_;
			}

			set
			{
				borderColor_ = value;

				if (borderGraphics_ != null)
					borderGraphics_.Color = value;
			}
		}

		public Color BackgroundColor
		{
			get
			{
				return bgColor_;
			}

			set
			{
				bgColor_ = value;
				SetBackground();
			}
		}

		public Tooltip Tooltip
		{
			get { return tooltip_; }
		}

		public Rectangle Bounds
		{
			get { return new Rectangle(bounds_); }
			set { bounds_ = value; }
		}

		public Rectangle AbsoluteClientBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle ClientBounds
		{
			get
			{
				var r = new Rectangle(0, 0, Bounds.Size);

				r.Deflate(Margins);
				r.Deflate(Borders);
				r.Deflate(Padding);

				return r;
			}
		}

		public Rectangle RelativeBounds
		{
			get
			{
				var r = new Rectangle(Bounds);

				if (parent_ != null)
					r.Translate(-parent_.Bounds.TopLeft);

				return r;
			}
		}

		public Size GetPreferredSize(float maxWidth, float maxHeight)
		{
			var s = new Size();

			if (layout_ != null)
				s = layout_.PreferredSize;

			s = Size.Max(s, DoGetPreferredSize(maxWidth, maxHeight));
			s = Size.Max(s, MinimumSize);

			s += Margins.Size + Borders.Size + Padding.Size;

			return s;
		}

		public Size MinimumSize
		{
			get
			{
				return minSize_;
			}

			set
			{
				minSize_ = value;
				NeedsLayout();
			}
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

		public virtual Root GetRoot()
		{
			if (parent_ != null)
				return parent_.GetRoot();

			return null;
		}

		public void Focus()
		{
			DoFocus();
		}

		protected virtual void DoFocus()
		{
			// no-op
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
				list.Add("ps=" + GetPreferredSize(DontCare, DontCare).ToString());
				list.Add("ly=" + (Layout?.TypeName ?? "none"));

				return string.Join(" ", list.ToArray());
			}
		}


		public void AddGeneric(UI.IWidget w, LayoutData d = null)
		{
			Add((Widget)w, d);
		}

		public T Add<T>(T w, LayoutData d = null)
			where T : Widget
		{
			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
			NeedsLayout();
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
			w.Destroy();

			NeedsLayout();
		}

		public void Remove()
		{
			if (parent_ == null)
			{
				Synergy.LogError("can't remove '" + Name + ", no parent");
				return;
			}

			parent_.Remove(this);
		}

		public void BringToTop()
		{
			if (widgetObject_ != null)
				Utilities.BringToTop(widgetObject_);
		}

		public void DoLayout()
		{
			layout_?.DoLayout();

			foreach (var w in children_)
				w.DoLayout();
		}

		public virtual void Create()
		{
			if (mainObject_ == null)
			{
				mainObject_ = new GameObject();
				mainObject_.AddComponent<RectTransform>();
				mainObject_.AddComponent<LayoutElement>();
				mainObject_.AddComponent<MouseCallbacks>().Widget = this;
				mainObject_.SetActive(visible_);

				if (parent_?.MainObject == null)
					mainObject_.transform.SetParent(Root.PluginParent, false);
				else
					mainObject_.transform.SetParent(parent_.MainObject.transform, false);

				widgetObject_ = CreateGameObject();
				widgetObject_.AddComponent<MouseCallbacks>().Widget = this;
				widgetObject_.transform.SetParent(mainObject_.transform, false);

				DoCreate();
				DoSetEnabled(enabled_);

				graphicsObject_ = new GameObject();
				graphicsObject_.transform.SetParent(mainObject_.transform, false);

				borderGraphics_ = graphicsObject_.AddComponent<BorderGraphics>();
				borderGraphics_.Borders = borders_;
				borderGraphics_.Color = borderColor_;

				SetBackground();
			}

			foreach (var w in children_)
				w.Create();

			Created?.Invoke();
		}

		private void SetBackground()
		{
			if (mainObject_ == null)
				return;

			if (bgObject_ == null && bgColor_.a > 0)
			{
				bgObject_ = new GameObject();
				bgObject_.transform.SetParent(mainObject_.transform, false);
				bgObject_.AddComponent<Image>();
			}

			SetBackgroundBounds();
		}

		private void SetMainObjectBounds()
		{
			var r = RelativeBounds;
			Utilities.SetRectTransform(mainObject_, r);

			var layoutElement = mainObject_.GetComponent<LayoutElement>();
			layoutElement.minWidth = r.Width;
			layoutElement.preferredWidth = r.Width;
			layoutElement.flexibleWidth = r.Width;
			layoutElement.minHeight = r.Height;
			layoutElement.preferredHeight = r.Height;
			layoutElement.flexibleHeight = r.Height;
			layoutElement.ignoreLayout = true;
		}

		private void SetBackgroundBounds()
		{
			if (bgObject_ == null)
				return;

			bgObject_.transform.SetAsFirstSibling();

			var image = bgObject_.GetComponent<Image>();
			image.color = bgColor_;
			image.raycastTarget = false;

			var r = new Rectangle(0, 0, Bounds.Size);
			r.Deflate(Margins);

			Utilities.SetRectTransform(bgObject_, r);
		}

		private void SetBorderBounds()
		{
			var r = new Rectangle(0, 0, Bounds.Size);
			r.Deflate(Margins);

			Utilities.SetRectTransform(borderGraphics_, r);
		}

		private void SetWidgetObjectBounds()
		{
			Utilities.SetRectTransform(widgetObject_, ClientBounds);
		}

		public virtual void UpdateBounds()
		{
			SetBackground();

			SetMainObjectBounds();
			SetBackgroundBounds();
			SetBorderBounds();
			SetWidgetObjectBounds();

			foreach (var w in children_)
				w.UpdateBounds();

			mainObject_.SetActive(visible_);
		}

		public virtual void NeedsLayout()
		{
			if (parent_ != null)
				parent_.NeedsLayout();
		}

		public void Dump(int indent = 0)
		{
			Synergy.LogError(new string(' ', indent * 2) + DebugLine);

			foreach (var w in children_)
				w.Dump(indent + 1);
		}


		protected virtual GameObject CreateGameObject()
		{
			var o = new GameObject();
			o.AddComponent<RectTransform>();
			o.AddComponent<LayoutElement>();
			return o;
		}

		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual void DoSetEnabled(bool b)
		{
			// no-op
		}

		protected virtual Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(DontCare, DontCare);
		}

		public virtual void OnPointerEnter(PointerEventData d)
		{
			GetRoot()?.Tooltips.WidgetEntered(this);
		}

		public virtual void OnPointerExit(PointerEventData d)
		{
			GetRoot()?.Tooltips.WidgetExited(this);
		}

		public virtual void OnPointerDown(PointerEventData d)
		{
			GetRoot()?.Tooltips.Hide();
		}
	}


	class Panel : Widget
	{
		public Panel(string name = "")
			: base(name)
		{
		}

		public Panel(Layout ly)
		{
			Layout = ly;
		}
	}
}
