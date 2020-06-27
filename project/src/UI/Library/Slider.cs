﻿using Leap.Unity;
using LeapInternal;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Synergy.UI
{
	class Slider : Widget
	{
		public delegate void ValueCallback(float f);
		public event ValueCallback ValueChanged;

		public override string TypeName { get { return "slider"; } }

		private UIDynamicSlider slider_ = null;

		// used before creation
		private float value_ = 0;
		private float min_ = 0;
		private float max_ = 0;

		public Slider()
		{
			Borders = new Insets(1);
		}

		public float Value
		{
			get
			{
				if (slider_ == null)
					return value_;
				else
					return slider_.slider.value;
			}

			set
			{
				if (slider_ == null)
				{
					value_ = value;
				}
				else
				{
					slider_.slider.value = value;
					ValueChanged?.Invoke(value);
				}
			}
		}

		public float Minimum
		{
			get
			{
				if (slider_ == null)
					return min_;
				else
					return slider_.slider.minValue;
			}

			set
			{
				if (slider_ == null)
					min_ = value;
				else
					slider_.slider.minValue = value;
			}
		}

		public float Maximum
		{
			get
			{
				if (slider_ == null)
					return max_;
				else
					return slider_.slider.maxValue;
			}

			set
			{
				if (slider_ == null)
					max_ = value;
				else
					slider_.slider.maxValue = value;
			}
		}

		public void Set(float value, float min, float max)
		{
			if (min > max)
			{
				var temp = min;
				min = max;
				max = temp;
			}

			value = global::Synergy.Utilities.Clamp(value, min, max);

			Minimum = min;
			Maximum = max;
			Value = value;
		}

		protected override GameObject CreateGameObject()
		{
			return UnityEngine.Object.Instantiate(
				Synergy.Instance.manager.configurableSliderPrefab).gameObject;
		}

		protected override void DoCreate()
		{
			slider_ = WidgetObject.GetComponent<UIDynamicSlider>();
			slider_.quickButtonsEnabled = false;
			slider_.defaultButtonEnabled = false;
			slider_.rangeAdjustEnabled = false;

			slider_.slider.minValue = min_;
			slider_.slider.maxValue = max_;
			slider_.slider.value = value_;

			slider_.slider.onValueChanged.AddListener(OnChanged);

			slider_.labelText.gameObject.SetActive(false);
			slider_.sliderValueTextFromFloat.gameObject.SetActive(false);
			UI.Utilities.FindChildRecursive(WidgetObject, "Panel").SetActive(false);

			var rt = slider_.slider.GetComponent<RectTransform>();
			rt.offsetMin = new Vector2(-1, -Bounds.Height + 1);
			rt.offsetMax = new Vector2(Bounds.Width, 0);
			rt.anchorMin = new Vector2(0, 1);
			rt.anchorMax = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(
				rt.offsetMin.x + (rt.offsetMax.x - rt.offsetMin.x) / 2,
				rt.offsetMin.y + (rt.offsetMax.y - rt.offsetMin.y) / 2);

			slider_.slider.GetComponent<Image>().color = new Color(0, 0, 0, 0);

			var fill = Utilities.FindChildRecursive(WidgetObject, "Fill");
			fill.GetComponent<Image>().color = new Color(0, 0, 0, 0);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			return new Size(100, 40);
		}

		private void OnChanged(float v)
		{
			ValueChanged?.Invoke(v);
		}
	}


	class TextSlider : UI.Panel
	{
		public delegate void ValueCallback(float f);
		public event ValueCallback ValueChanged;

		private readonly Slider slider_ = new Slider();
		private readonly TextBox text_ = new TextBox();

		private bool changingText_ = false;


		public TextSlider(ValueCallback valueChanged = null)
		{
			Layout = new BorderLayout(5);
			Add(slider_, BorderLayout.Center);
			Add(text_, BorderLayout.Right);

			text_.Text = "0";

			text_.Edited += OnTextChanged;
			slider_.ValueChanged += OnValueChanged;

			if (valueChanged != null)
				ValueChanged += valueChanged;
		}

		public float Value
		{
			get { return slider_.Value; }
			set { slider_.Value = value; }
		}

		public float Minimum
		{
			get { return slider_.Minimum; }
			set { slider_.Minimum = value; }
		}

		public float Maximum
		{
			get { return slider_.Maximum; }
			set { slider_.Maximum = value; }
		}

		public void Set(float value, float min, float max)
		{
			slider_.Set(value, min, max);
		}

		private void OnTextChanged(string s)
		{
			if (changingText_)
				return;

			float f;
			if (float.TryParse(s, out f))
			{
				f = global::Synergy.Utilities.Clamp(f, Minimum, Maximum);

				using (new ScopedFlag((b) => changingText_ = b))
				{
					slider_.Value = f;
				}
			}
		}

		private void OnValueChanged(float f)
		{
			text_.Text = f.ToString("0.00");
			ValueChanged?.Invoke(f);
		}
	}
}
