﻿using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		public override string TypeName { get { return "button"; } }

		public delegate void ClickHandler();

		private string text_ = "";
		private UIDynamicButton button_ = null;
		private event ClickHandler clicked_;

		public Button(string t = "")
		{
			text_ = t;
		}

		public ClickHandler Clicked
		{
			get { return clicked_; }
			set { clicked_ = value; }
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = Object.GetComponent<UIDynamicButton>();
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.text = text_;

			Style.Polish(button_);
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnClicked()
		{
			Root.SetFocus(this);
			clicked_?.Invoke();
		}
	}
}
