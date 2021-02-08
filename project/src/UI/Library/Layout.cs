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

		public const float DontCare = -1;

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


	abstract class FlowLayout : Layout
	{
		public const int AlignTop = 0x01;
		public const int AlignVCenter = 0x02;
		public const int AlignBottom = 0x04;

		public const int AlignLeft = 0x08;
		public const int AlignCenter = 0x10;
		public const int AlignRight = 0x20;

		private int align_;
		private bool expand_;

		public FlowLayout(int spacing, int align, bool expand)
		{
			Spacing = spacing;
			expand_ = expand;
			align_ = align;
		}

		public bool Expand
		{
			get { return expand_; }
			set { expand_ = value; }
		}

		public int Alignment
		{
			get { return align_; }
			set { align_ = value; }
		}
	}


	class HorizontalFlow : FlowLayout
	{
		public override string TypeName { get { return "horflow"; } }

		public HorizontalFlow(int spacing = 0, int align = AlignLeft|AlignVCenter)
			: base(spacing, align, false)
		{
		}

		protected override void LayoutImpl()
		{
			var av = Parent.AbsoluteClientBounds;
			var r = av;

			var bounds = new List<Rectangle?>();
			float totalWidth = 0;

			foreach (var w in Children)
			{
				if (!w.Visible)
				{
					bounds.Add(null);
					continue;
				}

				if (totalWidth > 0)
					totalWidth += Spacing;

				var wr = new Rectangle(
					r.TopLeft, w.GetRealPreferredSize(r.Width, r.Height));

				if (Expand)
				{
					wr.Height = r.Height;
				}
				else if (wr.Height < r.Height)
				{
					if (Bits.IsSet(Alignment, AlignVCenter))
					{
						wr.MoveTo(wr.Left, r.Top + (r.Height / 2) - (wr.Height / 2));
					}
					else if (Bits.IsSet(Alignment, AlignBottom))
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

			if (totalWidth > av.Width)
			{
				var excess = totalWidth - av.Width;
				float offset = 0;

				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] == null)
						continue;

					var b = bounds[i].Value;

					b.Translate(-offset, 0);

					if (excess > 0)
					{
						var ms = Children[i].GetRealMinimumSize();

						if (b.Width > ms.Width)
						{
							var d = Math.Min(b.Width - ms.Width, excess);

							b.Width -= d;
							excess -= d;
							offset += d;
							totalWidth -= d;
						}
					}

					bounds[i] = b;
				}
			}

			if (Bits.IsSet(Alignment, AlignCenter))
			{
				float offset = (Parent.Bounds.Width / 2) - (totalWidth / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(offset, 0);
				}
			}
			else if (Bits.IsSet(Alignment, AlignRight))
			{
				float offset = Parent.Bounds.Width - totalWidth;
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(offset, 0);
				}
			}
			else // left
			{
				// no-op
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				if (bounds[i] != null)
					Children[i].Bounds = bounds[i].Value;
			}
		}

		protected override Size GetPreferredSize()
		{
			float totalWidth = 0;
			float tallest = 0;

			for (int i=0; i<Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalWidth += Spacing;

				var ps = w.GetRealPreferredSize(DontCare, DontCare);

				totalWidth += ps.Width;
				tallest = Math.Max(tallest, ps.Height);
			}

			return new Size(totalWidth, tallest);
		}
	}


	class VerticalFlow : FlowLayout
	{
		public override string TypeName { get { return "verflow"; } }

		public VerticalFlow(int spacing = 0, bool expand = true)
			: base(spacing, AlignLeft | AlignTop, expand)
		{
		}

		protected override void LayoutImpl()
		{
			var av = Parent.AbsoluteClientBounds;
			var r = av;

			var bounds = new List<Rectangle?>();
			float totalHeight = 0;

			foreach (var w in Children)
			{
				if (!w.Visible)
				{
					bounds.Add(null);
					continue;
				}

				if (totalHeight > 0)
					totalHeight += Spacing;

				var wr = new Rectangle(
					r.TopLeft, w.GetRealPreferredSize(r.Width, r.Height));

				if (Expand)
				{
					wr.Width = r.Width;
				}
				else if (wr.Width < r.Width)
				{
					if (Bits.IsSet(Alignment, AlignCenter))
					{
						wr.MoveTo(r.Left + (r.Width / 2) - (wr.Width / 2), wr.Top);
					}
					else if (Bits.IsSet(Alignment, AlignRight))
					{
						wr.MoveTo(r.Right - wr.Width, wr.Top);
					}
					else // AlignLeft
					{
						// no-op
					}
				}

				bounds.Add(wr);
				totalHeight += wr.Height;
				r.Top += wr.Height + Spacing;
			}

			if (totalHeight > Parent.Bounds.Height)
			{
				var excess = totalHeight - Parent.Bounds.Height;
				float offset = 0;

				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] == null)
						continue;

					var b = bounds[i].Value;

					b.Translate(0, -offset);

					if (excess > 0)
					{
						var ms = Children[i].GetRealMinimumSize();

						if (b.Height > ms.Height)
						{
							var d = Math.Min(b.Height - ms.Height, excess);

							b.Height -= d;
							excess -= d;
							offset += d;
							totalHeight -= d;
						}
					}

				}
			}

			if (Bits.IsSet(Alignment, AlignVCenter))
			{
				float offset = (Parent.Bounds.Height / 2) - (totalHeight / 2);
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(0, offset);
				}
			}
			else if (Bits.IsSet(Alignment, AlignBottom))
			{
				float offset = Parent.Bounds.Height - totalHeight;
				for (int i = 0; i < bounds.Count; ++i)
				{
					if (bounds[i] != null)
						bounds[i] = bounds[i].Value.TranslateCopy(0, offset);
				}
			}
			else // left
			{
				// no-op
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				if (bounds[i] != null)
					Children[i].Bounds = bounds[i].Value;
			}
		}

		protected override Size GetPreferredSize()
		{
			float totalHeight = 0;
			float widest = 0;

			for (int i = 0; i < Children.Count; ++i)
			{
				var w = Children[i];
				if (!w.Visible)
					continue;

				if (i > 0)
					totalHeight += Spacing;

				var ps = w.GetRealPreferredSize(DontCare, DontCare);

				totalHeight += ps.Height;
				widest = Math.Max(widest, ps.Width);
			}

			return new Size(widest, totalHeight);
		}
	}
}
