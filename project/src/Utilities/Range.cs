using System;

namespace Synergy
{
	interface IRange
	{
		bool IsEmpty();
	}


	abstract class Range<T> : IRange
	{
		public T Minimum, Maximum;

		public abstract bool IsEmpty();
	}


	class FloatRange : Range<float>
	{
		public FloatRange(float min, float max)
		{
			Minimum = min;
			Maximum = max;
		}

		public float Distance
		{
			get
			{
				return Math.Abs(Maximum - Minimum);
			}
		}

		public override bool IsEmpty()
		{
			return (Minimum == Maximum);
		}

		public override string ToString()
		{
			return Minimum.ToString() + "-" + Maximum.ToString();
		}
	}


	class IntRange : Range<int>
	{
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

		public override bool IsEmpty()
		{
			return (Minimum == Maximum);
		}

		public override string ToString()
		{
			return Minimum.ToString() + "-" + Maximum.ToString();
		}
	}
}
