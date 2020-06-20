using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;

namespace Synergy.UI
{
	class TypedListImpl<ItemType> : Widget
		where ItemType : class
	{
		protected class Item
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
				get
				{
					return object_?.ToString() ?? "";
				}
			}
		}

		public delegate void ItemCallback(ItemType item);
		public event ItemCallback SelectionChanged;

		private readonly List<Item> items_ = new List<Item>();
		private int selection_ = -1;

		private UIDynamicPopup popup_ = null;
		private JSONStorableStringChooser storable_ =
			new JSONStorableStringChooser("", new List<string>(), "", "");


		public TypedListImpl(List<ItemType> items, ItemCallback selectionChanged)
		{
			if (items != null)
				SetItems(items);

			if (selectionChanged != null)
				SelectionChanged += selectionChanged;
		}

		public void AddItem(ItemType i)
		{
			AddItemNoUpdate(new Item(i));
			UpdateChoices();
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
			else
				Select(selection_);
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

		public void Clear()
		{
			SetItems(new List<ItemType>());
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

		public void UpdateItemsText()
		{
			UpdateChoices();
			UpdateLabel();
		}

		public virtual int IndexOf(ItemType item)
		{
			for (int i = 0; i < items_.Count; ++i)
			{
				if (items_[i].Object == item)
					return i;
			}

			return -1;
		}

		public void Select(ItemType item)
		{
			Select(IndexOf(item));
		}

		public void Select(int i)
		{
			if (i < 0 || i >= items_.Count)
				i = -1;

			selection_ = i;
			UpdateLabel();
			SelectionChanged?.Invoke(Selected);
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

		protected override void DoCreate()
		{
			popup_ = WidgetObject.GetComponent<UIDynamicPopup>();
			popup_.popup.showSlider = false;

			storable_.popup = popup_.popup;
			storable_.setCallbackFunction = OnSelectionChanged;

			var text = popup_.popup.popupButtonPrefab.GetComponentInChildren<Text>();
			if (text != null)
			{
				text.alignment = TextAnchor.MiddleLeft;
				text.rectTransform.offsetMin = new Vector2(
					text.rectTransform.offsetMin.x + 10,
					text.rectTransform.offsetMin.y);
			}

			var rt = popup_.popup.popupButtonPrefab;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 3, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 15);

			UpdateChoices();
			UpdateLabel();

			Style.Polish(popup_);
		}

		protected List<Item> InternalItems
		{
			get { return items_; }
		}

		protected UIDynamicPopup Popup
		{
			get { return popup_; }
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

		protected void UpdateLabel()
		{
			storable_.valNoCallback = "";
			if (selection_ != -1)
				storable_.valNoCallback = items_[selection_].GetHashCode().ToString();
		}

		private void AddItemNoUpdate(Item i)
		{
			items_.Add(i);
		}

		private void OnSelectionChanged(string s)
		{
			Utilities.Handler(() =>
			{
				int sel = -1;

				for (int i = 0; i < items_.Count; ++i)
				{
					if (items_[i].GetHashCode().ToString() == s)
					{
						sel = i;
						break;
					}
				}

				if (sel == -1)
					Synergy.LogError("combobox: selected item '" + s + "' not found");

				Select(sel);
			});
		}
	}


	class TypedComboBox<ItemType> : TypedListImpl<ItemType>
		where ItemType : class
	{
		public override string TypeName { get { return "combobox"; } }

		public delegate void Callback();
		public event Callback Opened;

		private Text arrow_ = null;


		public TypedComboBox(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public TypedComboBox(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public TypedComboBox(List<ItemType> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			float widest = 0;

			foreach (var i in InternalItems)
				widest = Math.Max(widest, Root.TextLength(i.Text) + 50);

			return new Size(Math.Max(175, widest), 40);
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableScrollablePopupPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			Popup.popup.onOpenPopupHandlers += () =>
			{
				Utilities.Handler(() =>
				{
					OnOpen();
				});
			};

			var arrowObject = new GameObject();
			arrowObject.transform.SetParent(WidgetObject.transform, false);
			arrowObject.AddComponent<RectTransform>();
			arrowObject.AddComponent<LayoutElement>();

			arrow_ = arrowObject.AddComponent<Text>();
			arrow_.alignment = TextAnchor.MiddleRight;
			arrow_.color = Style.TextColor;
			arrow_.raycastTarget = false;
			arrow_.text = "\x25bc";
			arrow_.fontSize = Style.FontSize;
			arrow_.font = Style.Font;

			var rt = Popup.popup.labelText.transform.parent.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			rt = Popup.popup.topButton.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 9, rt.offsetMin.y - 5);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y + 5);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);


			rt = Popup.popup.popupPanel;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 5);

			var text = Popup.popup.topButton.GetComponentInChildren<Text>();
			if (text != null)
			{
				text.alignment = TextAnchor.MiddleLeft;
				text.rectTransform.offsetMin = new Vector2(
					text.rectTransform.offsetMin.x + 10,
					text.rectTransform.offsetMin.y);

				// avoid overlap with arrow
				text.rectTransform.offsetMax = new Vector2(
					text.rectTransform.offsetMax.x - 25,
					text.rectTransform.offsetMax.y);
			}
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();

			var rect = arrow_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(0, 0);
			rect.offsetMax = new Vector2(Bounds.Width - 10, Bounds.Height);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Width / 2, Bounds.Height / 2);
		}

		protected virtual void OnOpen()
		{
			Root.SetFocus(this);
			Root.SetOpenedPopup(Popup.popup);
			Utilities.BringToTop(Popup.popup.popupPanel);

			Opened?.Invoke();
		}
	}


	class ComboBox : TypedComboBox<string>
	{
		public ComboBox(List<string> items = null)
			: base(items, null)
		{
		}

		public ComboBox(ItemCallback selectionChanged)
			: base(null, selectionChanged)
		{
		}

		public ComboBox(List<string> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
		}
	}
}
