using System;
using System.Collections.Generic;
using Leap;
using Leap.Unity;
using SimpleJSON;

namespace Synergy
{
	class SelectedMorph : IJsonable
	{
		private const float NoMagnitude = float.MinValue;

		private Atom atom_ = null;
		private DAZMorph morph_ = null;
		private BoolParameter enabled_ = new BoolParameter("MorphEnabled", true);
		private Movement movement_ = null;
		private float magnitude_ = NoMagnitude;

		public SelectedMorph()
		{
			morph_ = null;
			Movement = new Movement();
		}

		public static SelectedMorph Create(Atom atom, DAZMorph morph)
		{
			var sm = new SelectedMorph();

			sm.Atom = atom;
			sm.Morph = morph;

			return sm;
		}

		public FloatRange PreferredRange
		{
			get
			{
				if (morph_ == null)
					return new FloatRange(0, 1);
				else
					return new FloatRange(morph_.min, morph_.max);
			}
		}

		public Atom Atom
		{
			get { return atom_; }
			set { atom_ = value; }
		}

		public DAZMorph Morph
		{
			get { return morph_; }

			set { morph_ = value; }
		}

		public bool Enabled
		{
			get
			{
				return enabled_.Value;
			}

			set
			{
				if (!value)
					Reset();

				enabled_.Value = value;
			}
		}

		public BoolParameter EnabledParameter
		{
			get { return enabled_; }
		}

		public Movement Movement
		{
			get { return movement_; }
			set { movement_ = value; }
		}


		public string DisplayName
		{
			get
			{
				if (morph_ == null)
					return "";
				else
					return morph_.displayName;
			}
		}

		public SelectedMorph Clone(int cloneFlags)
		{
			var sm = new SelectedMorph();
			CopyTo(sm, cloneFlags);
			return sm;
		}

		public void CopyTo(SelectedMorph sm, int cloneFlags)
		{
			sm.atom_ = atom_;
			sm.morph_ = morph_;
			sm.enabled_ = enabled_;
			sm.movement_ = movement_?.Clone(cloneFlags);
			sm.magnitude_ = magnitude_;
		}

		public void Resume()
		{
			// no-op
		}

		public void Reset()
		{
			magnitude_ = NoMagnitude;

			if (morph_ != null && Enabled)
				morph_.morphValue = morph_.startValue;
		}

		public void Tick(float deltaTime, float progress, bool firstHalf)
		{
			movement_.Tick(deltaTime, progress, firstHalf);
			magnitude_ = Movement.Magnitude;
		}

		public void Set()
		{
			if (!Enabled)
				return;

			if (magnitude_ == NoMagnitude)
				Reset();
			else if (morph_ != null)
				morph_.morphValue = magnitude_;
		}


		public J.Node ToJSON()
		{
			var o = new J.Object();

			if (morph_ != null)
				o.Add("uid", morph_.uid);

			o.Add("movement", movement_);
			o.Add("enabled", enabled_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("SelectedMorph");
			if (o == null)
				return false;

			string uid = "";
			o.Opt("uid", ref uid);

			if (Atom != null && uid != "")
			{
				morph_ = Utilities.GetAtomMorph(Atom, uid);

				if (morph_  == null)
				{
					Synergy.LogError(
						"morph '" + uid + "' not found in " +
						"atom '" + Atom.uid + "'");

					return false;
				}
			}

			o.Opt("movement", ref movement_);
			o.Opt("enabled", ref enabled_);

			return true;
		}
	}


	interface IMorphProgression : IFactoryObject
	{
		List<SelectedMorph> Morphs { set; }

		bool HasOwnDuration { get; }
		bool Finished { get; }
		float TimeRemaining { get; }

		IMorphProgression Clone(int cloneFlags = 0);

		void Tick(float deltaTime, float progress, bool firstHalf);
		void TickPaused(float deltaTime);
		void Resume();
		void Reset();
		void Set(bool paused);
		void Stop(float timeRemaining);

		void MorphAdded(int i);
		void MorphRemoved(int i);
		void MorphsChanged();
	}


	class MorphProgressionFactory : BasicFactory<IMorphProgression>
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

		public List<SelectedMorph> Morphs
		{
			set { morphs_ = value; }
		}

		public virtual bool HasOwnDuration { get { return false; } }
		public virtual bool Finished { get { return true; } }
		public virtual float TimeRemaining { get { return 0; } }

		public abstract IMorphProgression Clone(int cloneFlags = 0);

		public abstract void Tick(
			float deltaTime, float progress, bool firstHalf);

		public virtual void TickPaused(float deltaTime)
		{
			// no-op
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

		public virtual void Stop(float timeRemaining)
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


	class NaturalMorphProgression : BasicMorphProgression
	{
		private const float NoLastValue = float.MaxValue;

		public static string FactoryTypeName { get; } = "natural";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Natural";
		public override string GetDisplayName() { return DisplayName; }

		private IDuration masterDuration_ = null;
		private Delay masterDelay_ = null;
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
				delay.Duration.Reset();
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
				return masterDuration_;
			}

			set
			{
				masterDuration_ = value;
			}
		}

