using System.Collections.Generic;
using UnityEngine;

namespace Synergy.UI
{
	class Widget
	{
		private Widget parent_ = null;
		private readonly List<Widget> children_ = new List<Widget>();
		private Layout layout_ = null;
		private Rect bounds_ = new Rect();

		public Widget()
		{
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

		public void Add(Widget w, LayoutData d)
		{
			w.parent_ = this;
			children_.Add(w);
			layout_?.Add(w, d);
		}

		public void DoLayout()
		{
			layout_?.DoLayout();
		}

		public void Create()
		{
			DoCreate();

			foreach (var w in children_)
				w.Create();
		}

		public Size PreferredSize
		{
			get
			{
				return GetPreferredSize();
			}
		}

		protected virtual void DoCreate()
		{
			// no-op
		}

		protected virtual Size GetPreferredSize()
		{
			return new Size(-1, -1);
		}

		public Rect Bounds
		{
			get { return bounds_; }
			set { bounds_ = value; }
		}

		public virtual string DebugLine
		{
			get
			{
				return TypeName;
			}
		}

		public virtual string TypeName
		{
			get
			{
				return "widget";
			}
		}
	}
}
