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

				timer_ = Synergy.Instance.CreateTimer(0.75f, () =>
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

			var p = root_.MousePosition;
			var ps = widget_.PreferredSize;

			widget_.Bounds = new Rectangle(p.X, p.Y + 45, ps);
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
		}
	}
}
