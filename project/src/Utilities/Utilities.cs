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


	public interface IRandomProvider
	{
		// returns a float in [min, max]
		//
		float RandomFloat(float min, float max);

		// returns an int in [min, max[
		//
		int RandomInt(int min, int max);
	}


	public class UnityRandomProvider : IRandomProvider
	{
		public float RandomFloat(float min, float max)
		{
			return UnityEngine.Random.Range(min, max);
		}

		public int RandomInt(int min, int max)
		{
			return UnityEngine.Random.Range(min, max);
		}
	}



	public class Utilities
	{
		private static IRandomProvider rng_ = null;

		public const int CloneZero = 1;
		public const string PresetAtomPlaceholder = "$ATOM";

		public const int FullPreset = 0x01;
		public const int StepPreset = 0x02;
		public const int ModifierPreset = 0x04;
		public const int PresetReplace = 0x08;
		public const int PresetAppend = 0x10;
		public const int PresetMerge = 0x20;
		public const int PresetUsePlaceholder = 0x40;

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

		public static IRandomProvider RandomProvider
		{
			set { rng_ = value; }
		}

		// returns a float in [min, max]
		//
		public static float RandomFloat(float min, float max)
		{
			return rng_.RandomFloat(min, max);
		}

		// returns an int in [min, max[
		//
		public static int RandomInt(int min, int max)
		{
			return rng_.RandomInt(min, max);
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

		private static Rigidbody centerEye_ = null;
		private static bool centerEyeChecked_ = false;

		public static Vector3 CenterEyePosition()
		{
			if (centerEye_ == null && !centerEyeChecked_)
			{
				centerEyeChecked_ = true;

				var rig = SuperController.singleton.GetAtomByUid("[CameraRig]");
				if (rig == null)
				{
					Synergy.LogError("[CameraRig] not found");
				}
				else
				{
					centerEye_ = FindRigidbody(rig, "CenterEye");
					if (centerEye_ == null)
						Synergy.LogError("CenterEye not found in [CameraRig]");
				}
			}

			return centerEye_?.transform?.position ?? new Vector3();
		}

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
			if (atom == null || string.IsNullOrEmpty(name))
				return null;

			foreach (var fr in atom.forceReceivers)
			{
				if (fr.name == name)
					return fr.GetComponent<Rigidbody>();
			}

			return null;
		}

		public static string FullName(Rigidbody rb)
		{
			return rb.transform.parent.name + "." + rb.name;
		}

		public static Rigidbody FindRigidbody(Atom atom, string name)
		{
			if (atom == null || string.IsNullOrEmpty(name))
				return null;

			foreach (var fr in atom.rigidbodies)
			{
				if (fr.name == name)
					return fr.GetComponent<Rigidbody>();
			}

			return null;
		}

		public static FreeControllerV3 FindFreeController(Atom atom, string name)
		{
			if (atom == null || string.IsNullOrEmpty(name))
				return null;

			foreach (var fc in atom.freeControllers)
			{
				if (fc.name == name)
					return fc;
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
				if (newMorph == null)
					newMorph = mui.GetMorphByDisplayName(oldMorph.displayName);

				if (newMorph != null)
					list.Add(newMorph);
			}

			return list;
		}

		public static JSONStorable FindStorableInNewAtom(
			Atom newAtom, string oldId)
		{
			if (newAtom == null || oldId == "")
				return null;

			var st = newAtom.GetStorableByID(oldId);
			if (st != null)
				return st;

			var re = new Regex("plugin#(\\d+)_(.+)");
			var m = re.Match(oldId);

			if (m == null)
				return null;

			var pluginName = m.Groups[2].Value;

			for (int i = 0; i < 20; ++i)
			{
				var stName = "plugin#" + i.ToString() + "_" + pluginName;

				st = newAtom.GetStorableByID(stName);
				if (st != null)
					return st;
			}


			return null;
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

		private static NamedAudioClip LoadClip(string path)
		{
			var sc = SuperController.singleton;
			var cm = URLAudioClipManager.singleton;

			var loadPath = sc.NormalizeLoadPath(path);

			if (cm.GetClip(loadPath) != null)
				return null;

			cm.QueueFilePath(path);
			var clip = cm.GetClip(path);

			if (clip == null)
			{
				Synergy.LogError("error while loading " + loadPath);
				return null;
			}

			return clip;
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

		public static void AddAudioClipDirectory(Action<List<NamedAudioClip>> f)
		{
			var exts = new List<string>() { ".mp3", ".wav", ".ogg" };

			try
			{
				var sc = SuperController.singleton;
				var cm = URLAudioClipManager.singleton;

				sc.GetDirectoryPathDialog((string path) =>
				{
					if (string.IsNullOrEmpty(path))
						return;

					var files = sc.GetFilesAtPath(path)?.ToList();
					if (files == null)
						return;

					var list = new List<NamedAudioClip>();

					foreach (var file in files)
					{
						bool skip = true;

						foreach (var ext in exts)
						{
							if (file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
							{
								skip = false;
								break;
							}
						}

						if (skip)
						{
							Synergy.LogVerbose("skipping " + file);
							continue;
						}

						var clip = LoadClip(file);
						if (clip != null)
							list.Add(clip);
					}

					f(list);

				}, sc.currentLoadDir);
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

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list)
		{
			list.Sort(new GenericNaturalStringComparer<T>());
		}

		public static bool StorableIsPlugin(string storableId)
		{
			return storableId.StartsWith("plugin#");
		}

		public static bool StorableIsPlugin(JSONStorable s)
		{
			return StorableIsPlugin(s.storeId);
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


	public class Integration
	{
		public class Gaze
		{
			private Atom atom_ = null;
			private JSONStorableBool toggle_ = null;

			public Gaze Clone(int cloneFlags = 0)
			{
				var g = new Gaze();
				CopyTo(g, cloneFlags);
				return g;
			}

			private void CopyTo(Gaze g, int cloneFlags)
			{
				g.atom_ = atom_;
				g.toggle_ = toggle_;
			}

			public Atom Atom
			{
				get
				{
					return atom_;
				}

				set
				{
					if (atom_ != value)
					{
						atom_ = value;
						toggle_ = null;
					}
				}
			}

			public bool Available()
			{
				if (atom_ == null)
					return false;

				foreach (var id in atom_.GetStorableIDs())
				{
					if (id.Contains("MacGruber.Gaze"))
						return true;
				}

				return false;
			}

			public bool SetEnabled(Atom atom, bool b)
			{
				if (toggle_ == null)
				{
					if (atom_ == null)
						return true;

					toggle_ = GetToggle();
				}

				if (toggle_ == null)
					return false;

				try
				{
					toggle_.val = b;
					return true;
				}
				catch (Exception)
				{
					Synergy.LogError(
						"gaze: failed to change value, " +
						"assuming script is gone");

					toggle_ = null;
				}

				return false;
			}

			private JSONStorableBool GetToggle()
			{
				Synergy.LogVerbose("gaze: searching for enabled parameter");

				foreach (var id in atom_.GetStorableIDs())
				{
					if (id.Contains("MacGruber.Gaze"))
					{
						var st = atom_.GetStorableByID(id);
						if (st == null)
						{
							Synergy.LogError("gaze: can't find storable " + id);
							continue;
						}

						var en = st.GetBoolJSONParam("enabled");
						if (en == null)
						{
							Synergy.LogError("gaze: no enabled param");
							continue;
						}

						return en;
					}
				}

				return null;
			}
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
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0) return r;
				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
		}
	}


	// from https://stackoverflow.com/questions/248603
	public class GenericNaturalStringComparer<T> : IComparer<T>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(T xt, T yt)
		{
			string x = xt.ToString();
			string y = yt.ToString();

			x = x.ToLower();
			y = y.ToLower();
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0) return r;
				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
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


	public static class HashHelper
	{
		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return 31 * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				return 31 * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3,
			T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				return 31 * hash + arg4.GetHashCode();
			}
		}
	}


	public class IgnoreFlag
	{
		private bool ignore_ = false;

		public static implicit operator bool(IgnoreFlag f)
		{
			return f.ignore_;
		}

		public void Do(Action a)
		{
			try
			{
				ignore_ = true;
				a();
			}
			finally
			{
				ignore_ = false;
			}
		}
	}


	public class ScopedFlag : IDisposable
	{
		private readonly Action<bool> a_;
		private readonly bool start_;

		public ScopedFlag(Action<bool> a, bool start = true)
		{
			a_ = a;
			start_ = start;

			a_(start_);
		}

		public void Dispose()
		{
			a_(!start_);
		}
	}


	public class Strings
	{
		public static string Get(string s, params object[] ps)
		{
			if (ps.Length > 0)
				return string.Format(s, ps);
			else
				return s;
		}
	}
}
