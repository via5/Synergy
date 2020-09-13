using System;
using System.Collections.Generic;
using System.Linq;

namespace Synergy
{
	interface IMorphProgression : IFactoryObject
	{
		MorphModifier ParentModifier { get; set; }
		List<SelectedMorph> Morphs { set; }

		bool HasOwnDuration { get; }
		bool Finished { get; }
		float TimeRemaining { get; }

		IMorphProgression Clone(int cloneFlags = 0);
		void Removed();

		void Tick(float deltaTime, float progress, bool firstHalf);
		void TickPaused(float deltaTime);
		void Resume();
		void Reset();
		void Set(bool paused);
		void Stop();

		void MorphAdded(int i);
		void MorphRemoved(int i);
		void MorphsChanged();
	}


	sealed class MorphProgressionFactory : BasicFactory<IMorphProgression>
	{
		public override List<IMorphProgression> GetAllObjects()
		{
			return new List<IMorphProgression>()
			{
				new NaturalMorphProgression(),
				new ConcurrentMorphProgression(),
				new SequentialMorphProgression(),
				new RandomMorphProgression(),
			};
		}
	}


	abstract class BasicMorphProgression : IMorphProgression
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		protected List<SelectedMorph> morphs_ = null;
		private MorphModifier parent_ = null;

		public MorphModifier ParentModifier
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public List<SelectedMorph> Morphs
		{
			set { morphs_ = value; }
		}

		public virtual bool HasOwnDuration { get { return false; } }
		public virtual bool Finished { get { return true; } }
		public virtual float TimeRemaining { get { return 0; } }

		public abstract IMorphProgression Clone(int cloneFlags = 0);

		public virtual void Removed()
		{
			morphs_ = null;
		}

		public abstract void Tick(
			float deltaTime, float progress, bool firstHalf);

		public virtual void TickPaused(float deltaTime)
		{
			foreach (var sm in morphs_)
				sm.TickPaused(deltaTime);
		}

		public virtual void Resume()
		{
			foreach (var sm in morphs_)
				sm.Resume();
		}

		public virtual void Reset()
		{
			foreach (var sm in morphs_)
				sm.Reset();
		}

		public virtual void Set(bool paused)
		{
			if (!paused)
			{
				foreach (var sm in morphs_)
					sm.Set();
			}
		}

		public virtual void Stop()
		{
			// no-op
		}

		public virtual void MorphAdded(int i)
		{
			// no-op
		}

		public virtual void MorphRemoved(int i)
		{
			// no-op
		}

		public virtual void MorphsChanged()
		{
			// no-op
		}

