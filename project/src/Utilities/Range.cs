using System;

namespace Synergy
{
	public interface IRange
	{
		bool IsEmpty();
	}


	public abstract class Range<T> : IRange
	{
		public T Minimum, Maximum;

		public abstract bool IsEmpty();
	}


	public class FloatRange : Range<float>
	{
		public FloatRange()
			: this(0, 0)
		{
		}

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

		public override bool IsEmpty()
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
			var r = o as FloatRange;
			if (r == null)
				return false;

			return Equals(r);
		}

		public bool Equals(FloatRange r)
		{
			if (r == null)
				return false;

			if (ReferenceEquals(this, r))
				return true;

			return (Minimum == r.Minimum) && (Maximum == r.Maximum);
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
