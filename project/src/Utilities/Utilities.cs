using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using System.Text.RegularExpressions;

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
		public const int PresetMerge     = 0x20;

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

		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;

		public static void Handler(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				SuperController.LogError(e.ToString());

				var now = Time.realtimeSinceStartup;

				if (now - lastErrorTime_ < 1)
				{
					++errorCount_;
					if (errorCount_ > 5)
					{
						SuperController.LogError(
							"more than 5 errors in the last second, " +
							"disabling plugin");

						Synergy.Instance.enabledJSON.val = false;
					}
				}
				else
				{
					errorCount_ = 0;
				}

				lastErrorTime_ = now;
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

		public static Rigidbody FindForceReceiver(Atom atom, string name)
		{
			if (atom == null || name == null)
				return null;

			foreach (var fr in atom.forceReceivers)
			{
				if (fr.name == name)
					return fr.GetComponent<Rigidbody>();
			}

			return null;
		}

		public static Rigidbody FindRigidbody(Atom atom, string name)
		{
			if (atom == null || name == null)
				return null;

			foreach (var fr in atom.rigidbodies)
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

		public static DAZMorph FindMorphInNewAtom(
			Atom newAtom, DAZMorph oldMorph)
		{
			var mui = GetMUI(newAtom);
			if (mui == null)
				return null;

			return mui.GetMorphByUid(oldMorph.uid);
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

		public static bool AtomHasForceReceivers(Atom a)
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

		public static bool AtomHasEyes(Atom a)
		{
			if (a == null)
				return false;

			if (FindRigidbody(a, "headControl") == null)
				return false;

			if (FindRigidbody(a, "eyeTargetControl") == null)
				return false;

			return true;
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

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}
	}


	// from https://stackoverflow.com/questions/248603
	public class NaturalStringComparer : IComparer<string>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(string x, string y)
		{
			x = x.ToLower();
			y = y.ToLower();
			if(string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if(x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while(true)
			{
				int r = PartCompare(a[i], b[i]);
				if(r != 0) return r;
				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if(int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
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

		public static List<int> Shuffle(List<int> old, int count)
		{
			if (count == 0)
				return new List<int>();

			var last = -1;
			if (old.Count > 0)
				last = old[old.Count - 1];

			var newList = new List<int>();

			for (int i = 0; i < count; ++i)
				newList.Add(i);

			newList.Shuffle();

			if (newList[0] == last)
			{
				var mid = newList.Count / 2;
				newList[0] = newList[mid];
				newList[mid] = last;
			}

			return newList;
		}

		public void Shuffle(int count)
		{
			order_ = Shuffle(order_, count);
		}
	}
}
