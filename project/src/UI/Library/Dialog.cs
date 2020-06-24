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

		public virtual Widget ContentPanel
		{
			get { return content_; }
		}

		public void RunDialog(CloseHandler h = null)
		{
			root_.OverlayVisible = true;

			var ps = GetPreferredSize(root_.Bounds.Width, root_.Bounds.Height);
			Bounds = new Rectangle(
				root_.Bounds.Center.X - (ps.Width / 2),
				root_.Bounds.Center.Y - (ps.Height / 2),
				ps);

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


	class DialogWithButtons : Dialog
	{
		// sync with ButtonBox
		public const int OK     = 0x01;
		public const int Cancel = 0x02;
		public const int Yes    = 0x04;
		public const int No     = 0x08;
		public const int Close  = 0x10;
		public const int Apply  = 0x20;

		private readonly ButtonBox buttons_;
		private readonly UI.Panel center_;
		private int button_ = -1;

		public DialogWithButtons(Root r, int buttons, string title)
			: base(r, title)
		{
			buttons_ = new ButtonBox(buttons);
			buttons_.ButtonClicked += OnButtonClicked;

			center_ = new UI.Panel(new BorderLayout());

			base.ContentPanel.Layout = new BorderLayout();
			base.ContentPanel.Add(center_, BorderLayout.Center);
			base.ContentPanel.Add(buttons_, BorderLayout.Bottom);

			center_.Margins = new Insets(0, 0, 0, 20);
		}

		public override Widget ContentPanel
		{
			get { return center_; }
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


	class MessageDialog : DialogWithButtons
	{
		public MessageDialog(Root r, int buttons, string title, string text)
			: base(r, buttons, title)
		{
			ContentPanel.Add(
				new UI.Label(text, UI.Label.AlignLeft | UI.Label.AlignTop),
				BorderLayout.Center);
		}
	}
}
