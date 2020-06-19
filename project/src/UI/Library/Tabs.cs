using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Stack : Widget
	{
		public override string TypeName { get { return "stack"; } }

		private readonly List<Widget> widgets_ = new List<Widget>();
		private int selection_ = -1;

		public Stack()
		{
			Layout = new BorderLayout();
		}

		public void AddToStack(Widget w)
		{
			w.Visible = false;
			widgets_.Add(w);
			Add(w, BorderLayout.Center);
		}

		public void Select(int sel)
		{
			if (sel < 0 || sel >= widgets_.Count)
				sel = -1;

			selection_ = sel;

			for (int i = 0; i < widgets_.Count; ++i)
				widgets_[i].Visible = (i == selection_);
		}
	}


	class Tabs : Widget
	{
		public override string TypeName { get { return "tabs"; } }

		class Tab
		{
			private readonly Tabs tabs_;
			private readonly Button button_;
			private readonly Panel panel_;
			private readonly Widget widget_;

			public Tab(Tabs tabs, string text, Widget w)
			{
				tabs_ = tabs;
				button_ = new Button(text);
				panel_ = new Panel();
				widget_ = w;

				panel_.Layout = new BorderLayout();
				panel_.Add(widget_, BorderLayout.Center);

				button_.Alignment = Label.AlignCenter | Label.AlignBottom;
				button_.Clicked += () => { tabs_.Select(this); };
			}

			public Button Button
			{
				get { return button_; }
			}

			public Widget Panel
			{
				get { return panel_; }
			}

			public void SetSelected(bool b)
			{
				button_.MinimumSize = new Size(DontCare, b ? 50 : 40);
			}
		}

		private readonly Panel top_ = new Panel();
		private readonly Stack stack_ = new Stack();
		private readonly List<Tab> tabs_ = new List<Tab>();

		public Tabs()
		{
			Layout = new BorderLayout();

			Add(top_, BorderLayout.Top);
			Add(stack_, BorderLayout.Center);

			top_.Layout = new HorizontalFlow(0, HorizontalFlow.AlignBottom);
			stack_.Layout = new BorderLayout();
			stack_.Borders = new Insets(2);
			stack_.Padding = new Insets(20);
		}

		public void AddTab(string text, Widget w)
		{
			var t = new Tab(this, text, w);
			tabs_.Add(t);

			top_.Add(t.Button);
			stack_.AddToStack(t.Panel);

			Select(tabs_[0]);
		}

		public void Select(int i)
		{
			if (i < 0 || i >= tabs_.Count)
				return;

			Select(tabs_[i]);
		}

		private void Select(Tab t)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i] == t)
				{
					stack_.Select(i);
					tabs_[i].SetSelected(true);
				}
				else
				{
					tabs_[i].SetSelected(false);
				}
			}
		}
	}
}
