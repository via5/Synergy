using System;

namespace Synergy
{
	public interface IParameter
	{
		string Name { get; set; }
		string TypeName { get; }
		bool Registered { get; }
		void Register();
		void Unregister();
	}


	public abstract class Parameter : IParameter
	{
		public const int NoFlags = 0x00;
		public const int AllowNegative = 0x01;
		public const int Constrained = 0x02;

		public abstract string Name { get; set; }
		public abstract string TypeName { get; }
		public abstract bool Registered { get; }
		public abstract void Register();
		public abstract void Unregister();
	}


	public abstract class BasicParameter<T> : Parameter
	{
		private string baseName_ = "";
		private string specificName_ = null;
		private string autoName_ = null;
		private string customName_ = null;

		protected T value_;
		protected T override_;
		protected bool hasOverride_ = false;
		protected readonly T rangeIncrement_;
		protected readonly int flags_;

		public BasicParameter(
			string specificName, T value,
			T rangeIncrement, int flags)
		{
			specificName_ = specificName;
			value_ = value;
			rangeIncrement_ = rangeIncrement;
			flags_ = flags;
		}

		public string BaseName
		{
			get { return baseName_; }
			set { baseName_ = value; }
		}

		public string SpecificName
		{
			set { specificName_ = value; }
		}

		public override string Name
		{
			get
			{
				if (customName_ != null)
					return customName_;

				if (autoName_ != null)
					return autoName_;

				string s = baseName_;

				if (specificName_ != null)
				{
					if (s != "")
						s += " ";

					s += specificName_;
				}

				autoName_ = Synergy.Instance.MakeParameterName(s);

				return autoName_;
			}

			set
			{
				customName_ = value;

				if (Registered)
					Register();
			}
		}

		public abstract T Value { get; set; }

		public T InternalValue
		{
			get { return value_; }
		}

		protected abstract void AdjustStorable();

		protected void SetOverride(T v)
		{
			override_ = v;
			hasOverride_ = true;
		}

		protected T RangeIncrement
		{
			get { return rangeIncrement_; }
		}

		protected int Flags
		{
			get { return flags_; }
		}
	}


	public abstract class BasicParameter2<T, StorableType> : BasicParameter<T>
		where StorableType : JSONStorableParam
	{
		private StorableType storable_ = null;

		protected BasicParameter2(
			string specificName, T value,
			T rangeIncrement, int flags)
				: base(specificName, value, rangeIncrement, flags)
		{
		}

		public override T Value
		{
			get
			{
				if (hasOverride_)
					return override_;
				else
					return value_;
			}

			set
			{
				value_ = value;
				AdjustStorable();
			}
		}

		public override bool Registered
		{
			get { return (storable_ != null); }
		}

		public StorableType Storable
		{
			get { return storable_; }
		}

		protected void SetStorable(StorableType s)
		{
			storable_ = s;

			if (storable_ != null)
			{
				storable_.storeType = JSONStorableParam.StoreType.Full;
				AdjustStorable();
			}
		}
	}


	public class BoolParameter : BasicParameter2<bool, JSONStorableBool>
	{
		private JSONStorableFloat storableFloat_ = null;

		public BoolParameter(string specificName, bool v)
			: base(specificName, v, false, Parameter.Constrained)
		{
		}

		public override string TypeName
		{
			get { return "Bool"; }
		}

		public JSONStorableFloat StorableFloat
		{
			get { return storableFloat_; }
		}

		public override void Register()
		{
			Unregister();

			SetStorable(new JSONStorableBool(Name, Value, BoolChanged));

			storableFloat_ = new JSONStorableFloat(
				Name + ".f", 0, FloatChanged, 0, 1, true);

			storableFloat_.storeType = JSONStorableParam.StoreType.Full;

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (Storable == null)
				return;

			Synergy.Instance.UnregisterParameter(this);
			SetStorable(null);
			storableFloat_ = null;
		}

		private void BoolChanged(bool b)
		{
			SetOverride(b);
		}

		private void FloatChanged(float f)
		{
			SetOverride(f >= 0.5);
		}

		protected override void AdjustStorable()
		{
			// no-op
		}
	}


	public class FloatParameter : BasicParameter2<float, JSONStorableFloat>
	{
		public FloatParameter(
			string specificName, float value,
			float rangeIncrement, int flags = NoFlags)
				: base(specificName, value, rangeIncrement, flags)
		{
		}

		public override string TypeName
		{
			get { return "Float"; }
		}

		public override void Register()
		{
			Unregister();

			SetStorable(new JSONStorableFloat(
				Name, 0, Changed, 0, 0, Bits.IsSet(Flags, Constrained)));

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (Storable != null)
			{
				Synergy.Instance.UnregisterParameter(this);
				SetStorable(null);
			}
		}

		private void Changed(float f)
		{
			SetOverride(f);
		}

		protected override void AdjustStorable()
		{
			if (Storable == null)
				return;

			var r = Utilities.MakeFloatRange(
				Value, Storable.min, Storable.max,
				RangeIncrement, Bits.IsSet(Flags, AllowNegative));

			Storable.min = r.Minimum;
			Storable.max = r.Maximum;
		}
	}


	public class IntParameter : BasicParameter2<int, JSONStorableFloat>
	{
		public IntParameter(
			string specificName, int value,
			int rangeIncrement, int flags = NoFlags)
				: base(specificName, value, rangeIncrement, flags)
		{
		}

		public override string TypeName
		{
			get { return "Int"; }
		}

		public override void Register()
		{
			Unregister();

			SetStorable(new JSONStorableFloat(
				Name, 0, Changed, 0, 1, Bits.IsSet(Flags, Constrained)));

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (Storable != null)
			{
				Synergy.Instance.UnregisterParameter(this);
				SetStorable(null);
			}
		}

		private void Changed(float f)
		{
			SetOverride((int)Math.Round(f));
		}

		protected override void AdjustStorable()
		{
			if (Storable == null)
				return;

			var r = Utilities.MakeFloatRange(
				Value, Storable.min, Storable.max,
				RangeIncrement, Bits.IsSet(Flags, AllowNegative));

			Storable.min = r.Minimum;
			Storable.max = r.Maximum;
		}
	}
}
