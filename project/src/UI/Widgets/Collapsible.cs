using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy
{
	class Collapsible : CompoundWidget
	{
		public delegate void ToggledCallback(bool expanded);

		public bool Expanded { get; private set; } = false;

		private readonly List<IWidget> widgets_ = new List<IWidget>();
		private readonly Button button_;

		private readonly ToggledCallback callback_;
		private string buttonText_;

		public Collapsible(
			string buttonText, ToggledCallback callback = null, int flags = 0)
				: base(flags)
		{
			callback_ = callback;
			buttonText_ = buttonText;

			button_ = new Button("", OnToggle, flags);
			UpdateButton();
		}

		public int Count
		{
			get
			{
				return widgets_.Count;
			}
		}

		public void Add(IWidget w)
		{
			widgets_.Add(w);
		}

		public void Add(List<IWidget> list)
		{
			widgets_.AddRange(list);
		}

		public void Remove(IWidget w)
		{
			w.RemoveFromUI();
			widgets_.Remove(w);
		}

		public void Clear()
		{
			foreach (var w in widgets_)
				w.RemoveFromUI();

			widgets_.Clear();
		}

		protected override void DoAddToUI()
		{
			RemoveFromUI();

			button_.AddToUI();

			if (Expanded)
			{
				button_.BackgroundColor = Color.green;

				foreach (var w in widgets_)
					w.AddToUI();
			}
			else
			{
				button_.BackgroundColor = Utilities.DefaultButtonColor;
			}
		}

		protected override void DoRemoveFromUI()
		{
			button_.RemoveFromUI();

			foreach (var w in widgets_)
				w.RemoveFromUI();
		}

		public override Selectable GetSelectable()
		{
			return null;
		}

		public string Text
		{
			get
			{
				return buttonText_;
			}

			set
			{
				buttonText_ = value;
				UpdateButton();
			}
		}

		private void OnToggle()
		{
			Expanded = !Expanded;
			callback_?.Invoke(Expanded);
			UpdateButton();

			sc_.UI.NeedsReset(
				"collapsible " + Text + " " +
				(Expanded ? "expanded" : "collapsed"));
		}

		private void UpdateButton()
		{
			if (Expanded)
				button_.Text = "\u2191 " + buttonText_;
			else
				button_.Text = "\u2193 " + buttonText_;
		}
	}
}
