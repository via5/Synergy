using System;
using UnityEngine;

namespace Synergy.UI
{
	class Dialog : Panel
	{
		private readonly Root root_;

		public Dialog(Root r)
		{
			root_ = r;
			//root_.Add(this);
			BackgroundColor = Color.red;
		}

		public int Run()
		{
			root_.OverlayVisible = true;

			Bounds = new Rectangle(300, 300, new Size(400, 400));

			DoLayout();
			Create();
			UpdateBounds();
			WidgetObject.transform.SetAsLastSibling();

			GetRoot().Dump();

			return 0;
		}
	}
}