		public bool Stopping
		{
			get { return stop_; }
		}

		public Delay Delay
		{
			get { return masterDelay_; }
			set { masterDelay_ = value; }
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


				if (mi.delay.Active)
				{
					mi.delay.Duration.Tick(deltaTime);
					if (!mi.delay.Duration.Finished)
						continue;

					mi.delay.Active = false;

					if (mi.delay.ResetDurationAfter)
					{
						mi.ResetDuration(masterDuration_, masterDelay_);
						mi.delay.ResetDurationAfter = false;
					}
					else
					{
						mi.ResetDelay(masterDelay_);
					}
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
							mi.delay.Active = true;
							mi.delay.ResetDurationAfter = true;
						}
						else
						{
							mi.ResetDuration(masterDuration_, masterDelay_);
						}
					}
				}
				else
				{
					if ((mi.inFirstHalf && !fh) && mi.delay.Halfway)
					{
						mi.delay.Active = true;
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

		public override void Stop(float timeRemaining)
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
				mi.ResetDuration(masterDuration_, masterDelay_);
			}
		}

		protected void CopyTo(NaturalMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		public override void MorphAdded(int i)
		{
			var mi = new MorphInfo(
				masterDuration_.Clone(), masterDelay_.Clone());

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

			o.Add("duration", masterDuration_);
			o.Add("delay", masterDelay_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("NaturalMorphProgression");
			if (o == null)
				return false;

			o.Opt<DurationFactory, IDuration>("duration", ref masterDuration_);
			o.Opt("delay", ref masterDelay_);

			return true;
		}
	}


	class ConcurrentMorphProgression : BasicMorphProgression
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

		protected void CopyTo(ConcurrentMorphProgression m, int cloneFlags)
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
		private bool holdHalfway_ = false;


		public bool HoldHalfway
		{
			get { return holdHalfway_; }
			set { holdHalfway_ = value; }
		}


		protected void CopyTo(OrderedMorphProgression m, int cloneFlags)
		{
			m.holdHalfway_ = holdHalfway_;
		}

		public override void Tick(
			float deltaTime, float progress, bool firstHalf)
		{
			float singleMorph = 1.0f / morphs_.Count;
			float p = progress;

			if (holdHalfway_)
			{
				for (int i = 0; i < morphs_.Count; ++i)
				{
					float singleMorphProgress = Utilities.Clamp(
						p / singleMorph, 0.0f, 1.0f);

					p -= singleMorph;

					var m = morphs_[GetMorphIndex(i)];
					m.Tick(deltaTime, singleMorphProgress, firstHalf);
				}
			}
			else
			{
				if (firstHalf)
					p /= 2;
				else
					p = 0.5f + (p / 2);

				for (int i = 0; i < morphs_.Count; ++i)
				{
					float singleMorphProgress = Utilities.Clamp(
						p / singleMorph, 0.0f, 1.0f);

					p -= singleMorph;

					if (singleMorphProgress <= 0.5f)
					{
						firstHalf = true;
						singleMorphProgress = singleMorphProgress / 0.5f;
					}
					else
					{
						firstHalf = false;
						singleMorphProgress = (singleMorphProgress - 0.5f) / 0.5f;
					}

					var m = morphs_[GetMorphIndex(i)];
					m.Tick(deltaTime, singleMorphProgress, firstHalf);
				}
			}

			if (!firstHalf && progress >= 1.0f)
				Reorder();
		}

		public override void Set(bool paused)
		{
			if (paused && !holdHalfway_)
				return;

			foreach (var sm in morphs_)
				sm.Set();
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("holdHalfway", holdHalfway_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("SequentialMorphProgression");
			if (o == null)
				return false;

			o.Opt("holdHalfway", ref holdHalfway_);

			return true;
		}

		protected abstract int GetMorphIndex(int i);

		protected virtual void Reorder()
		{
			// no-op
		}
	}


	class SequentialMorphProgression : OrderedMorphProgression
	{
		public static string FactoryTypeName { get; } = "sequential";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Sequential";
		public override string GetDisplayName() { return DisplayName; }

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new SequentialMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(SequentialMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		protected override int GetMorphIndex(int i)
		{
			return i;
		}
	}


	class RandomMorphProgression : OrderedMorphProgression
	{
		public static string FactoryTypeName { get; } = "random";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random";
		public override string GetDisplayName() { return DisplayName; }

		private readonly ShuffledOrder order_ = new ShuffledOrder();

		public override IMorphProgression Clone(int cloneFlags = 0)
		{
			var p = new RandomMorphProgression();
			CopyTo(p, cloneFlags);
			return p;
		}

		protected void CopyTo(RandomMorphProgression m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
		}

		protected override int GetMorphIndex(int i)
		{
			return order_.Get(i);
		}

		public override void MorphAdded(int i)
		{
			order_.Add(i);
		}

		public override void MorphRemoved(int i)
		{
			order_.Remove(i);
		}

		public override void MorphsChanged()
		{
			Reorder();
		}

		protected override void Reorder()
		{
			order_.Shuffle(morphs_.Count);
		}
	}


	class MorphModifier : AtomModifier
	{
		private readonly List<SelectedMorph> morphs_ =
			new List<SelectedMorph>();
		private IMorphProgression progression_ = null;


		public static string FactoryTypeName { get; } = "morph";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Morph";
		public override string GetDisplayName() { return DisplayName; }


		public MorphModifier()
		{
			if (!Utilities.AtomHasMorphs(Atom))
				Atom = null;

			Progression = new NaturalMorphProgression();
		}

		public MorphModifier(Atom a, DAZMorph m)
		{
			Atom = a;
			Progression = new NaturalMorphProgression();
			AddMorph(m);
		}

		public override FloatRange PreferredRange
		{
			get
			{
				if (morphs_.Count > 0)
					return morphs_[0].PreferredRange;
				else
					return new FloatRange(0, 1);
			}
		}

		public List<SelectedMorph> Morphs
		{
			get
			{
				return morphs_;
			}
		}

		public IMorphProgression Progression
		{
			get
			{
				return progression_;
			}

			set
			{
				if (progression_ != null)
					progression_.Morphs = null;

				progression_ = value;

				if (progression_ != null)
				{
					progression_.Morphs = morphs_;
					progression_.MorphsChanged();
				}
			}
		}

		public override bool Finished
		{
			get
			{
				if (progression_.HasOwnDuration)
					return progression_.Finished;
				else
					return base.Finished;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				if (progression_.HasOwnDuration)
					return progression_.TimeRemaining;
				else
					return base.TimeRemaining;
			}
		}

		public override void Stop(float timeRemaining)
		{
			base.Stop(timeRemaining);

			if (progression_.HasOwnDuration)
				progression_.Stop(timeRemaining);
		}


		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new MorphModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected void CopyTo(MorphModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);

			foreach (var sm in morphs_)
				m.morphs_.Add(sm.Clone(cloneFlags));

			m.Progression = progression_?.Clone(cloneFlags);
		}

		public override void AboutToBeRemoved()
		{
			base.AboutToBeRemoved();

			foreach (var m in morphs_)
				m.Reset();
		}

		public SelectedMorph AddMorph(DAZMorph m)
		{
			return AddMorph(m, new Movement(0, 1));
		}

		public SelectedMorph AddMorph(DAZMorph m, FloatRange r)
		{
			return AddMorph(m, new Movement(r));
		}

		public SelectedMorph AddMorph(DAZMorph m, Movement mv)
		{
			if (m == null)
				return null;

			var sm = SelectedMorph.Create(Atom, m);

			if (mv != null)
				sm.Movement = mv;

			AddMorph(sm);

			return sm;
		}

		public void AddMorph(SelectedMorph sm)
		{
			morphs_.Add(sm);
			progression_.MorphAdded(morphs_.Count - 1);
			FireNameChanged();
		}

		public void RemoveMorph(DAZMorph m)
		{
			for (int i = 0; i < morphs_.Count; ++i)
			{
				if (morphs_[i].Morph == m)
				{
					var sm = morphs_[i];
					morphs_.RemoveAt(i);

					sm.Reset();
					progression_.MorphRemoved(i);

					break;
				}
			}

			FireNameChanged();
		}

		public bool HasMorph(DAZMorph m)
		{
			foreach (var sm in morphs_)
			{
				if (sm.Morph == m)
					return true;
			}

			return false;
		}

		public void SetMorphs(List<DAZMorph> morphs)
		{
			morphs_.Clear();

			foreach (var m in morphs)
				morphs_.Add(SelectedMorph.Create(Atom, m));

			progression_.MorphsChanged();
		}

		protected override void DoResume()
		{
			progression_.Resume();
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);
			progression_.Set(paused);
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			progression_.Tick(deltaTime, progress, firstHalf);
		}

		protected override void DoTickPaused(float deltaTime)
		{
			base.DoTickPaused(deltaTime);
			progression_.TickPaused(deltaTime);
		}

		public override void Reset()
		{
			base.Reset();
			progression_.Reset();
		}

		protected override string MakeName()
		{
			const int Max = 3;

			string n = "M ";

			if (Atom == null)
				n += "none";
			else
				n += Atom.name;

			for (int i = 0; i < Max; ++i)
			{
				if (i >= morphs_.Count)
					break;

				if (n != "")
					n += ", ";

				n += morphs_[i].Morph.displayName;
			}

			if (n == "")
				n = "none";

			return n;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("selectedMorphs", morphs_);
			o.Add("progression", progression_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("MorphModifier");
			if (o == null)
				return false;

			if (Atom != null)
			{
				var morphsArray = o.Get("selectedMorphs").AsArray();

				if (morphsArray != null)
				{
					morphsArray.ForEach((morphNode) =>
					{
						var sm = SelectedMorph.Create(Atom, null);

						if (sm.FromJSON(morphNode))
							morphs_.Add(sm);
					});
				}

				o.Opt<MorphProgressionFactory, IMorphProgression>(
					"progression", ref progression_);

				if (progression_ != null)
				{
					progression_.Morphs = morphs_;
					progression_.MorphsChanged();
				}
			}

			return true;
		}
	}
}
