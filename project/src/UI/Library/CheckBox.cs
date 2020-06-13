using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class CheckBox : Widget
	{
		public override string TypeName { get { return "checkbox"; } }

		public delegate void ClickHandler();

		private string text_ = "";
		private UIDynamicToggle toggle_ = null;
		private event ClickHandler clicked_;

		public CheckBox(string t = "")
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
				Synergy.Instance.manager.configurableTogglePrefab).gameObject;
		}

		protected override void DoCreate()
		{
			toggle_ = Object.GetComponent<UIDynamicToggle>();
			toggle_.toggle.onValueChanged.AddListener(OnClicked);

			toggle_.backgroundImage.color = new Color(0, 0, 0, 0);
			toggle_.textColor = Root.DefaultTextColor;
			toggle_.labelText.text = text_;

			toggle_.toggle.graphic.rectTransform.localScale = new Vector3(
				0.75f, 0.75f, 0.75f);


			var rt = toggle_.toggle.image.rectTransform;// GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - 10);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 20, rt.offsetMax.y - 30);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			rt = toggle_.labelText.rectTransform;
			rt.offsetMin = new Vector2(rt.offsetMin.x - 15, rt.offsetMin.y);
			rt.offsetMax = new Vector2(rt.offsetMax.x - 15, rt.offsetMax.y);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength(text_) + 20 + 40, 40);
		}

		private void OnClicked(bool b)
		{
			Root.SetFocus(this);
			clicked_();
		}
	}
}
