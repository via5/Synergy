﻿using System.Collections.Generic;

namespace Synergy.UI
{
	interface LayoutData
	{
	}

	abstract class Layout
	{
		private Widget parent_ = null;
		private readonly List<Widget> children_ = new List<Widget>();

		public Widget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public List<Widget> Children
		{
			get { return children_; }
		}

		public void Add(Widget w, LayoutData data = null)
		{
			if (Contains(w))
			{
				Synergy.LogError("layout already has widget " + w.DebugLine);
				return;
			}

			children_.Add(w);
			AddImpl(w, data);
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

		protected abstract void LayoutImpl();
	}


	class HorizontalFlow : Layout
	{
		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.Bounds);

			foreach (var w in Children)
			{
				var wr = new Rectangle(r.TopLeft, w.PreferredSize);
				w.Bounds = wr;
				r.Left += wr.Width;
			}
		}
	}
}
