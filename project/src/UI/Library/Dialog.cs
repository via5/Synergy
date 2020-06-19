using System;
using System.Reflection.Emit;
using UnityEngine;

namespace Synergy.UI
{
	class Dialog : Panel
	{
		public delegate void CloseHandler();
		public event CloseHandler Closed;

		private readonly Root root_;
		private readonly Label title_;
		private readonly Panel content_;

		public Dialog(Root r, string title)
		{
			root_ = r;
			title_ = new Label(title, Label.AlignCenter | Label.AlignVCenter);
			content_ = new Panel();

			BackgroundColor = Style.BackgroundColor;
			Layout = new BorderLayout();
			Borders = new Insets(1);
			content_.Margins = new Insets(10, 20, 10, 10);

			Add(title_, BorderLayout.Top);
			Add(content_, BorderLayout.Center);

			MinimumSize = new Size(600, 200);
		}

		public Widget ContentPanel
		{
			get { return content_; }
		}

		public void RunDialog(CloseHandler h = null)
		{
			root_.OverlayVisible = true;

			var s = MinimumSize;

			Bounds = new Rectangle(
				root_.Bounds.Center.X - (s.Width / 2),
				root_.Bounds.Center.Y - (s.Height / 2),
				s);

			DoLayout();
			Create();
			UpdateBounds();
			BringToTop();

			if (h != null)
				Closed += h;
		}

		public void CloseDialog()
		{
			root_.OverlayVisible = false;
			Destroy();

			Closed?.Invoke();
		}
	}


	class ButtonBox : Panel
	{
		public delegate void ButtonCallback(int id);
		public event ButtonCallback ButtonClicked;

		// sync with MessageDialog
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;

		public ButtonBox(int buttons)
		{
			Layout = new HorizontalFlow(10, HorizontalFlow.AlignRight);

			AddButton(buttons, OK, S("OK"));
			AddButton(buttons, Cancel, S("Cancel"));
			AddButton(buttons, Yes, S("Yes"));
			AddButton(buttons, No, S("No"));
			AddButton(buttons, Apply, S("Apply"));
			AddButton(buttons, Close, S("Close"));
		}

		private void AddButton(int buttons, int id, string text)
		{
			if (!Bits.IsSet(buttons, id))
				return;

			Add(new UI.Button(text, () => OnButton(id)));
		}

		private void OnButton(int id)
		{
			ButtonClicked?.Invoke(id);
		}
	}


	class MessageDialog : Dialog
	{
		// sync with ButtonBox
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;

		private ButtonBox buttons_;
		private int button_ = -1;

		public MessageDialog(Root r, string title, string text)
			: base(r, title)
		{
			buttons_ = new ButtonBox(OK | Cancel);
			buttons_.ButtonClicked += OnButtonClicked;

			ContentPanel.Layout = new BorderLayout();
			ContentPanel.Add(
				new UI.Label(text, UI.Label.AlignLeft | UI.Label.AlignTop),
				BorderLayout.Center);

			ContentPanel.Add(buttons_, BorderLayout.Bottom);
		}

		public int Button
		{
			get { return button_; }
		}

		private void OnButtonClicked(int id)
		{
			button_ = id;
			CloseDialog();
		}
	}
}
