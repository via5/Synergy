using System;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Button : Widget
	{
		public override string TypeName { get { return "button"; } }

		public delegate void Callback();
		public event Callback Clicked;

		private string text_ = "";
		private UIDynamicButton button_ = null;
		private int align_ = Label.AlignCenter | Label.AlignVCenter;

		public Button(string t = "", Callback clicked = null)
		{
			text_ = t;
			MinimumSize = new Size(150, 40);

			if (clicked != null)
				Clicked += clicked;
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				align_ = value;
				NeedsLayout();
			}
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableButtonPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			button_ = WidgetObject.GetComponent<UIDynamicButton>();
			button_.button.onClick.AddListener(OnClicked);
			button_.buttonText.text = text_;

			Style.Polish(button_);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			button_.buttonText.alignment = Label.ToTextAnchor(align_);
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20, 40);
		}

		private void OnClicked()
		{
			Utilities.Handler(() =>
			{
				Root.SetFocus(this);
				Clicked?.Invoke();
			});
		}
	}
}
