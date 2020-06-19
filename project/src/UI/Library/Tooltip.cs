﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Tooltip
	{
		private string text_ = "";

		public Tooltip(string text = "")
		{
			text_ = text;
		}

		public string Text
		{
			get { return text_; }
			set { text_ = value; }
		}
	}


	class TooltipWidget : Panel
	{
		private readonly Label label_;

		public TooltipWidget()
		{
			Layout = new BorderLayout();
			Borders = new Insets(1);
			BackgroundColor = Style.BackgroundColor;
			Padding = new Insets(5);
			Visible = false;

			label_ = new Label();
			Add(label_);

			Tooltip.Text = "";
			label_.Tooltip.Text = "";
		}

		public override void Create()
		{
			base.Create();

			foreach (var c in MainObject.GetComponentsInChildren<Graphic>())
				c.raycastTarget = false;
		}

		public string Text
		{
			get { return label_.Text; }
			set { label_.Text = value; }
		}
	}


	class TooltipManager
	{
		private readonly Root root_;
		private readonly TooltipWidget widget_ = new TooltipWidget();
		private Timer timer_ = null;
		private Widget active_ = null;

		public TooltipManager(Root root)
		{
			root_ = root;
			root_.FloatingPanel.Add(widget_);
		}

		public void WidgetEntered(Widget w)
		{
			if (active_ == w)
				return;

			if (w.Tooltip.Text != "")
			{
				Hide();
				active_ = w;

				timer_ = Synergy.Instance.CreateTimer(Metrics.TooltipDelay, () =>
				{
					timer_ = null;
					Show();
				});
			}
		}

		public void WidgetExited(Widget w)
		{
			if (active_ == w)
				Hide();
		}

		private void Show()
		{
			widget_.Text = active_.Tooltip.Text;

			// size of text
			var size = Root.FitText(widget_.Text, Metrics.MaxTooltipWidth);

			// widget is size of text plus its insets
			size += widget_.Insets.Size;

			// current mouse position
			var mp = root_.MousePosition;

			// preferred position is just below the cursor
			var p = new Point(mp.X, mp.Y + Metrics.CursorHeight);

			// available rectangle, offset from the edges
			var av = root_.Bounds.DeflateCopy(Metrics.TooltipBorderOffset);


			if (p.X + size.Width >= av.Width)
			{
				// tooltip would extend past the right edge
				p.X = av.Width - size.Width;
			}

			if (p.Y + size.Height >= av.Height)
			{
				// tooltip would extend past the bottom edge; make sure it's
				// above the mouse cursor
				p.Y = mp.Y - size.Height;
			}

			widget_.Bounds = new Rectangle(p.X, p.Y, size);
			widget_.Visible = true;
		}

		private void Hide()
		{
			if (timer_ != null)
			{
				timer_.Destroy();
				timer_ = null;
			}

			widget_.Visible = false;
			active_ = null;
		}
	}
}