		protected void CopyTo(BasicMorphProgression m, int cloneFlags)
		{
			// no-op
		}

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
	}


	sealed class NaturalMorphProgression : BasicMorphProgression
	{
		private const float NoLastValue = float.MaxValue;

		public static string FactoryTypeName { get; } = "natural";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Natural";
		public override string GetDisplayName() { return DisplayName; }

		private readonly ExplicitHolder<IDuration> masterDuration_ =
			new ExplicitHolder<IDuration>();

		private readonly ExplicitHolder<Delay> masterDelay_ =
			new ExplicitHolder<Delay>();

		private bool stop_ = false;

		public class CustomTarget
		{
			public float time;
			public float movePerTick;
			public float value;
			public float lastValue;

			public CustomTarget(float t, float mpt, float v)
			{
				time = t;
				movePerTick = mpt;
				value = v;
				lastValue = NoLastValue;
			}
		};

		public class MorphInfo
		{
			public IDuration duration;
			public Delay delay;
			public bool inFirstHalf = true;

			public bool stopping = false;
			public bool finished = false;
			public CustomTarget target = null;

			public MorphInfo(IDuration d, Delay dl)
			{
				duration = d;
				delay = dl;
			}

			public void ResetDuration(
				IDuration masterDuration, Delay masterDelay)
			{
				duration = masterDuration.Clone();
				duration.Reset();

				ResetDelay(masterDelay);
			}

			public void ResetDelay(Delay master)
			{
				delay = master.Clone();
				delay.HalfwayDuration.Reset();
				delay.EndForwardsDuration.Reset();
				delay.EndBackwardsDuration.Reset();
			}
		}

		private List<MorphInfo> morphInfos_ = new List<MorphInfo>();
		private float deltaTime_ = 1;

		public NaturalMorphProgression()
		{
			Duration = new RandomDuration(1.0f, 0.0f);
			Delay = new Delay();
		}

		public MorphInfo GetMorphInfoFor(SelectedMorph sm)
		{
			var i = morphs_.IndexOf(sm);
			if (i == -1)
				return null;

			return morphInfos_[i];
		}

		public IDuration Duration
		{
			get
			{
				return masterDuration_.HeldValue;
			}

			set
			{
				masterDuration_.HeldValue?.Removed();
				masterDuration_.Set(value);
			}
		}

		public bool Stopping
		{
			get { return stop_; }
		}

		public Delay Delay
		{
			get
			{
				return masterDelay_.HeldValue;
			}

			set
			{
				masterDelay_.HeldValue?.Removed();
				masterDelay_.Set(value);
			}
		}

		public override bool HasOwnDuration
		{
			get { return true; }
		}

		public override bool Finished
		{
			get
			{
				foreach (var mi in morphInfos_)
				{
					if (!mi.finished)
						return false;
				}

				return true;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				float longest = 0;

				for (int i = 0; i < morphs_.Count; ++i)
				{
					var t =
						morphInfos_[i].duration.TimeRemaining +
						GetTargetForDefaultValue(i).time;

					longest = Math.Max(longest, t);
				}

				return longest;
			}
		}

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new NaturalMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		private void CopyTo(NaturalMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.Duration = Duration.Clone(cloneFlags);
			m.Delay = Delay.Clone(cloneFlags);
		}

		public override void Removed()
		{
			Duration = null;
			Delay = null;
		}

		private CustomTarget GetTargetForDefaultValue(int i)
		{
			return CreateCustomTarget(i, morphs_[i].Morph.startValue);
		}

		private CustomTarget CreateCustomTarget(int i, float v)
		{
			var mi = morphInfos_[i];
			var sm = morphs_[i];

			float current = 0;
			if (mi.duration != null)
				current = mi.duration.Current;

			var tickCount = current / deltaTime_;

			float movePerTick = -1.0f;
			if (tickCount > 0)
				movePerTick = (sm.Movement.CurrentRange.Distance * 2) / tickCount;

			movePerTick = Math.Max(0.01f, movePerTick);

			var moveNeeded = Math.Abs(sm.Morph.morphValue - v);
			var ticksNeeded = moveNeeded / movePerTick;
			var timeNeeded = ticksNeeded * deltaTime_;

			return new CustomTarget(timeNeeded, movePerTick, v);
		}

		public override void Tick(
			float deltaTime, float unusedProgress, bool unusedFirstHalf)
		{
			deltaTime_ = deltaTime;

			for (int i = 0; i < morphInfos_.Count; ++i)
			{
				var mi = morphInfos_[i];
				var sm = morphs_[i];

				if (stop_ && mi.finished)
					continue;

				if (mi.target != null)
				{
					if (!DoTargetTick(i))
					{
						if (stop_ && mi.stopping)
						{
							morphs_[i].Reset();
							mi.finished = true;
							continue;
						}
					}
					else
					{
						continue;
					}
				}


				if (mi.delay.ActiveType != Delay.None)
				{
					mi.delay.ActiveDuration.Tick(deltaTime);
					if (!mi.delay.ActiveDuration.Finished)
						continue;

					if (mi.delay.ResetDurationAfter)
					{
						mi.ResetDuration(Duration, Delay);
						mi.delay.ResetDurationAfter = false;
					}
					else
					{
						mi.ResetDelay(Delay);
					}

					mi.delay.ActiveType = Delay.None;
				}


				mi.duration.Tick(deltaTime);

				var p = mi.duration.InFirstHalf ?
					mi.duration.FirstHalfProgress :
					mi.duration.SecondHalfProgress;

				var fh = mi.duration.InFirstHalf;

				sm.Tick(deltaTime, p, fh);

				if (mi.duration.Finished)
				{
					if (stop_)
					{
						mi.target = GetTargetForDefaultValue(i);
						mi.stopping = true;
					}
					else
					{
						if (mi.delay.EndForwards)
						{
							mi.delay.ActiveType = Delay.EndForwardsType;
							mi.delay.ResetDurationAfter = true;
						}
						else
						{
							mi.ResetDuration(Duration, Delay);
						}
					}
				}
				else
				{
					if ((mi.inFirstHalf && !fh) && mi.delay.Halfway)
					{
						mi.delay.ActiveType = Delay.HalfwayType;
					}
				}

				mi.inFirstHalf = fh;
			}
		}

		private bool DoTargetTick(int i)
		{
			var mi = morphInfos_[i];
			var sm = morphs_[i];

			var vbefore = sm.Morph.morphValue;

			if (mi.target.lastValue != NoLastValue)
			{
				if (mi.target.lastValue != vbefore)
				{
					// something else changed the value of this morph,
					// there might be another modifier in another step that's
					// holding the value in TickPaused(), or whatever
					//
					// this is never going to work, so just give up
					mi.target = null;
					return false;
				}
			}

			if (sm.Enabled)
			{
				if (sm.Morph.morphValue < mi.target.value)
				{
					sm.Morph.morphValue += mi.target.movePerTick;
					if (sm.Morph.morphValue > mi.target.value)
						sm.Morph.morphValue = mi.target.value;
				}
				else
				{
					sm.Morph.morphValue -= mi.target.movePerTick;
					if (sm.Morph.morphValue < mi.target.value)
						sm.Morph.morphValue = mi.target.value;
				}
			}

			var vafter = sm.Morph.morphValue;

			if (vbefore == vafter)
			{
				// target looks out of range
				mi.target = null;
				return false;
			}

			mi.target.lastValue = vafter;

			var d = Math.Abs(sm.Morph.morphValue - mi.target.value);

			if (d < 0.01f)
			{
				sm.Morph.morphValue = mi.target.value;
				mi.target = null;
				return false;
			}

			return true;
		}

		public override void Set(bool paused)
		{
			if (paused)
				return;

			for (int i = 0; i < morphInfos_.Count; ++i)
			{
				if (morphInfos_[i].target != null)
					continue;

				morphs_[i].Set();
			}
		}

		public override void Stop()
		{
			if (!stop_)
				stop_ = true;
		}

		public override void Resume()
		{
			base.Resume();

			for (int i = 0; i < morphs_.Count; ++i)
			{
				var mi = morphInfos_[i];

				mi.stopping = false;
				mi.finished = false;
				mi.target = CreateCustomTarget(i, morphs_[i].Movement.Magnitude);
			}

			stop_ = false;
		}

		public override void Reset()
		{
			foreach (var mi in morphInfos_)
			{
				mi.stopping = false;
				mi.finished = false;
				mi.target = null;
				mi.ResetDuration(Duration, Delay);
			}

			foreach (var sm in morphs_)
				sm.Reset();
		}

		public override void MorphAdded(int i)
		{
			var mi = new MorphInfo(
				Duration.Clone(), Delay.Clone());

			morphInfos_.Insert(i, mi);
		}

		public override void MorphRemoved(int i)
		{
			morphInfos_.RemoveAt(i);
		}

		public override void MorphsChanged()
		{
			morphInfos_ = new List<MorphInfo>();

			for (int i = 0; i < morphs_.Count; ++i)
				MorphAdded(morphInfos_.Count);
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("duration", Duration);
			o.Add("delay", Delay);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("NaturalMorphProgression");
			if (o == null)
				return false;

			{
				IDuration d = null;
				o.Opt<DurationFactory, IDuration>("duration", ref d);
				Duration = d;
			}

			{
				Delay d = null;
				o.Opt("delay", ref d);
				Delay = d;
			}

			return true;
		}
	}


	sealed class ConcurrentMorphProgression : BasicMorphProgression
	{
		public static string FactoryTypeName { get; } = "concurrent";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Concurrent";
		public override string GetDisplayName() { return DisplayName; }

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new ConcurrentMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		public override void Tick(
			float deltaTime, float progress, bool firstHalf)
		{
			foreach (var sm in morphs_)
				sm.Tick(deltaTime, progress, firstHalf);
		}

		private void CopyTo(ConcurrentMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("ConcurrentMorphProgression");
			if (o == null)
				return false;

			return true;
		}
	}


	abstract class OrderedMorphProgression : BasicMorphProgression
	{
		private readonly BoolParameter holdHalfway_ =
			new BoolParameter("HoldHalfway", false);

		private Overlapper o_ = new Overlapper("morph");
		private List<SelectedMorph> enabledMorphs_ = new List<SelectedMorph>();
		private float overlapTime_ = -1;


		public OrderedMorphProgression(bool holdHalfway = false)
		{
			HoldHalfway = holdHalfway;

			o_.CanRun += i => true;
			o_.Resume += (i) => { enabledMorphs_[i].Resume(); return true; };
			o_.CanRunBackwards += i => holdHalfway_.Value;
			o_.Reset += (i) => { enabledMorphs_[i].Reset(); return true; };
			o_.Ticker += TickMorph;
			o_.TimeRemaining += i => GetTimeRemainingForMorph(i);
			o_.Regenerate += (old) => Regenerate(old, enabledMorphs_.Count);
			o_.ItemCount += () => enabledMorphs_.Count;
			o_.GetOverlapTime += () => OverlapTime;
		}

		public bool HoldHalfway
		{
			get { return holdHalfway_.Value; }
			set { holdHalfway_.Value = value; }
		}

		public BoolParameter HoldHalfwayParameter
		{
			get { return holdHalfway_; }
		}

		public Overlapper Overlapper
		{
			get { return o_; }
		}

		public float OverlapTime
		{
			get
			{
				float t = 0;

				if (OverrideOverlapTime)
					t = overlapTime_;
				else
					t = Synergy.Instance.Options.OverlapTime;

				if (enabledMorphs_.Count > 0)
					t = Math.Min(t, ParentModifier.CurrentDuration / enabledMorphs_.Count);

				return t;
			}

			set
			{
				overlapTime_ = value;
			}
		}

		public bool OverrideOverlapTime
		{
			get { return overlapTime_ >= 0; }
		}


		public float GetTimeRemainingForMorph(int i)
		{
			if (enabledMorphs_.Count == 0)
				return 0;

			float overlapTime = OverlapTime;

			float duration = ParentModifier.CurrentDuration;
			float remaining = ParentModifier.TimeRemaining;
			float timePerMorph = duration / enabledMorphs_.Count;
			float passed = duration - ParentModifier.TimeRemaining;
			float startTime = timePerMorph * (enabledMorphs_.Count - i - 1);

			float f = 0;

			if (enabledMorphs_.Count > 1 && overlapTime > 0 && i == 0 && (passed >= (timePerMorph * (enabledMorphs_.Count - 1))))
				f = Utilities.Clamp((timePerMorph + overlapTime) - (overlapTime - remaining), 0, timePerMorph + overlapTime);
			else
				f = Utilities.Clamp(remaining - startTime, 0, timePerMorph);

			return f;
		}


		private float GetStrictProgressForMorph(int i)
		{
			if (enabledMorphs_.Count == 0)
				return 0;

			float overlapTime = OverlapTime;

			float duration = ParentModifier.CurrentDuration;
			float timePerMorph = (duration / enabledMorphs_.Count);
			float passed = duration - ParentModifier.TimeRemaining;
			float startTime = timePerMorph * i;
			float morphPassed = Utilities.Clamp(passed - startTime, 0, timePerMorph);
			float p = morphPassed / timePerMorph;

			return p;
		}

		public float GetProgressForMorph(int i)
		{
			if (enabledMorphs_.Count == 0)
				return 0;

			float overlapTime = OverlapTime;

			float duration = ParentModifier.CurrentDuration;
			float timePerMorph = (duration / enabledMorphs_.Count);
			float remaining = ParentModifier.TimeRemaining;
			float passed = duration - ParentModifier.TimeRemaining;
			float startTime = timePerMorph * i;

			float p = 0;

			if (timePerMorph == 0)
				return 0;

			if (enabledMorphs_.Count > 1 && overlapTime > 0 && i == 0 && (passed >= (timePerMorph * (enabledMorphs_.Count - 1))))
			{
				float morphPassed = Utilities.Clamp((overlapTime - remaining), 0, timePerMorph + overlapTime);
				p = morphPassed / (timePerMorph + overlapTime);
			}
			else
			{
				if (enabledMorphs_.Count > 1 && overlapTime > 0)
				{
					float morphPassed = Utilities.Clamp(passed - startTime + overlapTime, 0, timePerMorph + overlapTime);
					p = morphPassed / (timePerMorph + overlapTime);
				}
				else
				{
					float morphPassed = Utilities.Clamp(passed - startTime, 0, timePerMorph);
					p = morphPassed / timePerMorph;
				}
			}

			return p;
		}

		private bool TickMorph(int i, float deltaTime, bool stepForwards, bool paused)
		{
			if (paused)
			{
				enabledMorphs_[i].TickPaused(deltaTime);
				return true;
			}
			else
			{
				float lp = GetStrictProgressForMorph(i);
				float p = GetProgressForMorph(i);
				bool fwd;

				if (p <= 0.5f)
				{
					fwd = true;
					p = p / 0.5f;
				}
				else
				{
					fwd = false;
					p = (p - 0.5f) / 0.5f;
				}

				enabledMorphs_[i].Tick(deltaTime, p, fwd);

				if (!fwd && lp == 1)
					return false;

				return true;
			}
		}


		protected void CopyTo(OrderedMorphProgression m, int cloneFlags)
		{
			m.holdHalfway_.Value = holdHalfway_.Value;
		}

		public override void Removed()
		{
			base.Removed();
			holdHalfway_.Unregister();
		}

		public override void Resume()
		{
			base.Resume();

			if (GatherEnabledMorphs())
				o_.ItemsChanged();
		}

		public override void Tick(
			float deltaTime, float progress, bool firstHalf)
		{
			o_.Tick(deltaTime);
		}

		public override void Set(bool paused)
		{
			if (paused && !HoldHalfway)
				return;

			foreach (var sm in enabledMorphs_)
				sm.Set();
		}

		public override void MorphAdded(int i)
		{
			GatherEnabledMorphs();
			o_.ItemInserted(i);
		}

		public override void MorphRemoved(int i)
		{
			GatherEnabledMorphs();
			o_.ItemDeleted(i);
		}

		public override void MorphsChanged()
		{
			GatherEnabledMorphs();
			o_.ItemsChanged();
		}

		private bool GatherEnabledMorphs()
		{
			var newList = new List<SelectedMorph>();

			foreach (var sm in morphs_)
			{
				if (sm.Enabled)
				{
					newList.Add(sm);
				}
				else
				{
					sm.Reset();
					sm.ResetMorphValue();
				}
			}

			if (!newList.SequenceEqual(enabledMorphs_))
			{
				enabledMorphs_ = newList;
				return true;
			}

			return false;
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("holdHalfway", HoldHalfway);
			o.Add("overlapTime", overlapTime_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("SequentialMorphProgression");
			if (o == null)
				return false;

			overlapTime_ = -1;

			o.Opt("holdHalfway", holdHalfway_);
			o.Opt("overlapTime", ref overlapTime_);

			return true;
		}

		protected abstract List<int> Regenerate(List<int> old, int count);
	}


	sealed class SequentialMorphProgression : OrderedMorphProgression
	{
		public static string FactoryTypeName { get; } = "sequential";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Sequential";
		public override string GetDisplayName() { return DisplayName; }

		public SequentialMorphProgression(bool holdHalfway = false)
			: base(holdHalfway)
		{
		}

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new SequentialMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		private void CopyTo(SequentialMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		protected override List<int> Regenerate(List<int> old, int count)
		{
			var list = new List<int>();

			for (int i = 0; i < count; ++i)
				list.Add(i);

			return list;
		}
	}


	sealed class RandomMorphProgression : OrderedMorphProgression
	{
		public static string FactoryTypeName { get; } = "random";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random";
		public override string GetDisplayName() { return DisplayName; }

		private readonly ShuffledOrder order_ = new ShuffledOrder();

		public RandomMorphProgression(bool holdHalfway = false)
			: base(holdHalfway)
		{
		}

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new RandomMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		private void CopyTo(RandomMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		protected override List<int> Regenerate(List<int> old, int count)
		{
			return ShuffledOrder.Shuffle(old, count);
		}
	}
}
