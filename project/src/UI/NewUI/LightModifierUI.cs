﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy.NewUI
{
	class LightModifierPanel : BasicModifierPanel
	{
		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasComponent<Light>);

		private readonly FactoryComboBox<
			LightPropertyFactory, ILightProperty> property_;

		private readonly FactoryComboBox<EasingFactory, IEasing> easing_ =
			new FactoryComboBox<EasingFactory, IEasing>();

		private FactoryObjectWidget<
			LightPropertyFactory,
			ILightProperty,
			LightPropertyUIFactory> ui_;

		private readonly MovementPanel min_ = new MovementPanel(
			S("Minimum"), MovementWidgets.SmallMovement);

		private readonly MovementPanel max_ = new MovementPanel(
			S("Maximum"), MovementWidgets.SmallMovement);

		private LightModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();


		public LightModifierPanel()
		{
			property_ = new FactoryComboBox<
				LightPropertyFactory, ILightProperty>(OnPropertyChanged);

			ui_ = new FactoryObjectWidget<
				LightPropertyFactory, ILightProperty, LightPropertyUIFactory>();

			var w = new UI.Panel(new UI.VerticalFlow(30));

			var gl = new UI.GridLayout(4);
			gl.Spacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true, false, true };

			var p = new UI.Panel(gl);
			p.Add(new UI.Label(S("Atom")));
			p.Add(atom_);
			p.Add(new UI.Label(S("Property")));
			p.Add(property_);
			p.Add(new UI.Label(S("Easing")));
			p.Add(easing_);
			w.Add(p);

			w.Add(min_);
			w.Add(max_);

			Layout = new UI.BorderLayout(20);
			Add(w, UI.BorderLayout.Top);
			Add(ui_, UI.BorderLayout.Center);

			atom_.AtomSelectionChanged += OnAtomChanged;
			easing_.FactoryTypeChanged += OnEasingChanged;
		}

		public override string Title
		{
			get { return S("Light"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is LightModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as LightModifier;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				property_.Select(modifier_.Property);
				easing_.Select(modifier_.Movement.Easing);
				min_.Set(modifier_.Movement.Minimum);
				max_.Set(modifier_.Movement.Maximum);
				ui_.Set(modifier_.Property);
			});
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;
		}

		private void OnPropertyChanged(ILightProperty p)
		{
			if (ignore_)
				return;

			modifier_.Property = p;
			ui_.Set(p);
		}

		private void OnEasingChanged(IEasing easing)
		{
			if (ignore_)
				return;

			modifier_.Movement.Easing = easing;
		}
	}


	class LightPropertyUIFactory : IUIFactory<ILightProperty>
	{
		public Dictionary<string, Func<IUIFactoryWidget<ILightProperty>>> GetCreators()
		{
			return new Dictionary<string, Func<IUIFactoryWidget<ILightProperty>>>()
			{
				{
					IntensityLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					RangeLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					SpotAngleLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					EnabledLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					ShadowStrengthLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					CastShadowsLightProperty.FactoryTypeName,
					() => { return new EmptyLightPropertyUI(); }
				},

				{
					ColorLightProperty.FactoryTypeName,
					() => { return new ColorLightPropertyUI(); }
				},
			};
		}
	}


	class EmptyLightPropertyUI : UI.Panel, IUIFactoryWidget<ILightProperty>
	{
		public void Set(ILightProperty o)
		{
			// no-op
		}
	}


	class ColorLightPropertyUI : UI.Panel, IUIFactoryWidget<ILightProperty>
	{
		private ColorLightProperty property_ = null;
		private readonly UI.ColorPicker color1_, color2_;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();

		public ColorLightPropertyUI()
		{
			color1_ = new UI.ColorPicker(S("Color 1"), OnColor1Changed);
			color2_ = new UI.ColorPicker(S("Color 2"), OnColor2Changed);

			Layout = new UI.HorizontalFlow(20);
			Add(color1_);
			Add(color2_);
		}

		public void Set(ILightProperty o)
		{
			property_ = o as ColorLightProperty;

			ignore_.Do(() =>
			{
				color1_.Color = property_.Color1;
				color2_.Color = property_.Color2;
			});
		}

		private void OnColor1Changed(Color c)
		{
			if (ignore_)
				return;

			property_.Color1 = c;
		}

		private void OnColor2Changed(Color c)
		{
			if (ignore_)
				return;

			property_.Color2 = c;
		}
	}
}
