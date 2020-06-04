using System.Collections.Generic;

namespace Synergy
{
	interface IModifierSync : IFactoryObject
	{
		IModifier ParentModifier { get; set; }
		IModifierSync Clone(int cloneFlags = 0);
		void StopWhenFinished(float timeRemaining);
		void Resume();
		bool Tick(float deltaTime);
		void TickPaused(float deltaTime);
		bool TickDelayed(float deltaTime);
		void PostTick();
		float GetProgress(IModifier m, float stepProgress, bool stepForwards);
		bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards);
		bool Finished { get; }
		float TimeRemaining { get; }
		void Reset();

		bool MustStopWhenFinished { get; }
		float StopGracePeriod { get; }
	}

	class ModifierSyncFactory : BasicFactory<IModifierSync>
	{
		public override List<IModifierSync> GetAllObjects()
		{
			return new List<IModifierSync>()
			{
				new DurationSyncedModifier(),
				new StepProgressSyncedModifier(),
				new OtherModifierSyncedModifier(),
				new UnsyncedModifier()
			};
		}
	}

	abstract class BasicModifierSync : IModifierSync
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		private IModifier parent_ = null;
		private float gracePeriod_ = -1;

		public IModifier ParentModifier
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public void StopWhenFinished(float gracePeriod)
		{
			gracePeriod_ = gracePeriod;
		}

		public void Resume()
		{
			gracePeriod_ = -1;
		}

		public abstract IModifierSync Clone(int cloneFlags = 0);

		public abstract bool Finished { get; }
		public abstract float TimeRemaining { get; }

		public abstract bool Tick(float deltaTime);
		public abstract void TickPaused(float deltaTime);
		public abstract bool TickDelayed(float deltaTime);
		public abstract void PostTick();
		public abstract float GetProgress(IModifier m, float stepProgress, bool stepForwards);
		public abstract bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards);
		public abstract void Reset();

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);

		public bool MustStopWhenFinished
		{
			get { return gracePeriod_ > 0; }
		}

		public float StopGracePeriod
		{
			get { return gracePeriod_; }
		}
	}


	class DurationSyncedModifier : BasicModifierSync
	{
		public static string FactoryTypeName { get; } = "duration";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Step duration";
		public override string GetDisplayName() { return DisplayName; }

		public override IModifierSync Clone(int cloneFlags = 0)
		{
			return new DurationSyncedModifier();
		}

		public override bool Finished
		{
			get { return true; }
		}

		public override float TimeRemaining
		{
			get { return 0; }
		}

		public override bool Tick(float deltaTime)
		{
			// no-op
			return true;
		}

		public override void TickPaused(float deltaTime)
		{
			// no-op
		}

		public override bool TickDelayed(float deltaTime)
		{
			// no-op
			return false;
		}

		public override void PostTick()
		{
			// no-op
		}

		public override float GetProgress(IModifier m, float stepProgress, bool stepForwards)
		{
			return stepProgress;
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			return stepForwards;
		}

		public override void Reset()
		{
			// no-op
		}

		public override J.Node ToJSON()
		{
			return new J.Object();
		}

		public override bool FromJSON(J.Node n)
		{
			// no-op
			return true;
		}
	}


	class UnsyncedModifier : BasicModifierSync
	{
		public static string FactoryTypeName { get; } = "unsynced";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Unsynced";
		public override string GetDisplayName() { return DisplayName; }

		private IDuration duration_ = new RandomDuration();
		private Delay delay_ = new Delay();
		private bool inFirstHalf_ = true;


		public UnsyncedModifier()
		{
		}

		public UnsyncedModifier(IDuration d, Delay delay = null)
		{
			duration_ = d;

			if (delay != null)
				delay_ = delay;
		}

		public override bool Finished
		{
			get
			{
				return duration_.Finished;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				if (duration_ == null)
					return 0;

				var t = duration_.TimeRemaining;

				if ((inFirstHalf_ && delay_.Halfway) || delay_.Active)
					t += delay_.Duration.TimeRemaining;

				return t;
			}
		}

		public override IModifierSync Clone(int cloneFlags)
		{
			var m = new UnsyncedModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected void CopyTo(UnsyncedModifier m, int cloneFlags)
		{
			m.duration_ = duration_?.Clone(cloneFlags);
			m.delay_ = delay_.Clone(cloneFlags);
		}

		public IDuration Duration
		{
			get { return duration_; }
			set { duration_ = value; }
		}

		public Delay Delay
		{
			get { return delay_; }
			set { delay_ = value; }
		}

		public override bool Tick(float deltaTime)
		{
			if (delay_.Active)
				return DoDelay(deltaTime);

			duration_.Tick(deltaTime);

			return true;
		}

		public override void TickPaused(float deltaTime)
		{
			// no-op
		}

		public override bool TickDelayed(float deltaTime)
		{
			return true;
		}

		public override void PostTick()
		{
			if (duration_.Finished)
			{
				if (delay_.EndForwards)
				{
					if (MustStopWhenFinished)
					{
						if (delay_.Duration.Current >= StopGracePeriod)
							return;
					}

					delay_.Active = true;
					delay_.StopAfter = true;
					delay_.ResetDurationAfter = true;
				}
				else
				{
					if (MustStopWhenFinished)
						duration_.Reset(StopGracePeriod);
					else
						Reset();

					return;
				}
			}
			else
			{
				var firstHalf = duration_.InFirstHalf;

				if ((inFirstHalf_ && !firstHalf) && delay_.Halfway)
				{
					inFirstHalf_ = firstHalf;
					delay_.Active = true;
				}
				else
				{
					inFirstHalf_ = firstHalf;
				}
			}
		}

		public override float GetProgress(IModifier m, float stepProgress, bool stepForwards)
		{
			if (IsInFirstHalf(m, stepProgress, stepForwards))
				return duration_.FirstHalfProgress;
			else
				return duration_.SecondHalfProgress;
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			return duration_.InFirstHalf;
		}

		public override void Reset()
		{
			duration_.Reset();
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("duration", duration_);
			o.Add("delay", delay_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("UnsyncedModifier");
			if (o == null)
				return false;

			o.Opt<DurationFactory, IDuration>("duration", ref duration_);
			o.Opt("delay", ref delay_);

			return true;
		}

		private bool DoDelay(float deltaTime)
		{
			delay_.Duration.Tick(deltaTime);

			if (!delay_.Duration.Finished)
				return true;

			delay_.Duration.Reset();
			delay_.Active = false;

			if (delay_.StopAfter)
			{
				delay_.StopAfter = false;

				if (delay_.ResetDurationAfter)
				{
					delay_.ResetDurationAfter = false;

					if (MustStopWhenFinished)
						duration_.Reset(StopGracePeriod);
					else
						Reset();
				}

				return false;
			}

			return true;
		}
	}


	class StepProgressSyncedModifier : BasicModifierSync
	{
		public static string FactoryTypeName { get; } = "stepProgress";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Step progress";
		public override string GetDisplayName() { return DisplayName; }

		public override IModifierSync Clone(int cloneFlags = 0)
		{
			return new StepProgressSyncedModifier();
		}

		public override bool Finished
		{
			get { return true; }
		}

		public override float TimeRemaining
		{
			get { return 0; }
		}

		public override bool Tick(float deltaTime)
		{
			// no-op
			return true;
		}

		public override void TickPaused(float deltaTime)
		{
			// no-op
		}

		public override bool TickDelayed(float deltaTime)
		{
			// no-op
			return false;
		}

		public override void PostTick()
		{
			// no-op
		}

		public override float GetProgress(IModifier m, float stepProgress, bool stepForwards)
		{
			if (m?.ParentStep == null)
				return 1.0f;

			if (m.ParentStep.InFirstHalfTotal)
				return m.ParentStep.TotalProgress;
			else
				return 1 - m.ParentStep.TotalProgress;
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			if (m?.ParentStep == null)
				return false;

			return m.ParentStep.InFirstHalfTotal;
		}

		public override void Reset()
		{
			// no-op
		}

		public override J.Node ToJSON()
		{
			return new J.Object();
		}

		public override bool FromJSON(J.Node n)
		{
			// no-op
			return true;
		}
	}


	class OtherModifierSyncedModifier : BasicModifierSync
	{
		public static string FactoryTypeName { get; } = "otherModifier";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Other modifier";
		public override string GetDisplayName() { return DisplayName; }

		private IModifier modifier_ = null;
		private int modifierIndex_ = -1;


		public OtherModifierSyncedModifier()
		{
		}

		public OtherModifierSyncedModifier(IModifier m)
		{
			OtherModifier = m;
		}

		public IModifier OtherModifier
		{
			get
			{
				return modifier_;
			}

			set
			{
				modifier_ = value;

				if (modifier_?.ParentStep == null)
					modifierIndex_ = -1;
				else
					modifierIndex_ = modifier_.ParentStep.IndexOfModifier(modifier_);
			}
		}

		public override IModifierSync Clone(int cloneFlags)
		{
			var m = new OtherModifierSyncedModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		protected void CopyTo(OtherModifierSyncedModifier m, int cloneFlags)
		{
			m.modifier_ = modifier_?.Clone(cloneFlags);
			m.modifierIndex_ = modifierIndex_;
		}

		public override bool Finished
		{
			get { return true; }
		}

		public override float TimeRemaining
		{
			get
			{
				if (modifier_ == null)
					return 0;
				else
					return modifier_.TimeRemaining;
			}
		}

		public override bool Tick(float deltaTime)
		{
			// no-op
			return true;
		}

		public override void TickPaused(float deltaTime)
		{
			// no-op
		}

		public override bool TickDelayed(float deltaTime)
		{
			// no-op
			return false;
		}

		public override void PostTick()
		{
			// no-op
		}

		public override float GetProgress(IModifier m, float stepProgress, bool stepForwards)
		{
			ResolveModifier(m);

			if (modifier_?.ModifierSync == null)
			{
				return 1;
			}
			else
			{
				return modifier_.ModifierSync.GetProgress(
					modifier_, stepProgress, stepForwards);
			}
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			ResolveModifier(m);

			if (modifier_?.ModifierSync == null)
			{
				return false;
			}
			else
			{
				return modifier_.ModifierSync.IsInFirstHalf(
					modifier_, stepProgress, stepForwards);
			}
		}

		public override void Reset()
		{
			// no-op
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("modifier", modifierIndex_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("OtherModifierSyncedModifier");
			if (o == null)
				return false;

			o.Opt("modifier", ref modifierIndex_);

			return true;
		}

		private void ResolveModifier(IModifier m)
		{
			if (m?.ParentStep == null)
				return;

			if (modifierIndex_ == -1 && modifier_ != null)
			{
				modifierIndex_ = m.ParentStep.IndexOfModifier(modifier_);
			}
			else if (modifierIndex_ >= 0 && modifier_ == null)
			{
				var mods = m.ParentStep.Modifiers;
				if (modifierIndex_ >= 0 && modifierIndex_ < mods.Count)
					modifier_ = mods[modifierIndex_].Modifier;
			}

			if (modifier_ == m)
			{
				Synergy.LogError("OtherModifierSyncedModifier: same modifiers");
				modifier_ = null;
				modifierIndex_ = -1;
			}
		}
	}
}
