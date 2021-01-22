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

		public delegate void IndexCallback(int index);
		public event IndexCallback SelectionIndexChanged;

		private readonly List<Item> items_ = new List<Item>();
		private int selection_ = -1;
		private bool updatingChoices_ = false;

		private UIDynamicPopup popup_ = null;


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

		public ItemType At(int index)
		{
			if (index < 0 || index >= items_.Count)
				return null;
			else
				return items_[index].Object;
		}

		public int Count
		{
			get { return items_.Count; }
		}

		public void Clear()
		{
			SetItems(new List<ItemType>());
		}

		public virtual void SetItems(List<ItemType> items, ItemType sel = null)
		{
			items_.Clear();

			int selIndex = -1;

			for (int i = 0; i < items.Count; ++i)
			{
				if (EqualityComparer<ItemType>.Default.Equals(items[i], sel))
					selIndex = i;

				AddItemNoUpdate(new Item(items[i]));
			}

			UpdateChoices();
			Select(selIndex);
		}

		public void UpdateItemsText()
		{
			UpdateChoices();
			UpdateLabel();
		}

		public void UpdateItemText(int index)
		{
			if (index < 0 || index >= items_.Count)
				return;

			popup_.popup.setDisplayPopupValue(index, items_[index].Text);

			if (index == selection_)
				UpdateLabel();
		}

		public void UpdateItemText(ItemType item)
		{
			int i = IndexOf(item);
			if (i != -1)
				UpdateItemText(i);
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
			SelectionIndexChanged?.Invoke(selection_);
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

		public int SelectedIndex
		{
			get { return selection_; }
		}

		protected override void DoCreate()
		{
			popup_ = WidgetObject.GetComponent<UIDynamicPopup>();
			popup_.popup.showSlider = false;
			popup_.popup.useDifferentDisplayValues = true;
			popup_.popup.labelText.gameObject.SetActive(false);
			popup_.popup.onValueChangeHandlers += OnSelectionChanged;

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

			Style.Setup(this);
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);

			popup_.popup.topButton.interactable = b;
			Style.Polish(this);
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
			if (popup_ == null)
				return;

			using (new ScopedFlag((b) => updatingChoices_ = b))
			{
				var display = new List<string>();
				var hashes = new List<string>();

				foreach (var i in items_)
				{
					display.Add(i.Text);
					hashes.Add(i.GetHashCode().ToString());
				}

				popup_.popup.numPopupValues = display.Count;
				for (int i = 0; i < display.Count; ++i)
				{
					popup_.popup.setDisplayPopupValue(i, display[i]);
					popup_.popup.setPopupValue(i, hashes[i]);
				}
			}
		}

		protected void UpdateLabel()
		{
			if (popup_ == null)
				return;

			var visible = popup_.popup.visible;

			popup_.popup.currentValueNoCallback = "";
			if (selection_ != -1)
			{
				popup_.popup.currentValueNoCallback =
					items_[selection_].GetHashCode().ToString();
			}

			popup_.popup.visible = visible;
		}

		private void AddItemNoUpdate(Item i)
		{
			items_.Add(i);
		}

		private void OnSelectionChanged(string s)
		{
			if (updatingChoices_)
				return;

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


	class ComboBoxList<ItemType> : TypedListImpl<ItemType>
		where ItemType : class
	{
		public override string TypeName { get { return "comboboxlist"; } }

		public event Callback Opened;

		private Text arrow_ = null;
		private BorderGraphics borders_ = null;


		public ComboBoxList(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ComboBoxList(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ComboBoxList(List<ItemType> items, ItemCallback selectionChanged)
			: base(items, selectionChanged)
		{
		}

		public override void SetItems(List<ItemType> items, ItemType sel = null)
		{
			base.SetItems(items, sel);

			if (SelectedIndex == -1 && Count > 0)
				Select(0);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			float widest = 0;

			foreach (var i in InternalItems)
			{
				widest = Math.Max(
					widest,
					Root.TextLength(Font, FontSize, i.Text) + 50);
			}

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
				var rt2 = borders_.gameObject.GetComponent<RectTransform>();
				Utilities.SetRectTransform(rt2, new Rectangle(
					0, 0, new Size(
					Popup.popup.popupPanel.rect.width,
					Popup.popup.popupPanel.rect.height)));
			};

			var h = Popup.popup.topButton.gameObject.AddComponent<MouseHandler>();
			h.Up += (data) =>
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
			arrow_.text = Utilities.DownArrow;
			arrow_.fontSize = Style.DefaultFontSize;
			arrow_.font = Style.DefaultFont;

			var rt = Popup.popup.labelText.transform.parent.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			rt = Popup.popup.topButton.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x - 12, rt.offsetMin.y - 6);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 8, rt.offsetMax.y + 6);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);


			rt = Popup.popup.popupPanel;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 10, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x + 5, rt.offsetMax.y - 5);

			var go = new GameObject();
			go.transform.SetParent(Popup.popup.popupPanel.transform, false);
			borders_ = go.AddComponent<BorderGraphics>();
			borders_.Borders = new Insets(2);
			borders_.Color = BorderColor;


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


	class ComboBox<ItemType> : Widget
		where ItemType : class
	{
		public override string TypeName { get { return "combobox"; } }

		public event Callback Opened;

		public delegate void ItemCallback(ItemType item);
		public event ItemCallback SelectionChanged;

		public delegate void IndexCallback(int index);
		public event IndexCallback SelectionIndexChanged;

		private readonly Panel buttons_ = null;
		private readonly CustomButton up_ = null;
		private readonly CustomButton down_ = null;
		private ComboBoxList<ItemType> list_;
		private bool nav_ = false;


		public ComboBox(List<ItemType> items = null)
			: this(items, null)
		{
		}

		public ComboBox(IndexCallback selectionChanged)
			: this(null, null)
		{
			if (selectionChanged != null)
				SelectionIndexChanged += selectionChanged;
		}

		public ComboBox(ItemCallback selectionChanged)
			: this(null, selectionChanged)
		{
		}

		public ComboBox(List<ItemType> items, ItemCallback selectionChanged)
		{
			buttons_ = new Panel(new VerticalFlow());
			up_ = new CustomButton(Utilities.UpArrow, OnUp);
			up_.FontSize = Style.ComboBoxNavTextSize;
			down_ = new CustomButton(Utilities.DownArrow, OnDown);
			down_.FontSize = Style.ComboBoxNavTextSize;
			list_ = new ComboBoxList<ItemType>(items);

			up_.MinimumSize = new Size(20, 20);
			down_.MinimumSize = new Size(20, 20);

			buttons_.Visible = false;
			buttons_.Add(up_);
			buttons_.Add(down_);

			Layout = new BorderLayout(3);
			Add(buttons_, BorderLayout.Left);
			Add(list_, BorderLayout.Center);

			list_.Opened += () => Opened?.Invoke();
			list_.SelectionChanged += (item) => SelectionChanged?.Invoke(item);
			list_.SelectionIndexChanged += (index) => SelectionIndexChanged?.Invoke(index);

			if (selectionChanged != null)
				SelectionChanged += selectionChanged;
		}

		public bool NavButtons
		{
			get
			{
				return nav_;
			}

			set
			{
				nav_ = value;
				buttons_.Visible = value;
			}
		}

		public void AddItem(ItemType i)
		{
			list_.AddItem(i);
		}

		public void RemoveItem(ItemType item)
		{
			list_.RemoveItem(item);
		}

		public List<ItemType> Items
		{
			get { return list_.Items; }
			set { list_.Items = value; }
		}

		public ItemType At(int index)
		{
			return list_.At(index);
		}

		public int Count
		{
			get { return list_.Count; }
		}

		public void Clear()
		{
			list_.Clear();
		}

		public void SetItems(List<ItemType> items, ItemType sel = null)
		{
			list_.SetItems(items, sel);

			if (SelectedIndex == -1 && Count > 0)
				Select(0);
		}

		public void UpdateItemsText()
		{
			list_.UpdateItemsText();
		}

		public void UpdateItemText(int index)
		{
			list_.UpdateItemText(index);
		}

		public void UpdateItemText(ItemType item)
		{
			list_.UpdateItemText(item);
		}

		public int IndexOf(ItemType item)
		{
			return list_.IndexOf(item);
		}

		public void Select(ItemType item)
		{
			list_.Select(item);
		}

		public void Select(int i)
		{
			list_.Select(i);
		}

		public ItemType Selected
		{
			get { return list_.Selected; }
		}

		public int SelectedIndex
		{
			get { return list_.SelectedIndex; }
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			list_.Enabled = b;
		}

		private void OnUp()
		{
			if (SelectedIndex < (Count - 1))
				Select(SelectedIndex + 1);
		}

		private void OnDown()
		{
			if (SelectedIndex > 0)
				Select(SelectedIndex - 1);
		}
	}
}
