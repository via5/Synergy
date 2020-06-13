﻿using System;
using System.Collections.Generic;

namespace Synergy.UI
{
	class GridLayout : Layout
	{
		public override string TypeName { get { return "gl"; } }


		public class Data : LayoutData
		{
			public int col, row;

			public Data(int col, int row)
			{
				this.col = col;
				this.row = row;
			}
		}


		class RowData<T>
			where T : new()
		{
			private readonly List<T> cells_ = new List<T>();

			public int Count
			{
				get { return cells_.Count; }
			}

			public T Cell(int i)
			{
				return cells_[i];
			}

			public void Set(int i, T t)
			{
				cells_[i] = t;
			}

			public void Extend(int cols)
			{
				while (cells_.Count < cols)
					cells_.Add(new T());
			}
		}


		class CellData<T>
			where T : new()
		{
			private readonly List<RowData<T>> data_ = new List<RowData<T>>();

			public CellData()
			{
			}

			public CellData(int cols, int rows)
			{
				Extend(cols, rows);
			}

			public int RowCount
			{
				get { return data_.Count; }
			}

			public int ColumnCount
			{
				get
				{
					if (data_.Count == 0)
						return 0;
					else
						return data_[0].Count;
				}
			}

			public RowData<T> Row(int row)
			{
				return data_[row];
			}

			public T Cell(int col, int row)
			{
				return Row(row).Cell(col);
			}

			public void Set(int col, int row, T t)
			{
				Row(row).Set(col, t);
			}

			public void Extend(int cols, int rows)
			{
				cols = Math.Max(cols, ColumnCount);

				while (data_.Count < rows)
					data_.Add(new RowData<T>());

				foreach (var row in data_)
					row.Extend(cols);
			}
		}


		private readonly CellData<List<Widget>> widgets_ =
			new CellData<List<Widget>>();

		private float hspacing_ = 0;
		private float vspacing_ = 0;
		private bool uniformHeight_ = true;
		private int nextCol_ = 0;
		private int nextRow_ = 0;

		public GridLayout()
		{
		}

		public GridLayout(int cols)
		{
			widgets_.Extend(cols, 1);
		}

		public static Data P(int col, int row)
		{
			return new Data(col, row);
		}

		public override float Spacing
		{
			get
			{
				return base.Spacing;
			}

			set
			{
				hspacing_ = value;
				vspacing_ = value;
			}
		}


		public float HorizontalSpacing
		{
			get { return hspacing_; }
			set { hspacing_ = value; }
		}

		public float VerticalSpacing
		{
			get { return vspacing_; }
			set { vspacing_ = value; }
		}

		protected override void AddImpl(Widget w, LayoutData data)
		{
			var d = data as Data;
			if (d == null)
			{
				d = new Data(nextCol_, nextRow_);

				++nextCol_;
				if (nextCol_ >= widgets_.ColumnCount)
				{
					nextCol_ = 0;
					++nextRow_;
				}
			}

			if (d.row < 0)
			{
				Synergy.LogError("gridlayout: bad row");
				return;
			}

			if (d.col < 0)
			{
				Synergy.LogError("gridlayout: bad col");
				return;
			}

			Synergy.LogError(d.col.ToString() + " " + d.row.ToString());

			widgets_.Extend(d.col + 1, d.row + 1);
			widgets_.Cell(d.col, d.row).Add(w);
		}

		protected override void LayoutImpl()
		{
			var r = new Rectangle(Parent.Bounds);
			var d = GetCellPreferredSizes();

			float xfactor = 1;//.Width / d.ps.Width;
			float yfactor = 1;//r.Height / d.ps.Height;

			float x = r.Left;
			float y = r.Top;

			for (int rowIndex = 0; rowIndex < widgets_.RowCount; ++rowIndex)
			{
				float tallestInRow = 0;

				for (int colIndex = 0; colIndex < widgets_.ColumnCount; ++colIndex)
				{
					var ws = widgets_.Cell(colIndex, rowIndex);
					var ps = d.sizes.Cell(colIndex, rowIndex);
					var uniformWidth = d.widths[colIndex];

					var ww = Math.Min(ps.Width, uniformWidth);
					var wh = ps.Height;

					var wr = Rectangle.FromSize(
						x, y, (ww * xfactor), (wh * yfactor));

					foreach (var w in ws)
						w.Bounds = wr;

					x += uniformWidth + HorizontalSpacing;
					tallestInRow = Math.Max(tallestInRow, wr.Height);
				}

				x = r.Left;

				if (uniformHeight_)
					y += d.tallest + VerticalSpacing;
				else
					y += tallestInRow + VerticalSpacing;
			}
		}


		struct PreferredSizesData
		{
			public Size ps;
			public List<float> widths;
			public CellData<Size> sizes;
			public float tallest;

			public PreferredSizesData(int cols, int rows)
			{
				ps = new Size();
				widths = new List<float>();
				for (int i = 0; i < cols; ++i)
					widths.Add(0);
				sizes = new CellData<Size>(cols, rows);
				tallest = 0;
			}
		}


		private PreferredSizesData GetCellPreferredSizes()
		{
			var d = new PreferredSizesData(widgets_.ColumnCount, widgets_.RowCount);

			for (int rowIndex = 0; rowIndex < widgets_.RowCount; ++rowIndex)
			{
				var row = widgets_.Row(rowIndex);

				float width = 0;
				float tallestInRow = 0;

				for (int colIndex = 0; colIndex < row.Count; ++colIndex)
				{
					var cell = row.Cell(colIndex);
					var cellPs = new Size(Widget.DontCare, Widget.DontCare);

					foreach (var w in cell)
					{
						var ps = w.PreferredSize;
						cellPs.Width = Math.Max(cellPs.Width, ps.Width);
						cellPs.Height = Math.Max(cellPs.Height, ps.Height);
					}

					d.sizes.Set(colIndex, rowIndex, cellPs);
					d.widths[colIndex] = Math.Max(d.widths[colIndex], cellPs.Width);

					if (colIndex > 0)
						width += HorizontalSpacing;

					width += cellPs.Width;
					tallestInRow = Math.Max(tallestInRow, cellPs.Height);
				}

				if (rowIndex > 0)
					d.ps.Height += vspacing_;

				d.ps.Width = Math.Max(d.ps.Width, width);
				d.ps.Height += tallestInRow;
				d.tallest = Math.Max(d.tallest, tallestInRow);
			}

			if (uniformHeight_)
			{
				d.ps.Height = 0;

				for (int i = 0; i < widgets_.RowCount; ++i)
				{
					if (i > 0)
						d.ps.Height += vspacing_;

					d.ps.Height += d.tallest;
				}
			}

			return d;
		}

		protected override Size GetPreferredSize()
		{
			return GetCellPreferredSizes().ps;
		}
	}
}