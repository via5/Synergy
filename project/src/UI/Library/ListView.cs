using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class ListView : Widget
	{
		public override string TypeName { get { return "list"; } }

		private UIDynamicPopup popup_ = null;

		public ListView()
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
			var storable = new JSONStorableStringChooser(
				"", new List<string>(), "", "");

			popup_ = Object.GetComponent<UIDynamicPopup>();
			popup_.popup.alwaysOpen = true;
			popup_.popup.showSlider = false;
			popup_.popup.topButton.gameObject.SetActive(false);
			popup_.popup.labelText.gameObject.SetActive(false);
			popup_.popup.backgroundImage.gameObject.SetActive(false);
			popup_.popup.selectColor = new Color(0.55f, 0.55f, 0.55f);

			var rt = popup_.popup.popupPanel;
			rt.offsetMin = new Vector2(ClientBounds.Left, ClientBounds.Top);
			rt.offsetMax = new Vector2(ClientBounds.Right, ClientBounds.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(ClientBounds.Center.X, 0);

			storable.popup = popup_.popup;
			storable.choices = new List<string>() { "RT X head Person", "b", "c", "d" };
		}

		protected override Size GetPreferredSize()
		{
			return new Size(300, 200);
		}

		protected override void UpdateVisibility(bool b)
		{
			base.UpdateVisibility(b);

			if (popup_ != null)
				popup_.popup.popupPanel.gameObject.SetActive(b);
		}
	}
}
