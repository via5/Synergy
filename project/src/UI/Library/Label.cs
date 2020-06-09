using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label2 : Widget
	{
		private GameObject object_ = null;

		protected override void DoCreate()
		{
			var m = Synergy.Instance.manager;

			var t = UnityEngine.Object.Instantiate(m.configurableButtonPrefab);
			object_ = t.gameObject;
			object_.transform.SetParent(Root.PluginParent, false);

			var rect = object_.GetComponent<RectTransform>();

			rect.offsetMin = new Vector2(Bounds.left, -Bounds.bottom);
			rect.offsetMax = new Vector2(Bounds.right, -Bounds.top);
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);

			var db = object_.GetComponent<UIDynamicButton>();
			var text = db.buttonText;

			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.black;
			text.raycastTarget = false;
			text.text = "Test";

			var layoutElement = object_.GetComponent<LayoutElement>();
			layoutElement.minWidth = Bounds.width;
			layoutElement.preferredWidth = Bounds.width;
			layoutElement.flexibleWidth = Bounds.width;
			layoutElement.ignoreLayout = true;
		}

		protected override Size GetPreferredSize()
		{
			return new Size(Root.TextLength("Test"), 40);
		}

		public override string TypeName
		{
			get
			{
				return "button";
			}
		}
	}
}
