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

		public const int FullPreset = 0x01;
		public const int StepPreset = 0x02;
		public const int ModifierPreset = 0x04;
		public const int PresetReplace = 0x08;
		public const int PresetAppend = 0x10;
		public const int PresetMerge = 0x20;

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

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
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


	class Overlapper
	{
		public delegate bool RealIndexCallback(int i);
		public delegate bool TickCallback(
			int realIndex, float deltaTime, bool stepForwards, bool paused);
		public delegate float TimeRemainingCallback(int realIndex);
		public delegate List<int> Generator(List<int> old);
		public delegate int CountCallback();
		public delegate float TimeCallback();

		public event RealIndexCallback CanRun;
		public event RealIndexCallback Resume;
		public event RealIndexCallback CanRunBackwards;
		public event RealIndexCallback Reset;
		public event TickCallback Ticker;
		public event TimeRemainingCallback TimeRemaining;
		public event Generator Regenerate;
		public event CountCallback ItemCount;
		public event TimeCallback GetOverlapTime;

		public class TickInfo
		{
			public int orderIndex;
			public bool forwards;
			public bool order1;
			public bool mustWait;

			public TickInfo()
			{
				orderIndex = -1;
				forwards = false;
				order1 = true;
				mustWait = false;
			}

			public TickInfo(TickInfo o)
			{
				orderIndex = o.orderIndex;
				forwards = o.forwards;
				order1 = o.order1;
				mustWait = o.mustWait;
			}

			public override string ToString()
			{
				return
					"i=" + orderIndex.ToString() + " " +
					(forwards ? "fw" : "bw") + " " +
					(order1 ? "o1" : "o2") + " " +
					(mustWait ? "wait" : "");
			}
		}


		private readonly string name_;
		private List<int> order1_ = new List<int>();
		private List<int> order2_ = new List<int>();
		private TickInfo active_ = new TickInfo();
		private TickInfo overlap_ = new TickInfo();


		public Overlapper(string name)
		{
			name_ = name;
		}

		public int CurrentIndex
		{
			get
			{
				return GetRealIndex(active_.orderIndex, true);
			}
		}

		public float TimeRemainingForCurrent
		{
			get
			{
				var ri = CurrentIndex;
				if (ri == -1)
					return -1;
				else
					return TimeRemaining(ri);
			}
		}

		public TickInfo ActiveTick
		{
			get { return new TickInfo(active_); }
		}

		public TickInfo OverlapTick
		{
			get { return new TickInfo(overlap_); }
		}

		public float OverlapTime
		{
			get { return GetOverlapTime(); }
		}

		public int GetRealIndex(int orderIndex, bool order1)
		{
			if (orderIndex == -1)
				return -1;

			var list = (order1 ? order1_ : order2_);

			if (orderIndex < 0 || orderIndex >= list.Count)
			{
				LogErrorST(
					$"GetRealIndex: orderIndex {orderIndex} out of range " +
					 "in " + (order1 ? "order1" : "order2") + ", " +
					$"count={list.Count}");

				return -1;
			}

			var realIndex = list[orderIndex];

			if (realIndex < 0 || realIndex >= ItemCount())
			{
				LogErrorST(
					$"GetRealIndex: realIndex {realIndex} out of range, " +
					$"count={ItemCount()} in " + (order1 ? "order1" : "order2"));

				return -1;
			}

			return realIndex;
		}

		public void ForceRun(int realIndex)
		{
			order1_ = Regenerate(null);
			order2_ = new List<int>();

			active_ = new TickInfo();
			overlap_ = new TickInfo();

			for (int i = 0; i < order1_.Count; ++i)
			{
				var ri = GetRealIndex(i, true);
				Reset(ri);

				if (ri == realIndex)
					active_.orderIndex = i;
			}

			if (active_.orderIndex == -1)
			{
				LogError($"item {realIndex} not found");
				return;
			}

			Resume(realIndex);
		}

		public void Tick(float deltaTime)
		{
			if (order1_.Count == 0)
			{
				Log("Tick: generating order1");
				order1_ = Regenerate(order1_);
			}

			if (order1_.Count == 0)
			{
				Log("Tick: no steps");
				return;
			}

			if (order2_.Count == 0)
			{
				Log("Tick: generating order2");
				order2_ = Regenerate(order1_);
			}

			if (active_.orderIndex == -1)
			{
				Log(
					"active is -1, starting from 0, " +
					$"order count={order1_.Count}");

				active_.forwards = true;
				active_.order1 = true;
				NextActive();
			}


			for (int orderIndex = 0; orderIndex < order1_.Count; ++orderIndex)
			{
				var realIndex = GetRealIndex(orderIndex, true);

				if (realIndex == -1)
				{
					Log(
						$"Tick: in loop, GetRealIndex failed, " +
						$"i={orderIndex} count={order1_.Count}");

					continue;
				}

				if (orderIndex == active_.orderIndex)
				{
					if (CanRun(realIndex) && Ticker(realIndex, deltaTime, active_.forwards, false))
					{
						// still going

						if (overlap_.orderIndex == -1 &&
							GetOverlapTime() > 0 &&
							TimeRemaining(realIndex) < GetOverlapTime())
						{
							Log(
								$"Tick: remaining {TimeRemaining(realIndex)}, " +
								"starting overlap");

							// step ending, start overlap
							NextOverlap();
						}
					}
					else
					{
						Log("step tick finished, next");

						// next
						NextActive();
					}
				}
				else
				{
					if (overlap_.order1 && orderIndex != overlap_.orderIndex)
					{
						// this step is not active nor the overlap
						Ticker(realIndex, deltaTime, false, true);
					}
				}
			}

			if (overlap_.orderIndex != -1 && !overlap_.mustWait)
			{
				var realIndex = GetRealIndex(overlap_.orderIndex, overlap_.order1);

				if (realIndex == -1)
				{
					Log(
						$"step not found for overlap " +
						$"index={overlap_.orderIndex} in " +
						(overlap_.order1 ? "order1" : "order2"));
				}
				else
				{
					if (!CanRun(realIndex) || !Ticker(realIndex, deltaTime, overlap_.forwards, false))
					{
						Log(
							$"overlap: step tick finished, getting next");

						// next
						NextOverlap();
					}
				}
			}
		}

		private void NextActive()
		{
			Log("\nin NextActive");

			if (order1_.Count == 0)
			{
				Log("NextActive: no steps");
				active_.orderIndex = -1;
				return;
			}

			if (overlap_.orderIndex == -1)
			{
				Log(
					$"NextActive: initial fwd={active_.forwards} i={active_.orderIndex}");

				int initial = active_.orderIndex;
				if (initial < 0)
					initial = 0;

				bool reversedDir = false;

				for (; ; )
				{
					if (active_.orderIndex == initial && reversedDir)
					{
						Log("NextActive: looped around, no valid step, bailing out");
						active_.orderIndex = -1;
						break;
					}

					if (active_.forwards)
					{
						++active_.orderIndex;
						Log($"NextActive: checking {active_.orderIndex}");

						if (active_.orderIndex >= order1_.Count)
						{
							Log(
								$"NextActive: {active_.orderIndex} past end, reversing");

							active_.forwards = false;
							active_.orderIndex = order1_.Count - 1;
							reversedDir = true;
						}

						Log(
							$"NextActive: i now {active_.orderIndex}");
					}
					else
					{
						if (active_.orderIndex == 0)
						{
							Log($"NextActive: at beginning, going forwards");

							active_.forwards = true;
							order1_ = new List<int>(order2_);
							order2_ = Regenerate(order2_);
							reversedDir = true;
						}
						else
						{
							--active_.orderIndex;

							Log(
								$"NextActive: i now {active_.orderIndex}");
						}
					}

					var realIndex = GetRealIndex(active_.orderIndex, true);

					if (realIndex == -1)
					{
						Log("NextActive: GetRealIndex says it's -1, continuing");
					}
					else if (!CanRun(realIndex))
					{
						Log("NextActive: but it's disabled, continuing");
					}
					else
					{
						Log("NextActive: looks good, taking it");
						break;
					}
				}
			}
			else
			{
				Log("NextActive: overlap already active, taking over");

				// already an overlap, take it over
				active_ = new TickInfo(overlap_);
				active_.mustWait = false;

				Log(
					$"NextActive: i={active_.orderIndex}");

				if (!active_.order1)
				{
					Log($"NextActive: was in order2, swapping");
					order1_ = new List<int>(order2_);
					order2_ = Regenerate(order2_);
					active_.order1 = true;
				}

				overlap_ = new TickInfo();

				return;
			}


			var newRealIndex = GetRealIndex(active_.orderIndex, true);
			if (newRealIndex != -1)
			{
				overlap_.mustWait = false;

				Log(
					$"NextActive: resuming {active_.orderIndex}");

				Resume(newRealIndex);
			}
		}

		private void NextOverlap()
		{
			Log("\nin NextOverlap");

			if (overlap_.mustWait)
			{
				Log("\nNextOverlap: must wait");
				return;
			}


			if (overlap_.orderIndex == -1)
			{
				Log(
					"NextOverlap: no current overlap, starting from active " +
					$"i={active_.orderIndex} fwd={active_.forwards}");

				overlap_ = new TickInfo(active_);
			}
			else
			{
				var realIndex = GetRealIndex(active_.orderIndex, true);

				if (CanRunBackwards(realIndex))
				{
					Log(
						"NextOverlap: current overlap finished but active is " +
						"half move, must wait");

					overlap_.mustWait = true;
					return;
				}
			}

			if (overlap_.orderIndex == -1)
			{
				Log("NextOverlap: no current active, bailing out");
				return;
			}


			bool order1 = true;
			bool reversedDir = false;


			for (; ; )
			{
				if (overlap_.forwards)
				{
					Log("NextOverlap: checking forwards");

					++overlap_.orderIndex;
					Log($"NextOverlap: i now {overlap_.orderIndex}");

					if (overlap_.orderIndex >= (order1 ? order1_ : order2_).Count)
					{
						Log("NextOverlap: past end, reversing");
						overlap_.orderIndex = order1_.Count;
						overlap_.forwards = false;
						reversedDir = true;
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Log(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (fw)");

						overlap_.mustWait = true;
						break;
					}
					else
					{
						var realIndex = GetRealIndex(overlap_.orderIndex, order1);

						if (realIndex == -1)
						{
							Log("NextOverlap: GetRealIndex says it's -1, continuing");
						}
						else if (!CanRun(realIndex))
						{
							Log("NextOverlap: but it's disabled, continuing");
						}
						else
						{
							Log("NextOverlap: looks good, taking it");
							break;
						}
					}
				}
				else
				{
					Log("NextOverlap: checking backwards");

					if (overlap_.orderIndex == 0)
					{
						Log("NextOverlap: past end, reversing");
						overlap_.forwards = true;
						overlap_.orderIndex = -1;

						if (reversedDir)
						{
							Log(
								"NextOverlap: already reversed dir before, " +
								"switching to order2");

							order1 = false;
						}
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Log(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (bw)");

						overlap_.mustWait = true;
						break;
					}
					else
					{
						--overlap_.orderIndex;

						var realIndex = GetRealIndex(overlap_.orderIndex, order1);

						if (realIndex == -1)
						{
							Log(
								$"NextOverlap: no step for {overlap_.orderIndex}, " +
								"continuing");
						}
						else if (!CanRun(realIndex))
						{
							Log(
								$"NextOverlap: step {overlap_.orderIndex} disabled, " +
								"continuing");
						}
						else if (CanRunBackwards(realIndex))
						{
							if (overlap_.orderIndex == active_.orderIndex)
							{
								Log(
									$"NextOverlap: step {overlap_.orderIndex} enabled " +
									"but half move and is same as active; must wait");

								overlap_.mustWait = true;
							}
							else
							{
								Log(
									$"NextOverlap: step {overlap_.orderIndex} enabled " +
									"and half move, taking it");
							}

							break;
						}

						Log(
							$"NextOverlap: step {overlap_.orderIndex} enabled " +
							"but not half move, so doesn't need ticking; continuing");
					}
				}
			}


			if (overlap_.mustWait)
				return;

			var newRealIndex = GetRealIndex(overlap_.orderIndex, order1);
			if (newRealIndex != -1)
			{
				Log(
					$"NextOverlap: resuming {overlap_.orderIndex}");

				Resume(newRealIndex);
			}
		}

		public void ItemInserted(int at)
		{
			if (at >= 0 && at < order1_.Count)
			{
				Log("item inserted");
				order1_.Insert(at, ItemCount() - 1);
				order2_.Insert(at, ItemCount() - 1);
			}
		}

		public void ItemDeleted(int realIndex)
		{
			Log($"item deleted {realIndex}");

			if (active_.orderIndex != -1 && order1_[active_.orderIndex] == realIndex)
			{
				Log("this is the active step, regenerating");
				ItemsChanged();
			}
			else
			{
				Log("not the active step");
				RemoveFromOrder(realIndex, true);
				RemoveFromOrder(realIndex, false);
			}
		}

		private void RemoveFromOrder(int realIndex, bool order1)
		{
			var list = (order1 ? order1_ : order2_);

			var orderIndex = list.IndexOf(realIndex);

			if (orderIndex == -1)
			{
				LogError(
					$"StepDeleted: step index {realIndex} not found in " +
						"order list");

				return;
			}

			if (order1)
			{
				if (orderIndex < active_.orderIndex)
				{
					Log(
						$"is before current in the order, so " +
						$"active was {active_.orderIndex}, now {active_.orderIndex - 1}");

					--active_.orderIndex;
				}
			}

			if (order1 == overlap_.order1)
			{
				if (orderIndex < overlap_.orderIndex)
				{
					Log(
						$"is before current overlap in the order, so " +
						$"overlap was {overlap_.orderIndex}, now {overlap_.orderIndex - 1}");

					--overlap_.orderIndex;
				}
			}


			list.RemoveAt(orderIndex);

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] >= realIndex)
					--list[i];
			}

			Log($"removed from list, now has {list.Count} elements");
		}

		public bool IsRunning(int realIndex)
		{
			if (active_.orderIndex != -1)
			{
				if (GetRealIndex(active_.orderIndex, true) == realIndex)
					return true;
			}

			if (overlap_.orderIndex != -1)
			{
				if (GetRealIndex(overlap_.orderIndex, overlap_.order1) == realIndex)
					return true;
			}

			return false;
		}

		public bool IsActive(int realIndex)
		{
			for (int i = 0; i <= active_.orderIndex; ++i)
			{
				if (GetRealIndex(i, true) == realIndex)
					return true;
			}

			return false;
		}

		public void ItemsChanged()
		{
			Log("items changed, regenerating");

			active_ = new TickInfo();
			overlap_ = new TickInfo();

			order1_ = new List<int>();
			order2_ = new List<int>();
		}

		private void LogError(string s)
		{
			Synergy.LogError($"[{name_}] {s}");
		}

		private void LogErrorST(string s)
		{
			Synergy.LogErrorST($"[{name_}] {s}");
		}

		private void Log(string s)
		{
			Synergy.LogOverlap($"[{name_}] {s}");
		}
	}
}
