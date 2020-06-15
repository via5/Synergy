using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class ListView : Widget
	{
		public override string TypeName { get { return "list"; } }

		private UIDynamicPopup popup_ = null;
		private JSONStorableStringChooser storable_ =
			new JSONStorableStringChooser("", new List<string>(), "", "");

		public ListView()
		{
			Borders = new Insets(1);
		}

		public List<string> Items
		{
			set
			{
				storable_.choices = value;
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableScrollablePopupPrefab)
					.gameObject;
		}

		protected override void DoCreate()
		{
			popup_ = Object.GetComponent<UIDynamicPopup>();
			popup_.popup.alwaysOpen = true;
			popup_.popup.showSlider = false;
			popup_.popup.topButton.gameObject.SetActive(false);
			popup_.popup.labelText.gameObject.SetActive(false);
			popup_.popup.backgroundImage.gameObject.SetActive(false);
			popup_.popup.onValueChangeHandlers += (string s) => { Root.SetFocus(this); };
			popup_.popup.topBottomBuffer = 3;

			Style.Polish(popup_);

			var rt = popup_.popup.popupPanel;
			rt.offsetMin = new Vector2(ClientBounds.Left, ClientBounds.Top);
			rt.offsetMax = new Vector2(ClientBounds.Right, ClientBounds.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(ClientBounds.Center.X, 0);

			rt = popup_.popup.popupButtonPrefab;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 3, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 15);

			var text = popup_.popup.popupButtonPrefab.GetComponentInChildren<Text>();
			text.alignment = TextAnchor.MiddleLeft;
			text.rectTransform.offsetMin = new Vector2(
				text.rectTransform.offsetMin.x + 10,
				text.rectTransform.offsetMin.y);

			storable_.popup = popup_.popup;
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
