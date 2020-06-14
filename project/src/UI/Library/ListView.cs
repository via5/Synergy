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

			var sv = Utilities.FindChildRecursive(
				popup_.gameObject, "Scroll View");
			var sr = sv.GetComponent<ScrollRect>();
			sr.elasticity = 0;
			sr.inertia = false;
			sr.movementType = ScrollRect.MovementType.Clamped;

			var viewport = Utilities.FindChildRecursive(
				popup_.gameObject, "Viewport");
			var rt = viewport.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			var sb = Utilities.FindChildRecursive(
				popup_.gameObject, "Scrollbar Vertical");
			rt = sb.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, 0);

			// scrollbar background color
			sb.GetComponent<Image>().color = Style.BackgroundColor;

			// scrollbar handle color
			var handle = Utilities.FindChildRecursive(sb, "Handle");
			handle.GetComponent<Image>().color = Style.ButtonBackgroundColor;

			ComboBox.SetColors(popup_.popup);

			popup_.popup.popupPanelHeight = 50;

			rt = popup_.popup.popupPanel;
			rt.offsetMin = new Vector2(ClientBounds.Left, ClientBounds.Top);
			rt.offsetMax = new Vector2(ClientBounds.Right, ClientBounds.Bottom);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(ClientBounds.Center.X, 0);

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
