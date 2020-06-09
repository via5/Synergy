namespace Synergy.UI
{
	interface LayoutData
	{
	}

	abstract class Layout
	{
		private Widget parent_ = null;

		public Widget Parent
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public abstract void Add(Widget w, LayoutData data = null);
		public abstract void DoLayout();
	}
}
