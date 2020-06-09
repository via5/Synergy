﻿using System;
using System.Collections.Generic;

namespace Synergy.UI
{
	class BorderLayout : Layout
	{
		public class Data : LayoutData
		{
			public int side;

			public Data(int s)
			{
				side = s;
			}
		}

		public static Data Left = new Data(0);
		public static Data Top = new Data(1);
		public static Data Right = new Data(2);
		public static Data Bottom = new Data(3);
		public static Data Center = new Data(4);
		public static Data DefaultSide = Center;

		private const int LeftSide = 0;
		private const int TopSide = 1;
		private const int RightSide = 2;
		private const int BottomSide = 3;
		private const int CenterSide = 4;

		private const int TopLeft = 0;
		private const int TopRight = 1;
		private const int BottomLeft = 2;
		private const int BottomRight = 3;


		private List<Widget>[] sides_ = new List<Widget>[5];
		private int[] corners_ = new int[4];

		public BorderLayout()
		{
			for (int i = 0; i < 5; ++i)
				sides_[i] = new List<Widget>();

			corners_[TopLeft] = TopSide;
			corners_[TopRight] = TopSide;
			corners_[BottomLeft] = BottomSide;
			corners_[BottomRight] = BottomSide;
		}

		public override void Add(Widget w, LayoutData data = null)
		{
			var d = data as Data;
			if (d == null)
				d = DefaultSide;

			if (d.side < 0 || d.side > 5)
			{
				Synergy.LogError(
					"bad border layout side " + d.side.ToString());

				return;
			}

			if (Contains(w))
			{
				Synergy.LogError(
					"border layout already has widget " + w.DebugLine);

				return;
			}

			var s = sides_[d.side];
			s.Add(w);
		}

		public bool Contains(Widget w)
		{
			foreach (var s in sides_)
			{
				if (s.Contains(w))
					return true;
			}

			return false;
		}

		public override void DoLayout()
		{
			Rectangle av = new Rectangle(Parent.Bounds);
			Rectangle center = new Rectangle(av);

			center.top += DoTop(av);
			center.bottom -= DoBottom(av);
			center.left += DoLeft(av);
			center.right -= DoRight(av);

			DoCenter(center);
		}

		private float DoTop(Rectangle av)
		{
			float tallest = 0;

			foreach (var w in sides_[TopSide])
			{
				float wh = w.PreferredSize.height;
				tallest = Math.Max(tallest, wh);

				Rectangle r = new Rectangle();

				r.left = av.left;
				r.top = av.top;
				r.right = r.left + av.width;
				r.bottom = r.top + wh;

				var lw = SideWidth(LeftSide);
				var rw = SideWidth(RightSide);

				if (corners_[TopLeft] != TopSide)
					r.left += lw;

				if (corners_[TopRight] != TopSide)
					r.right -= rw;

				w.Bounds = r;
			}

			return tallest;
		}

		private float DoBottom(Rectangle av)
		{
			float tallest = 0;

			foreach (var w in sides_[BottomSide])
			{
				float wh = w.PreferredSize.height;
				tallest = Math.Max(tallest, wh);

				Rectangle r = new Rectangle();

				r.left = av.left;
				r.top = av.bottom - wh;
				r.right = r.left + av.width;
				r.bottom = r.top + wh;

				var lw = SideWidth(LeftSide);
				var rw = SideWidth(RightSide);

				if (corners_[BottomLeft] != BottomSide)
					r.left += lw;

				if (corners_[BottomRight] != BottomSide)
					r.right -= rw;

				w.Bounds = r;
			}

			return tallest;
		}

		private float DoLeft(Rectangle av)
		{
			float widest = 0;

			foreach (var w in sides_[LeftSide])
			{
				float ww = w.PreferredSize.width;
				widest = Math.Max(widest, ww);

				Rectangle r = new Rectangle();

				r.left = av.left;
				r.top = av.top;

				r.right = r.left + ww;
				r.bottom = r.top + av.height;

				var th = SideHeight(TopSide);
				var bh = SideHeight(BottomSide);

				if (corners_[TopLeft] != LeftSide)
					r.top += th;

				if (corners_[BottomLeft] != LeftSide)
					r.bottom -= bh;

				w.Bounds = r;
			}

			return widest;
		}

		private float DoRight(Rectangle av)
		{
			float widest = 0;

			foreach (var w in sides_[RightSide])
			{
				float ww = w.PreferredSize.width;
				widest = Math.Max(widest, ww);

				Rectangle r = new Rectangle();

				r.left = av.right - ww;
				r.top = av.top;

				r.right = r.left + ww;
				r.bottom = r.top + av.height;

				var th = SideHeight(TopSide);
				var bh = SideHeight(BottomSide);

				if (corners_[TopRight] != RightSide)
					r.top += th;

				if (corners_[BottomRight] != RightSide)
					r.bottom -= bh;

				w.Bounds = r;
			}

			return widest;
		}

		private void DoCenter(Rectangle av)
		{
			foreach (var w in sides_[CenterSide])
				w.Bounds = av;
		}

		private float SideWidth(int side)
		{
			float widest = 0;

			foreach (var w in sides_[side])
			{
				float ww = w.PreferredSize.width;
				widest = Math.Max(widest, ww);
			}

			return widest;
		}

		private float SideHeight(int side)
		{
			float tallest = 0;

			foreach (var w in sides_[side])
			{
				float wh = w.PreferredSize.height;
				tallest = Math.Max(tallest, wh);
			}

			return tallest;
		}
	}
}