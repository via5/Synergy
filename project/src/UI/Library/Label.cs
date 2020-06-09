using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Label2 : Widget
	{
		private GameObject object_ = null;

		protected override void DoCreate()
		{
			//   0,0
			//
			//           500,-500

			var m = Synergy.Instance.manager;

			var t = UnityEngine.Object.Instantiate(m.configurableButtonPrefab);
			object_ = t.gameObject;
			object_.transform.SetParent(Root.PluginParent, false);

			var rect = object_.GetComponent<RectTransform>();
			//rect.offsetMin = new Vector2(
			//	Root.Pixels.xMin + (Bounds.xMin * Root.Pixels.width),
			//	-Root.Pixels.yMin - (Bounds.yMax * Root.Pixels.height));
			//
			//rect.offsetMax = new Vector2(
			//	Root.Pixels.xMin + (Bounds.xMax * Root.Pixels.width),
			//	-Root.Pixels.yMin - (Bounds.yMin * Root.Pixels.height));

			rect.offsetMin = new Vector2(Bounds.xMin, -Bounds.yMax);
			rect.offsetMax = new Vector2(Bounds.xMax, -Bounds.yMin);

			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0f, 0);

			Synergy.LogError(rect.offsetMin.ToString() + " " + rect.offsetMax.ToString());

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
