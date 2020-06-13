using System;
using System.Collections.Generic;

namespace Synergy.UI
{
	interface LayoutData
	{
	}

	abstract class Layout
	{
		private Widget parent_ = null;
		private readonly List<Widget> children_ = new List<Widget>();
		private float spacing_ = 0;

		public Widget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public List<Widget> Children
		{
			get { return children_; }
		}

		public Size PreferredSize
		{
			get { return GetPreferredSize(); }
		}

		public float Spacing
		{
			get { return spacing_; }
			set { spacing_ = value; }
		}

		public void Add(Widget w, LayoutData data = null)
		{
			if (Contains(w))
			{
				Synergy.LogError("layout already has widget " + w.Name);
				return;
			}

			children_.Add(w);
			AddImpl(w, data);
		}

		public void Remove(Widget w)
		{
			if (!children_.Remove(w))
			{
				Synergy.LogError(
					"can't remove '" + w.Name + "' from layout, not found");

				return;
			}

			RemoveImpl(w);
		}

		public void DoLayout()
		{
			LayoutImpl();
		}

		public bool Contains(Widget w)
		{
			return children_.Contains(w);
		}

		protected virtual void AddImpl(Widget w, LayoutData data = null)
		{
			// no-op
		}

		protected virtual void RemoveImpl(Widget w)
		{
			// no-op
		}

		protected virtual Size GetPreferredSize()
		{
			return new Size(Widget.DontCare, Widget.DontCare);
		}

		protected abstract void LayoutImpl();
	}


	class HorizontalFlow : Layout
	{
		public HorizontalFlow(int spacing = 0)
		{
			Spacing = spacing;
		}

		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.Bounds);

			foreach (var w in Children)
			{
				var wr = new Rectangle(r.TopLeft, w.PreferredSize);
				w.Bounds = wr;
				r.Left += wr.Width + Spacing;
			}
		}

		protected override Size GetPreferredSize()
		{
			float totalWidth = 0;
			float tallest = 0;

			for (int i=0; i<Children.Count; ++i)
			{
				if (i > 0)
					totalWidth += Spacing;

				var ps = Children[i].PreferredSize;

				totalWidth += ps.width;
				tallest = Math.Max(tallest, ps.height);
			}

			return new Size(totalWidth, tallest);
		}
	}


	class VerticalFlow : Layout
	{
		public VerticalFlow(int spacing = 0)
		{
			Spacing = spacing;
		}

		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.Bounds);

			foreach (var w in Children)
			{
				var wr = new Rectangle(r.TopLeft, w.PreferredSize);
				w.Bounds = wr;
				r.Top += wr.Height + Spacing;
			}
		}

		protected override Size GetPreferredSize()
		{
			float totalHeight = 0;
			float widest = 0;

			for (int i = 0; i < Children.Count; ++i)
			{
				if (i > 0)
					totalHeight += Spacing;

				var ps = Children[i].PreferredSize;

				totalHeight += ps.height;
				widest = Math.Max(widest, ps.width);
			}

			return new Size(widest, totalHeight);
		}
	}
}
