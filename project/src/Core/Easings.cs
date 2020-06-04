using System.Collections.Generic;
using static System.Math;

// auto generated from Easings.tt

namespace Synergy
{
	class EasingFactory : BasicFactory<IEasing>
	{
		public override List<IEasing> GetAllObjects()
		{
			return new List<IEasing>()
			{
				new LinearEasing(),
				new SinusoidalEasing(),
				new QuadInEasing(),
				new QuadOutEasing(),
				new QuadInOutEasing(),
				new CubicInEasing(),
				new CubicOutEasing(),
				new CubicInOutEasing(),
				new QuartInEasing(),
				new QuartOutEasing(),
				new QuartInOutEasing(),
				new QuintInEasing(),
				new QuintOutEasing(),
				new QuintInOutEasing(),
				new SineInEasing(),
				new SineOutEasing(),
				new SineInOutEasing(),
				new ExpoInEasing(),
				new ExpoOutEasing(),
				new ExpoInOutEasing(),
				new CircInEasing(),
				new CircOutEasing(),
				new CircInOutEasing(),
				new BackInEasing(),
				new BackOutEasing(),
				new BackInOutEasing(),
				new ElasticInEasing(),
				new ElasticOutEasing(),
				new ElasticInOutEasing(),
				new BounceInEasing(),
				new BounceOutEasing(),
				new BounceInOutEasing(),
			};
		}
	}


	interface IEasing : IFactoryObject
	{
		IEasing Clone(int cloneFlags = 0);
		float Magnitude(float f);
	}


	abstract class BasicEasing : IEasing
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract float Magnitude(float f);

		public abstract IEasing Clone(int cloneFlags = 0);

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}

		protected float BounceOut(float x)
		{
			float n1 = 7.5625f;
			float d1 = 2.75f;

			if (x < 1 / d1) {
				return n1 * x * x;
			} else if (x < 2 / d1) {
				return n1 * (x -= 1.5f / d1) * x + 0.75f;
			} else if (x < 2.5 / d1) {
				return n1 * (x -= 2.25f / d1) * x + 0.9375f;
			} else {
				return n1 * (x -= 2.625f / d1) * x + 0.984375f;
			}
		}
	}


	class LinearEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "linear";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Linear";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new LinearEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x);
		}
	}

	class SinusoidalEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "sinusoidal";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Sinusoidal";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SinusoidalEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(-(Cos(PI * x) - 1) / 2);
		}
	}

	class QuadInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quadIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quad in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x);
		}
	}

	class QuadOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quadOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quad out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - (1 - x) * (1 - x));
		}
	}

	class QuadInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quadInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quad in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuadInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 2 * x * x : 1 - Pow(-2 * x + 2, 2) / 2);
		}
	}

	class CubicInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "cubicIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Cubic in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x);
		}
	}

	class CubicOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "cubicOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Cubic out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 3));
		}
	}

	class CubicInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "cubicInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Cubic in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CubicInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 4 * x * x * x : 1 - Pow(-2 * x + 2, 3) / 2);
		}
	}

	class QuartInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quartIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quart in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x * x);
		}
	}

	class QuartOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quartOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quart out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 4));
		}
	}

	class QuartInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quartInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quart in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuartInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 8 * x * x * x * x : 1 - Pow(-2 * x + 2, 4) / 2);
		}
	}

	class QuintInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quintIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quint in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x * x * x * x * x);
		}
	}

	class QuintOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quintOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quint out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Pow(1 - x, 5));
		}
	}

	class QuintInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "quintInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Quint in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new QuintInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? 16 * x * x * x * x * x : 1 - Pow(-2 * x + 2, 5) / 2);
		}
	}

	class SineInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "sineIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Sine in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Cos((x * PI) / 2));
		}
	}

	class SineOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "sineOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Sine out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(Sin((x * PI) / 2));
		}
	}

	class SineInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "sineInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Sine in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new SineInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(-(Cos(PI * x) - 1) / 2);
		}
	}

	class ExpoInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "expoIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Expo in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : Pow(2, 10 * x - 10));
		}
	}

	class ExpoOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "expoOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Expo out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 1 ? 1 : 1 - Pow(2, -10 * x));
		}
	}

	class ExpoInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "expoInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Expo in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ExpoInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? Pow(2, 20 * x - 10) / 2 : (2 - Pow(2, -20 * x + 10)) / 2);
		}
	}

	class CircInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "circIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Circ in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - Sqrt(1 - Pow(x, 2)));
		}
	}

	class CircOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "circOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Circ out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(Sqrt(1 - Pow(x - 1, 2)));
		}
	}

	class CircInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "circInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Circ in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new CircInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (1 - Sqrt(1 - Pow(2 * x, 2))) / 2 : (Sqrt(1 - Pow(-2 * x + 2, 2)) + 1) / 2);
		}
	}

	class BackInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "backIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Back in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(2.70158f * x * x * x - 1.70158f * x * x);
		}
	}

	class BackOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "backOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Back out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 + 2.70158f * Pow(x - 1, 3) + 1.70158f * Pow(x - 1, 2));
		}
	}

	class BackInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "backInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Back in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BackInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (Pow(2 * x, 2) * ((2.5949f + 1) * 2 * x - 2.5949f)) / 2 : (Pow(2 * x - 2, 2) * ((2.5949f + 1) * (x * 2 - 2) + 2.5949f) + 2) / 2);
		}
	}

	class ElasticInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "elasticIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Elastic in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : -Pow(2, 10 * x - 10) * Sin((x * 10 - 10.75) * 2.0944f));
		}
	}

	class ElasticOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "elasticOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Elastic out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : Pow(2, -10 * x) * Sin((x * 10 - 0.75) * 2.0944f) + 1);
		}
	}

	class ElasticInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "elasticInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Elastic in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new ElasticInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? -(Pow(2, 20 * x - 10) * Sin((20 * x - 11.125) * 1.3963f)) / 2 : (Pow(2, -20 * x + 10) * Sin((20 * x - 11.125) * 1.3963f)) / 2 + 1);
		}
	}

	class BounceInEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "bounceIn";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Bounce in";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceInEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(1 - BounceOut(1 - x));
		}
	}

	class BounceOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "bounceOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Bounce out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(BounceOut(x));
		}
	}

	class BounceInOutEasing : BasicEasing
	{
		public static string FactoryTypeName
		{
			get
			{
				return "bounceInOut";
			}
		}

		public override string GetFactoryTypeName()
		{
			return FactoryTypeName;
		}


		public static string DisplayName
		{
			get
			{
				return "Bounce in/out";
			}
		}

		public override string GetDisplayName()
		{
			return DisplayName;
		}


		public override IEasing Clone(int cloneFlags = 0)
		{
			return new BounceInOutEasing();
		}

		public override float Magnitude(float x)
		{
			return (float)(x < 0.5 ? (1 - BounceOut(1 - 2 * x)) / 2 : (1 + BounceOut(2 * x - 1)) / 2);
		}
	}
}

