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


	public abstract class BasicParameter<T> : IParameter
	{
		private string baseName_ = "";
		private string specificName_ = null;
		private string autoName_ = null;
		private string customName_ = null;

		public BasicParameter()
		{
		}

		public string BaseName
		{
			set { baseName_ = value; }
		}

		public string SpecificName
		{
			set { specificName_ = value; }
		}

		public string Name
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
		public abstract string TypeName { get; }
		public abstract bool Registered { get; }
		public abstract void Register();
		public abstract void Unregister();
	}


	public class BoolParameter : BasicParameter<bool>
	{
		private bool value_;
		private bool? override_;
		private JSONStorableBool storableBool_ = null;
		private JSONStorableFloat storableFloat_ = null;

		public BoolParameter(bool v)
		{
			value_ = v;
		}

		public override bool Value
		{
			get
			{
				if (override_.HasValue)
					return override_.Value;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public bool InternalValue
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override bool Registered
		{
			get { return (storableBool_ != null); }
		}

		public override string TypeName
		{
			get { return "Bool"; }
		}

		public JSONStorableBool StorableBool
		{
			get { return storableBool_; }
		}

		public JSONStorableFloat StorableFloat
		{
			get { return storableFloat_; }
		}

		public override void Register()
		{
			Unregister();

			storableBool_ = new JSONStorableBool(Name, value_, BoolChanged);
			storableBool_.storeType = JSONStorableParam.StoreType.Full;

			storableFloat_ = new JSONStorableFloat(
				Name + ".f", 0, FloatChanged, 0, 1);

			storableFloat_.storeType = JSONStorableParam.StoreType.Full;

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (storableBool_ != null)
			{
				Synergy.Instance.UnregisterParameter(this);
				storableBool_ = null;
				storableFloat_ = null;
			}

			override_ = null;
		}

		private void BoolChanged(bool b)
		{
			override_ = b;
		}

		private void FloatChanged(float f)
		{
			override_ = (f >= 0.5);
		}
	}


	public class FloatParameter : BasicParameter<float>
	{
		private float value_;
		private float? override_;
		private JSONStorableFloat storable_ = null;

		public FloatParameter(float v)
		{
			value_ = v;
		}

		public override float Value
		{
			get
			{
				if (override_.HasValue)
					return override_.Value;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public float InternalValue
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override bool Registered
		{
			get { return (storable_ != null); }
		}

		public override string TypeName
		{
			get { return "Float"; }
		}

		public JSONStorableFloat Storable
		{
			get { return storable_; }
		}

		public override void Register()
		{
			Unregister();

			storable_ = new JSONStorableFloat(Name + ".f", 0, Changed, 0, 1);
			storable_.storeType = JSONStorableParam.StoreType.Full;

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (storable_ != null)
			{
				Synergy.Instance.UnregisterParameter(this);
				storable_ = null;
			}

			override_ = null;
		}

		private void Changed(float f)
		{
			override_ = f;
		}
	}


	public class IntParameter : BasicParameter<int>
	{
		private int value_;
		private int? override_;
		private JSONStorableFloat storable_ = null;

		public IntParameter(int v)
		{
			value_ = v;
		}

		public override int Value
		{
			get
			{
				if (override_.HasValue)
					return override_.Value;
				else
					return value_;
			}

			set
			{
				value_ = value;
			}
		}

		public int InternalValue
		{
			get { return value_; }
			set { value_ = value; }
		}

		public override bool Registered
		{
			get { return (storable_ != null); }
		}

		public override string TypeName
		{
			get { return "Int"; }
		}

		public JSONStorableFloat Storable
		{
			get { return storable_; }
		}

		public override void Register()
		{
			Unregister();

			storable_ = new JSONStorableFloat(Name + ".f", 0, Changed, 0, 1);
			storable_.storeType = JSONStorableParam.StoreType.Full;

			Synergy.Instance.RegisterParameter(this);
		}

		public override void Unregister()
		{
			if (storable_ != null)
			{
				Synergy.Instance.UnregisterParameter(this);
				storable_ = null;
			}

			override_ = null;
		}

		private void Changed(float f)
		{
			override_ = (int)Math.Round(f);
		}
	}
}
