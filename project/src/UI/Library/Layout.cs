using System;
using System.Collections.Generic;

namespace Synergy.UI
{
	interface LayoutData
	{
	}

	abstract class Layout
	{
		public abstract string TypeName { get; }

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

		public virtual float Spacing
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

		protected virtual void AddImpl(Widget w, LayoutData data)
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
		public override string TypeName { get { return "horflow"; } }

		public const int AlignTop = 0x01;
		public const int AlignVCenter = 0x02;
		public const int AlignBottom = 0x04;

		public const int AlignLeft = 0x08;
		public const int AlignCenter = 0x10;
		public const int AlignRight = 0x20;

		private int align_;

		public HorizontalFlow(int spacing = 0, int align = AlignLeft|AlignTop)
		{
			Spacing = spacing;
			align_ = align;
		}

		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.Bounds);

			var bounds = new List<Rectangle>();
			float totalWidth = 0;

			foreach (var w in Children)
			{
				if (totalWidth > 0)
					totalWidth += Spacing;

				var wr = new Rectangle(r.TopLeft, w.PreferredSize);

				if (wr.Height < r.Height)
				{
					if (Bits.IsSet(align_, AlignVCenter))
					{
						wr.MoveTo(wr.Left, r.Top + (r.Height / 2) - (wr.Height / 2));
					}
					else if (Bits.IsSet(align_, AlignBottom))
					{
						wr.MoveTo(wr.Left, r.Bottom - wr.Height);
					}
					else // AlignTop
					{
						// no-op
					}
				}

				bounds.Add(wr);
				totalWidth += wr.Width;
				r.Left += wr.Width + Spacing;
			}

			if (Bits.IsSet(align_, AlignCenter))
			{
				float offset = (Parent.Bounds.Width / 2) - (totalWidth / 2);
				foreach (var wr in bounds)
					wr.Translate(offset, 0);
			}
			else if (Bits.IsSet(align_, AlignRight))
			{
				float offset = Parent.Bounds.Width - totalWidth;
				foreach (var wr in bounds)
					wr.Translate(offset, 0);
			}
			else // left
			{
				// no-op
			}

			for (int i = 0; i < Children.Count; ++i)
				Children[i].Bounds = bounds[i];
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

				totalWidth += ps.Width;
				tallest = Math.Max(tallest, ps.Height);
			}

			return new Size(totalWidth, tallest);
		}
	}


	class VerticalFlow : Layout
	{
		public override string TypeName { get { return "verflow"; } }

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

				totalHeight += ps.Height;
				widest = Math.Max(widest, ps.Width);
			}

			return new Size(widest, totalHeight);
		}
	}
}
