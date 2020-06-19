using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class TypedListView<ItemType> : TypedListImpl<ItemType>
		where ItemType : class
	{
		public override string TypeName { get { return "list"; } }

		public TypedListView()
		{
			Borders = new Insets(1);
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
			Popup.popup.labelText.gameObject.SetActive(false);
			Popup.popup.backgroundImage.gameObject.SetActive(false);
			Popup.popup.onValueChangeHandlers += (string s) => { Root.SetFocus(this); };
			Popup.popup.topBottomBuffer = 3;
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

		protected override Size GetPreferredSize()
		{
			return new Size(300, 200);
		}

		protected override void UpdateVisibility(bool b)
		{
			base.UpdateVisibility(b);

			if (Popup != null)
				Popup.popup.popupPanel.gameObject.SetActive(b);
		}
	}


	class ListView : TypedListView<string>
	{
	}
}
