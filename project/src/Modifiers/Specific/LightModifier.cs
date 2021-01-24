using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	interface ILightProperty : IFactoryObject
	{
		ILightProperty Clone(int cloneFlags = 0);
		FloatRange PreferredRange { get; }
		void Set(Light light, float magnitude);
		void Reset(Light light);
	}

	sealed class LightPropertyFactory : BasicFactory<ILightProperty>
	{
		public override List<ILightProperty> GetAllObjects()
		{
			return new List<ILightProperty>()
			{
				new IntensityLightProperty(),
				new RangeLightProperty(),
				new SpotAngleLightProperty(),
				new EnabledLightProperty(),
				new ShadowStrengthLightProperty(),
				new CastShadowsLightProperty(),
				new ColorLightProperty()
			};
		}
	}


	abstract class BasicLightProperty : ILightProperty
	{
		public virtual FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 1);
			}
		}

		public abstract ILightProperty Clone(int cloneFlags = 0);

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}

		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract void Set(Light light, float magnitude);
		public abstract void Reset(Light light);
	}

	sealed class IntensityLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "intensity";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Intensity";
		public override string GetDisplayName() { return DisplayName; }

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 8);
			}
		}

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new IntensityLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			light.intensity = magnitude;
		}

		public override void Reset(Light light)
		{
			light.intensity = 1.0f;
		}
	}

	sealed class RangeLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "range";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Range";
		public override string GetDisplayName() { return DisplayName; }

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 25);
			}
		}

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new RangeLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			light.range = magnitude;
		}

		public override void Reset(Light light)
		{
			light.intensity = 5.0f;
		}
	}

	sealed class SpotAngleLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "spotAngle";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Spot angle";
		public override string GetDisplayName() { return DisplayName; }

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 180);
			}
		}

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new SpotAngleLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			light.spotAngle = magnitude;
		}

		public override void Reset(Light light)
		{
			light.intensity = 80.0f;
		}
	}

	sealed class ColorLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "color";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Color";
		public override string GetDisplayName() { return DisplayName; }

		private Color color1_ = Color.blue;
		private Color color2_ = Color.yellow;

		public Color Color1
		{
			get { return color1_; }
			set { color1_ = value; }
		}

		public Color Color2
		{
			get { return color2_; }
			set { color2_ = value; }
		}

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new ColorLightProperty
			{
				Color1 = Color1,
				Color2 = Color2
			};
		}

		public override void Set(Light light, float magnitude)
		{
			var c = new Color(
				Interpolate(Color1.r, Color2.r, magnitude),
				Interpolate(Color1.g, Color2.g, magnitude),
				Interpolate(Color1.b, Color2.b, magnitude));

			light.color = c;
		}

		private float Interpolate(float a, float b, float magnitude)
		{
			return a + (b - a) * magnitude;
		}

		public override void Reset(Light light)
		{
			light.color = new Color(1.0f, 228.0f / 255.0f, 199.0f / 255.0f);
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("color1", color1_);
			o.Add("color2", color2_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ColorLightProperty");
			if (o == null)
				return false;

			o.Opt("color1", ref color1_);
			o.Opt("color2", ref color2_);

			return true;
		}
	}

	sealed class ShadowStrengthLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "shadowStrength";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Shadow strength";
		public override string GetDisplayName() { return DisplayName; }

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new ShadowStrengthLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			light.shadowStrength = magnitude;
		}

		public override void Reset(Light light)
		{
			light.shadowStrength = 0.2f;
		}
	}

	sealed class EnabledLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "onoff";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "On/off";
		public override string GetDisplayName() { return DisplayName; }

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new EnabledLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			light.enabled = (magnitude >= 0.5f);
		}

		public override void Reset(Light light)
		{
			light.enabled = true;
		}
	}

	sealed class CastShadowsLightProperty : BasicLightProperty
	{
		public static string FactoryTypeName { get; } = "castShadows";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Cast shadows";
		public override string GetDisplayName() { return DisplayName; }

		public override ILightProperty Clone(int cloneFlags = 0)
		{
			return new CastShadowsLightProperty();
		}

		public override void Set(Light light, float magnitude)
		{
			if (magnitude >= 0.5f)
				light.shadows = LightShadows.Hard;
			else
				light.shadows = LightShadows.None;
		}

		public override void Reset(Light light)
		{
			light.shadows = LightShadows.None;
		}
	}


	sealed class LightModifier : AtomWithMovementModifier
	{
		public static string FactoryTypeName { get; } = "light";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Light";
		public override string GetDisplayName() { return DisplayName; }


		private ILightProperty property_ = null;


		public LightModifier(ILightProperty property = null)
		{
			if (!Utilities.AtomHasComponent<Light>(Atom))
				Atom = null;

			Property = property;
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new LightModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(LightModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.property_ = property_?.Clone(cloneFlags);
		}

		public override void Removed()
		{
			base.Removed();
			ResetProperty();
		}

		public ILightProperty Property
		{
			get
			{
				return property_;
			}

			set
			{
				ResetProperty();
				property_ = value;
				FirePreferredRangeChanged();
			}
		}

		private void ResetProperty()
		{
			if (Atom != null)
			{
				Light light = Atom.GetComponentInChildren<Light>();
				if (light != null)
					property_?.Reset(light);
			}
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (property_ == null)
					return new FloatRange(0, 1);
				else
					return property_.PreferredRange;
			}
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (paused)
				return;

			if (property_ != null && Atom != null)
			{
				Light light = Atom.GetComponentInChildren<Light>();
				if (light == null)
					return;

				property_.Set(light, Movement.Magnitude);
			}
		}

		protected override string MakeName()
		{
			string s = "LT ";

			if (Atom == null && property_ == null)
			{
				s += "none";
			}
			else
			{
				if (Atom == null)
					s += "none";
				else
					s += Atom.name;

				s += " ";

				if (property_ == null)
					s += "none";
				else
					s += property_.GetDisplayName();
			}

			return s;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("property", property_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("LightModifier");
			if (o == null)
				return false;

			o.Opt<LightPropertyFactory, ILightProperty>(
				"property", ref property_);

			return true;
		}
	}
}
