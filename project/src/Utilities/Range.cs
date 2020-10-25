using System;

namespace Synergy
{
	public struct FloatRange
	{
		public float Minimum, Maximum;

		public FloatRange(float min, float max)
		{
			Minimum = min;
			Maximum = max;
		}

		public FloatRange(FloatRange r)
		{
			Minimum = r.Minimum;
			Maximum = r.Maximum;
		}

		public float Distance
		{
			get
			{
				return Math.Abs(Maximum - Minimum);
			}
		}

		public bool IsEmpty()
		{
			return (Minimum == Maximum);
		}

		public override string ToString()
		{
			return Minimum.ToString() + "-" + Maximum.ToString();
		}



		public override int GetHashCode()
		{
			return HashHelper.GetHashCode(Minimum, Maximum);
		}

		public override bool Equals(Object o)
		{
			if (!(o is FloatRange))
				return false;

			return Equals((FloatRange)o);
		}

		public bool Equals(FloatRange r)
		{
			return (Minimum == r.Minimum) && (Maximum == r.Maximum);
		}
	}


	class IntRange
	{
		public int Minimum, Maximum;

		public IntRange(int min, int max)
		{
			Minimum = min;
			Maximum = max;
		}

		public int Distance
		{
			get
			{
				return Math.Abs(Maximum - Minimum);
			}
		}

		public bool IsEmpty()
		{
			return (Minimum == Maximum);
		}

		public override string ToString()
		{
			return Minimum.ToString() + "-" + Maximum.ToString();
		}
	}
}
