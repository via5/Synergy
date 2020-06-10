using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class ComboBox : Widget
	{
		public class Item
		{
			private string text_;

			public Item(string text)
			{
				text_ = text;
			}

			public string Text
			{
				get { return text_; }
				set { text_ = value; }
			}
		}


		//private readonly Label label_ = new Label();
		//private readonly Button button_ = new Button("\x25bc");
		private readonly List<Item> items_ = new List<Item>();
		private int selection_ = -1;

		private UIDynamicPopup popup_ = null;
		private Text arrow_ = null;

		private JSONStorableStringChooser storable_ =
			new JSONStorableStringChooser("", new List<string>(), "", "");

		public ComboBox(List<string> strings = null)
		{
			if (strings != null)
			{
				foreach (var s in strings)
					AddItem(s);
			}

			//label_.MinimumSize = new Size(200, 0);
			//
			//Layout = new HorizontalFlow();
			//Add(label_);
			//Add(button_);
		}

		public void AddItem(Item i)
		{
			items_.Add(i);

			if (selection_ == -1)
				Select(0);
		}

		public Item AddItem(string s)
		{
			var i = new Item(s);
			AddItem(i);
			return i;
		}

		public void Select(int i)
		{
			if (i < 0 || i >= items_.Count)
				i = -1;

			selection_ = i;
			UpdateLabel();
		}

		protected override Size GetPreferredSize()
		{
			float widest = 0;

			foreach (var i in items_)
				widest = Math.Max(widest, Root.TextLength(i.Text));

			widest = Math.Max(widest, 60);

			return new Size(300, 40);
		}

		public override string TypeName
		{
			get
			{
				return "combobox";
			}
		}

		protected override GameObject CreateGameObject()
		{
			var t = UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurablePopupPrefab);

			popup_ = t.GetComponent<UIDynamicPopup>();
			popup_.labelWidth = 0;
			popup_.labelSpacingRight = 0;
			popup_.popupPanelHeight = 1000;
			popup_.popup.showSlider = false;

			popup_.popup.selectColor = new Color(0.55f, 0.55f, 0.55f);


			var arrowObject = new GameObject();
			arrowObject.transform.SetParent(t, false);
			arrowObject.AddComponent<RectTransform>();
			arrowObject.AddComponent<LayoutElement>();

			arrow_ = arrowObject.AddComponent<Text>();
			arrow_.alignment = TextAnchor.MiddleRight;
			arrow_.color = new Color(0.15f, 0.15f, 0.15f);
			arrow_.raycastTarget = false;
			arrow_.text = "\x25bc";
			arrow_.fontSize = Root.DefaultFontSize;
			arrow_.font = Root.DefaultFont;

			return popup_.gameObject;
		}

		protected override void DoCreate()
		{
			var strings = new List<string>();

			foreach (var i in items_)
				strings.Add(i.Text);

			storable_.choices = strings;
			storable_.popup = popup_.popup;

			UpdateLabel();

			var rt = popup_.popup.labelText.transform.parent.gameObject.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + 40);
			rt.offsetMax = new Vector2(rt.offsetMax.x , rt.offsetMax.y + 44);
			rt.anchorMin = new Vector2(0, 0);
			rt.anchorMax = new Vector2(0, 0);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			var rect = arrow_.GetComponent<RectTransform>();
			rect.offsetMin = new Vector2(0, 0);
			rect.offsetMax = new Vector2(Bounds.Width - 10, Bounds.Height);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.anchoredPosition = new Vector2(Bounds.Width/2, Bounds.Height/2);

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

		private void UpdateLabel()
		{
			//if (popup_ != null)
			{
				if (selection_ == -1)
					storable_.valNoCallback = "";
				else
					storable_.valNoCallback = items_[selection_].Text;
			}
		}
	}
}
