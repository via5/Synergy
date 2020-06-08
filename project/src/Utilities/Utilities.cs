using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace Synergy
{
	class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}
	}

	sealed class ExplicitHolder<T>
		where T : class
	{
		private T value_ = null;

		public T HeldValue
		{
			get { return value_; }
		}

		public void Set(T t)
		{
			value_ = t;
		}
	}

	class Utilities
	{
		public const int CloneZero = 1;
		public const string PresetAtomPlaceholder = "$ATOM";

		public const int FullPreset      = 0x01;
		public const int StepPreset      = 0x02;
		public const int ModifierPreset  = 0x04;
		public const int PresetReplace   = 0x08;
		public const int PresetAppend    = 0x10;

		public static string PresetSavePath
		{
			get { return "Saves\\Synergy"; }
		}

		public static string CompletePresetExtension
		{
			get { return "syn"; }
		}

		public static string ModifierPresetExtension
		{
			get { return "synmodifier"; }
		}

		public static string StepPresetExtension
		{
			get { return "synstep"; }
		}


		public static Color DefaultButtonColor
		{
			get
			{
				return new Color(0.84f, 0.84f, 0.84f);
			}
		}

		public static FloatRange MakeFloatRange(
			float value, float min, float max,
			float rangeIncrement, bool allowNegative)
		{
			if (allowNegative)
			{
				if (value < min)
					min = -(((int)Math.Abs(value / rangeIncrement)) + 1) * rangeIncrement;

				min = Math.Min(min, -rangeIncrement);
			}
			else
			{
				min = 0;
			}


			if (value > max)
				max = (((int)Math.Abs(value / rangeIncrement)) + 1) * rangeIncrement;

			max = Math.Max(max, rangeIncrement);

			return new FloatRange(min, max);
		}


		public static void Handler(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Synergy.LogError(e.ToString());
			}
		}

		public static T Clamp<T>(T val, T min, T max)
			where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		public static string DirectionString(Vector3 v)
		{
			if (v == new Vector3(1, 0, 0))
				return "X";
			else if (v == new Vector3(0, 1, 0))
				return "Y";
			else if (v == new Vector3(0, 0, 1))
				return "Z";
			else
				return "";
		}

		public static string SecondsToString(float seconds)
		{
			return seconds.ToString("0.0") + "s";
		}

		public static Rigidbody FindRigidbody(Atom atom, string name)
		{
			if (atom == null)
				return null;

			foreach (var fr in atom.forceReceivers)
			{
				if (fr.name == name)
					return fr.GetComponent<Rigidbody>();
			}

			return null;
		}

		public static List<DAZMorph> GetAtomMorphs(Atom atom)
		{
			var mui = GetMUI(atom);

			if (mui == null)
				return new List<DAZMorph>();

			 return mui.GetMorphs();
		}

		public static DAZMorph GetAtomMorph(Atom atom, string morphUID)
		{
			var mui = GetMUI(atom);
			if (mui == null)
				return null;

			var m = mui.GetMorphByUid(morphUID);
			if (m == null)
				Synergy.LogWarning(morphUID + " not found");

			return m;
		}

		public static bool AtomHasMorphs(Atom atom)
		{
			var mui = GetMUI(atom);

			if (mui == null)
				return false;

			return (mui.GetMorphs().Count > 0);
		}

		public static List<DAZMorph> FindMorphsInNewAtom(
			Atom newAtom, List<DAZMorph> oldMorphs)
		{
			var list = new List<DAZMorph>();

			var mui = GetMUI(newAtom);
			if (mui == null)
				return list;

			foreach (var oldMorph in oldMorphs)
			{
				var newMorph = mui.GetMorphByUid(oldMorph.uid);
				if (newMorph != null)
					list.Add(newMorph);
			}

			return list;
		}

		public static bool AtomHasRigidbodies(Atom a)
		{
			foreach (var fr in a.forceReceivers)
			{
				var rb = fr.GetComponent<Rigidbody>();
				if (rb != null && rb.name != "object")
					return true;
			}

			return false;
		}

		public static bool AtomHasComponent<T>(Atom a)
		{
			return (a.GetComponentInChildren<T>() != null);
		}

		public static bool AtomCanPlayAudio(Atom a)
		{
			return (AtomAudioSource(a) != null);
		}

		public static void AddAudioClip(Action<NamedAudioClip> f)
		{
			try
			{
				var sc = SuperController.singleton;
				var cm = URLAudioClipManager.singleton;

				sc.GetMediaPathDialog((string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					var loadPath = sc.NormalizeLoadPath(path);

					if (cm.GetClip(loadPath) != null)
					{
						Synergy.LogError("already exists");
						return;
					}

					cm.QueueFilePath(path);
					var clip = cm.GetClip(path);

					if (clip == null)
					{
						Synergy.LogError("error while loading");
						return;
					}

					f(clip);
				});
			}
			catch (Exception e)
			{
				Synergy.LogError(e.Message);
			}
		}

		public static AudioSourceControl AtomAudioSource(Atom a)
		{
			if (a == null)
				return null;

			var headAudio = a.GetStorableByID("HeadAudioSource")
				as AudioSourceControl;

			if (headAudio != null)
				return headAudio;

			var child = a.GetComponentInChildren<AudioSourceControl>();
			if (child != null)
				return child;

			return null;
		}

		public static NamedAudioClip GetAudioClip(string uid)
		{
			var cm = URLAudioClipManager.singleton;
			if (cm == null)
				return null;

			return cm.GetClip(uid);
		}

		private static GenerateDAZMorphsControlUI GetMUI(Atom atom)
		{
			if (atom == null)
				return null;

			var cs = atom.GetStorableByID("geometry") as DAZCharacterSelector;
			if (cs == null)
				return null;

			return cs.morphsControlUI;
		}
	}


	class ShuffledOrder
	{
		private List<int> order_ = new List<int>();

		public ShuffledOrder Clone()
		{
			var o = new ShuffledOrder();
			o.order_ = new List<int>(order_);
			return o;
		}

		public int Count
		{
			get { return order_.Count; }
		}

		public int Get(int i)
		{
			return order_[i];
		}

		public void Add(int i)
		{
			order_.Add(i);
		}

		public void Remove(int i)
		{
			order_.Remove(i);

			for (int o = 0; o < order_.Count; ++o)
			{
				if (order_[o] > i)
					--order_[o];
			}
		}

		public void Shuffle(int count)
		{
			if (count == 0)
			{
				order_.Clear();
				return;
			}

			var last = -1;
			if (order_.Count > 0)
				last = order_[order_.Count - 1];

			order_.Clear();

			for (int i = 0; i < count; ++i)
				order_.Add(i);

			order_.Shuffle();

			if (order_[0] == last)
			{
				var mid = order_.Count / 2;
				order_[0] = order_[mid];
				order_[mid] = last;
			}
		}
	}


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
			string baseName, T value,
			T rangeIncrement, int flags)
		{
			baseName_ = baseName;
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
			string baseName, T value,
			T rangeIncrement, int flags)
				: base(baseName, value, rangeIncrement, flags)
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

		public BoolParameter(string baseName, bool v)
			: base(baseName, v, false, Parameter.Constrained)
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
			string baseName, float value,
			float rangeIncrement, int flags = NoFlags)
				: base(baseName, value, rangeIncrement, flags)
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
			string baseName, int value,
			int rangeIncrement, int flags = NoFlags)
				: base(baseName, value, rangeIncrement, flags)
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
