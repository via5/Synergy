﻿using System;
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
				set { object_ = value; }
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

		public void AddItem(ItemType i, bool select=false)
		{
			AddItemNoUpdate(new Item(i));
			UpdateChoices();

			if (select)
				Select(i);
		}

		private bool ItemsEqual(ItemType a, ItemType b)
		{
			return EqualityComparer<ItemType>.Default.Equals(a, b);
		}

		public void RemoveItem(ItemType item)
		{
			int itemIndex = -1;

			for (int i = 0; i < items_.Count; ++i)
			{
				if (ItemsEqual(items_[i].Object, item))
				{
					itemIndex = i;
					break;
				}
			}

			if (itemIndex == -1)
				return;

			RemoveItemAt(itemIndex);
		}

		public void RemoveItemAt(int index)
		{
			items_.RemoveAt(index);
			UpdateChoices();

			if (items_.Count == 0)
				Select(-1);
			else if (selection_ >= items_.Count)
				Select(items_.Count - 1);
			else if (selection_ > index)
				Select(selection_ - 1);
			else
				Select(selection_);
		}

		public void SetItemAt(int index, ItemType item)
		{
			if (index < 0 || index >= items_.Count)
				return;

			items_[index].Object = item;
			UpdateItemText(index);
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
				if (ItemsEqual(items[i], sel))
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
				if (ItemsEqual(items_[i].Object, item))
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
		}

		protected override void DoSetEnabled(bool b)
		{
			base.DoSetEnabled(b);
			popup_.popup.topButton.interactable = b;
		}

		protected List<Item> InternalItems
		{
			get { return items_; }
		}

		public UIDynamicPopup Popup
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

		private GameObject arrowObject_ = null;
		private BorderGraphics borders_ = null;
		private TextBox filter_ = null;
		private bool filterable_ = false;
		private bool accuratePreferredSize_ = true;
		private int popupHeight_ = -1;


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

		public GameObject Arrow
		{
			get { return arrowObject_; }
		}

		public bool Filterable
		{
			get { return filterable_; }
			set { filterable_ = value; }
		}

		public bool AccuratePreferredSize
		{
			get { return accuratePreferredSize_; }
			set { accuratePreferredSize_ = value; }
		}

		public int PopupHeight
		{
			get
			{
				return popupHeight_;
			}

			set
			{
				popupHeight_ = value;

				if (Popup != null)
					Popup.popupPanelHeight = value;
			}
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
			if (!accuratePreferredSize_)
				return new Size(maxWidth, maxHeight);

			float widest = 0;

			foreach (var i in InternalItems)
			{
				widest = Math.Max(
					widest,
					Root.TextLength(Font, FontSize, i.Text) + 50);
			}

			return new Size(Math.Max(175, widest), 40);
		}

		public override void Create()
		{
			base.Create();

			if (filterable_)
			{
				filter_.Create();
				filter_.MainObject.transform.SetParent(FilterParent(), false);
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableFilterablePopupPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			if (filterable_)
			{
				filter_ = new TextBox("", "Filter");
				filter_.FocusFlags = Root.FocusKeepPopup;
				filter_.Changed += OnFilterChanged;
			}
			else
			{
				Popup.popup.useFiltering = false;
			}

			if (popupHeight_ >= 0)
				Popup.popupPanelHeight = popupHeight_;

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
					// this handler is called before the popup, so visible is
					// false when it's about to open
					if (!Popup.popup.visible)
						OnOpen();
				});
			};

			arrowObject_ = new GameObject();
			arrowObject_.transform.SetParent(WidgetObject.transform, false);
			arrowObject_.AddComponent<RectTransform>();
			arrowObject_.AddComponent<LayoutElement>();

			var arrowText = arrowObject_.AddComponent<Text>();
			arrowText.alignment = TextAnchor.MiddleRight;
			arrowText.raycastTarget = false;
			arrowText.text = Utilities.DownArrow;

			var go = new GameObject();
			go.transform.SetParent(Popup.popup.popupPanel.transform, false);

			borders_ = go.AddComponent<BorderGraphics>();
			borders_.Borders = new Insets(2);
			borders_.Color = BorderColor;

			var text = Popup.popup.topButton?.GetComponentInChildren<Text>();
			if (text != null)
				text.alignment = TextAnchor.MiddleLeft;

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

			var rect = arrowObject_.GetComponent<RectTransform>();
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

			UpdateFilterBounds();

			if (filterable_)
			{
				// the popup hasn't processed the event yet and it will steal
				// focus when it does, so focus the filter after that
				Synergy.Instance.CreateTimer(Timer.Immediate, () =>
				{
					filter_.Focus();
				});
			}

			Opened?.Invoke();
		}

		Transform FilterParent()
		{
			return Utilities.FindChildRecursive(Popup.popup, "PopupPanel").transform;
		}

		private void UpdateFilterBounds()
		{
			if (!filterable_)
				return;

			var parent = FilterParent();
			var rt = parent.GetComponent<RectTransform>();
			var r = rt.rect;
			var h = filter_.GetRealPreferredSize(DontCare, DontCare).Height;

			filter_.Bounds = Rectangle.FromPoints(
				0, r.height, r.width, r.height + h);

			filter_.UpdateBounds();
		}

		private void OnFilterChanged(string s)
		{
			Popup.popup.filter = s;
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
			up_.FontSize = Style.Theme.ComboBoxNavTextSize;
			down_ = new CustomButton(Utilities.DownArrow, OnDown);
			down_.FontSize = Style.Theme.ComboBoxNavTextSize;
			list_ = new ComboBoxList<ItemType>(items);

			up_.MinimumSize = new Size(20, 20);
			down_.MinimumSize = new Size(20, 20);

			buttons_.Visible = false;
			buttons_.Add(up_);
			buttons_.Add(down_);

			Layout = new BorderLayout(3);
			Add(buttons_, BorderLayout.Left);
			Add(list_, BorderLayout.Center);

			list_.Opened += () => OnOpened();
			list_.SelectionChanged += (item) => SelectionChanged?.Invoke(item);
			list_.SelectionIndexChanged += (index) => SelectionIndexChanged?.Invoke(index);

			if (selectionChanged != null)
				SelectionChanged += selectionChanged;
		}

		public ComboBox(List<ItemType> items, ItemType sel, ItemCallback selectionChanged)
			: this(items, selectionChanged)
		{
			Select(sel);
		}

		private void OnOpened()
		{
			Opened?.Invoke();
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

		public bool Filterable
		{
			get { return list_.Filterable; }
			set { list_.Filterable = value; }
		}

		public bool AccuratePreferredSize
		{
			get { return list_.AccuratePreferredSize; }
			set { list_.AccuratePreferredSize = value; }
		}

		public int PopupHeight
		{
			get { return list_.PopupHeight; }
			set { list_.PopupHeight = value; }
		}

		public void AddItem(ItemType i, bool select=false)
		{
			list_.AddItem(i, select);
		}

		public void RemoveItem(ItemType item)
		{
			list_.RemoveItem(item);
		}

		public void RemoveItemAt(int index)
		{
			list_.RemoveItemAt(index);
		}

		public void SetItemAt(int index, ItemType item)
		{
			list_.SetItemAt(index, item);
		}

		public List<ItemType> Items
		{
			get { return list_.Items; }
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
			if (SelectedIndex > 0)
				Select(SelectedIndex - 1);
		}

		private void OnDown()
		{
			if (SelectedIndex < (Count - 1))
				Select(SelectedIndex + 1);
		}
	}
}
