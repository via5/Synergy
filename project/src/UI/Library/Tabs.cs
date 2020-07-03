﻿using System.Collections.Generic;
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

		public int Selected
		{
			get { return selection_; }
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
			private bool selected_ = false;

			public Tab(Tabs tabs, string text, Widget w)
			{
				tabs_ = tabs;
				button_ = new Button(text);
				panel_ = new Panel();
				widget_ = w;

				panel_.Layout = new BorderLayout();
				panel_.Add(widget_, BorderLayout.Center);

				button_.Alignment = Label.AlignCenter | Label.AlignBottom;
				button_.Clicked += () => { tabs_.SelectImpl(this); };
			}

			public Button Button
			{
				get { return button_; }
			}

			public Widget Panel
			{
				get { return panel_; }
			}

			public Widget Widget
			{
				get { return widget_; }
			}

			public bool Selected
			{
				get { return selected_; }
			}

			public void SetSelected(bool b)
			{
				selected_ = b;
				button_.MinimumSize = new Size(DontCare, b ? 50 : 40);
			}
		}


		public delegate void SelectionCallback(int index);
		public event SelectionCallback SelectionChanged;


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

		public int Selected
		{
			get
			{
				for (int i = 0; i < tabs_.Count; ++i)
				{
					if (tabs_[i].Selected)
						return i;
				}

				return -1;
			}
		}

		public Widget SelectedWidget
		{
			get
			{
				var i = Selected;
				if (i < 0 || i >= tabs_.Count)
					return null;

				return tabs_[i].Widget;
			}
		}

		public void AddTab(string text, Widget w)
		{
			var t = new Tab(this, text, w);
			tabs_.Add(t);

			top_.Add(t.Button);
			stack_.AddToStack(t.Panel);

			SelectImpl(tabs_[0]);
		}

		public void Select(int i)
		{
			if (i < 0 || i >= tabs_.Count)
				SelectImpl(null);
			else
				SelectImpl(tabs_[i]);
		}

		public void Select(Widget w)
		{
			var i = IndexOfWidget(w);
			if (i == -1)
			{
				Synergy.LogError("Select: widget not found");
				return;
			}

			Select(i);
		}

		public void SetTabVisible(int i, bool b)
		{
			if (i < 0 || i >= tabs_.Count)
				return;


			if (!b && i == Selected)
			{
				if (i < (tabs_.Count - 1))
					Select(i + 1);
				else if (i > 0)
					Select(i - 1);
				else
					Select(-1);
			}

			tabs_[i].Button.Visible = b;
		}

		public void SetTabVisible(Widget w, bool b)
		{
			var i = IndexOfWidget(w);
			if (i == -1)
			{
				Synergy.LogError("SetTabVisible: widget not found");
				return;
			}

			SetTabVisible(i, b);
		}

		public int IndexOfWidget(Widget w)
		{
			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i].Widget == w)
					return i;
			}

			return -1;
		}

		private void SelectImpl(Tab t)
		{
			int sel = -1;

			for (int i = 0; i < tabs_.Count; ++i)
			{
				if (tabs_[i] == t)
				{
					sel = i;
					stack_.Select(i);
					tabs_[i].SetSelected(true);
				}
				else
				{
					tabs_[i].SetSelected(false);
				}
			}

			SelectionChanged?.Invoke(sel);
		}
	}
}
