﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class ListView<ItemType> : TypedListImpl<ItemType>
		where ItemType : class
	{
		public override string TypeName { get { return "list"; } }

		public ListView(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ListView(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ListView(List<ItemType> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
			Borders = new Insets(2);
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableScrollablePopupPrefab)
					.gameObject;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			Popup.popup.alwaysOpen = true;
			Popup.popup.topButton.gameObject.SetActive(false);
			Popup.popup.backgroundImage.gameObject.SetActive(false);
			Popup.popup.onValueChangeHandlers += (string s) => { Root.SetFocus(this); };
			Popup.popup.topBottomBuffer = 3;

			Style.Setup(this);
		}

		protected override void Polish()
		{
			base.Polish();
			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			var rt = Popup.popup.popupPanel;
			rt.offsetMin = new Vector2(ClientBounds.Left, ClientBounds.Top);
			rt.offsetMax = new Vector2(ClientBounds.Right, ClientBounds.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(ClientBounds.Center.X, 0);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(300, 200);
		}
	}
}
