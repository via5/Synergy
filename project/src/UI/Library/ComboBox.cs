using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class TypedComboBox<ItemType> : Widget
		where ItemType : class
	{
		public override string TypeName { get { return "combobox"; } }


		public class Item
		{
			private ItemType object_;

			public Item(ItemType o)
			{
				object_ = o;
			}

			public ItemType Object
			{
				get { return object_; }
			}

			public string Text
			{
				get { return object_.ToString(); }
			}
		}


		private readonly List<Item> items_ = new List<Item>();
		private int selection_ = -1;

		private UIDynamicPopup popup_ = null;
		private Text arrow_ = null;

		private JSONStorableStringChooser storable_ =
			new JSONStorableStringChooser("", new List<string>(), "", "");



		public TypedComboBox(List<ItemType> items = null)
		{
			if (items != null)
			{
				foreach (var i in items)
					AddItemNoUpdate(new Item(i));
			}
		}

		public void AddItem(ItemType i, bool select = false)
		{
			AddItemNoUpdate(new Item(i));
			UpdateChoices();

			if (select)
				Select(items_.Count - 1);
		}

		public void RemoveItem(ItemType item)
		{
			int itemIndex = -1;

			for (int i = 0; i < items_.Count; ++i)
			{
				if (items_[i].Object == item)
				{
					itemIndex = i;
					break;
				}
			}

			if (itemIndex == -1)
			{
				Synergy.LogError(
					"combobox: can't remove item '" + item.ToString() + "', " +
					"not found");

				return;
			}

			items_.RemoveAt(itemIndex);
			UpdateChoices();

			if (items_.Count == 0)
				Select(-1);
			else if (selection_ >= items_.Count)
				Select(items_.Count - 1);
			else if (selection_ > itemIndex)
				Select(selection_ - 1);
		}

		public List<ItemType> Items
		{
			get
			{
				var list = new List<ItemType>();

				foreach (var i in items_)
					list.Add(i.Object);

				return list;
			}

			set
			{
				SetItems(value, null);
			}
		}

		public void SetItems(List<ItemType> items, ItemType sel = null)
		{
			items_.Clear();

			int selIndex = -1;

			for (int i = 0; i < items.Count; ++i)
			{
				if (items[i] == sel)
					selIndex = i;

				AddItemNoUpdate(new Item(items[i]));
			}

			if (selIndex == -1 && items_.Count > 0)
				selIndex = 0;

			UpdateChoices();
			Select(selIndex);
		}

		public void Select(int i)
		{
			if (i < 0 || i >= items_.Count)
				i = -1;

			selection_ = i;
			UpdateLabel();
		}

		public ItemType Selected
		{
			get
			{
				if (selection_ < 0 || selection_ >= items_.Count)
					return null;
				else
					return items_[selection_].Object;
			}
		}

		protected override Size GetPreferredSize()
		{
			float widest = 0;

			foreach (var i in items_)
				widest = Math.Max(widest, Root.TextLength(i.Text));

			widest = Math.Max(widest, 60);

			return new Size(300, 40);
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurablePopupPrefab).gameObject;
		}

		public static void SetColors(UIPopup popup)
		{
			// popup selection background color
			popup.selectColor = Style.SelectionBackgroundColor;

			// popup button background color
			popup.normalColor = Style.ButtonBackgroundColor;

			// button text color
			var st = popup.topButton.GetComponentInChildren<UIStyleText>();
			if (st != null)
			{
				st.color = Style.TextColor;
				st.UpdateStyle();
			}

			// button background color
			var i = popup.topButton.GetComponent<Image>();
			i.color = Style.ButtonBackgroundColor;

			// popup background color for combobox
			i = popup.popupPanel.GetComponent<Image>();
			i.color = Style.ButtonBackgroundColor;

			// popup background color for lists
			Transform sv = null;
			foreach (Transform c in popup.popupPanel.transform)
			{
				if (c.name == "Scroll View")
				{
					sv = c;
					break;
				}
			}

			if (sv != null)
				sv.GetComponent<Image>().color = Style.BackgroundColor;


			// popup button text color
			var sst = popup.popupButtonPrefab.GetComponentsInChildren<UIStyleText>();
			foreach (var st2 in sst)
			{
				st2.color = Style.TextColor;
				st2.UpdateStyle();
			}

			// popup button highlight background color
			var sb = popup.popupButtonPrefab.GetComponent<UIStyleButton>();
			sb.highlightedColor = Style.HighlightBackgroundColor;
			sb.UpdateStyle();

			sb = popup.topButton.GetComponent<UIStyleButton>();
			sb.normalColor = Style.ButtonBackgroundColor;
			sb.highlightedColor = Style.HighlightBackgroundColor;
			sb.colorMultiplier = 3.89f;
			sb.UpdateStyle();
		}

		protected override void DoCreate()
		{
			popup_ = Object.GetComponent<UIDynamicPopup>();
			popup_.labelWidth = 0;
			popup_.labelSpacingRight = 0;
			popup_.popupPanelHeight = 1000;
			popup_.popup.showSlider = false;
			popup_.popup.onOpenPopupHandlers += OnOpen;

			SetColors(popup_.popup);


			var arrowObject = new GameObject();
			arrowObject.transform.SetParent(Object.transform, false);
			arrowObject.AddComponent<RectTransform>();
			arrowObject.AddComponent<LayoutElement>();

			arrow_ = arrowObject.AddComponent<Text>();
			arrow_.alignment = TextAnchor.MiddleRight;
			arrow_.color = Style.TextColor;
			arrow_.raycastTarget = false;
			arrow_.text = "\x25bc";
			arrow_.fontSize = Style.FontSize;
			arrow_.font = Style.Font;

			storable_.popup = popup_.popup;
			storable_.setCallbackFunction = OnSelectionChanged;

			UpdateChoices();
			UpdateLabel();

			var rt = popup_.popup.labelText.transform.parent.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + 40);
			rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + 44);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			var rect = arrow_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(0, 0);
			rect.offsetMax = new Vector2(Bounds.Width - 10, Bounds.Height);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Width / 2, Bounds.Height / 2);

			rt = popup_.popup.topButton.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 9, rt.offsetMin.y - 3);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 3, rt.offsetMax.y + 3);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);


			rt = popup_.popup.popupPanel;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 3, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 5);

			rt = popup_.popup.popupButtonPrefab;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 3, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 15);
		}

		private void AddItemNoUpdate(Item i)
		{
			items_.Add(i);
		}

		private void UpdateLabel()
		{
			if (selection_ == -1)
				storable_.valNoCallback = "";
			else
				storable_.valNoCallback = items_[selection_].GetHashCode().ToString();
		}

		private void UpdateChoices()
		{
			var display = new List<string>();
			var hashes = new List<string>();

			foreach (var i in items_)
			{
				display.Add(i.Text);
				hashes.Add(i.GetHashCode().ToString());
			}

			storable_.displayChoices = display;
			storable_.choices = hashes;
		}

		private void OnSelectionChanged(string s)
		{
			for (int i = 0; i < items_.Count; ++i)
			{
				if (items_[i].GetHashCode().ToString() == s)
				{
					Select(i);
					return;
				}
			}

			Synergy.LogError("combobox: selected item '" + s + "' not found");
			Select(-1);
		}

		private void OnOpen()
		{
			Root.SetFocus(this);
			Root.SetOpenedPopup(popup_.popup);
		}
	}


	class ComboBox : TypedComboBox<string>
	{
		public ComboBox(List<string> items = null)
			: base(items)
		{
		}
	}
}
