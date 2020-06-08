using System.Collections.Generic;

namespace Synergy
{
	interface IModifierSync : IFactoryObject
	{
		IModifier ParentModifier { get; set; }

		IModifierSync Clone(int cloneFlags = 0);
		void Removed();

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

	sealed class ModifierSyncFactory : BasicFactory<IModifierSync>
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

		public virtual void Removed()
		{
			ParentModifier = null;
		}

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


	sealed class DurationSyncedModifier : BasicModifierSync
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


	sealed class UnsyncedModifier : BasicModifierSync
	{
		public static string FactoryTypeName { get; } = "unsynced";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Unsynced";
		public override string GetDisplayName() { return DisplayName; }

		private readonly ExplicitHolder<IDuration> duration_ =
			new ExplicitHolder<IDuration>();

		private readonly ExplicitHolder<Delay> delay_ =
			new ExplicitHolder<Delay>();

		private bool inFirstHalf_ = true;


		public UnsyncedModifier()
			: this(new RandomDuration(), new Delay())
		{
		}

		public UnsyncedModifier(IDuration d, Delay delay = null)
		{
			if (d == null)
				Duration = new RandomDuration(1);
			else
				Duration = d;

			if (delay == null)
				Delay = new Delay();
			else
				Delay = delay;
		}

		public override bool Finished
		{
			get
			{
				return Duration.Finished;
			}
		}

		public override float TimeRemaining
		{
			get
			{
				if (duration_ == null)
					return 0;

				var t = Duration.TimeRemaining;

				if ((inFirstHalf_ && Delay.Halfway) || Delay.Active)
					t += Delay.Duration.TimeRemaining;

				return t;
			}
		}

		public override IModifierSync Clone(int cloneFlags)
		{
			var m = new UnsyncedModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		public override void Removed()
		{
			base.Removed();
			Duration = null;
			Delay = null;
		}

		private void CopyTo(UnsyncedModifier m, int cloneFlags)
		{
			m.Duration = Duration?.Clone(cloneFlags);
			m.Delay = Delay.Clone(cloneFlags);
		}

		public IDuration Duration
		{
			get
			{
				return duration_.HeldValue;
			}

			set
			{
				duration_.HeldValue?.Removed();
				duration_.Set(value);
			}
		}

		public Delay Delay
		{
			get
			{
				return delay_.HeldValue;
			}

			set
			{
				if (delay_.HeldValue != null)
					delay_.HeldValue.Removed();

				delay_.Set(value);
			}
		}

		public override bool Tick(float deltaTime)
		{
			if (Delay.Active)
				return DoDelay(deltaTime);

			Duration.Tick(deltaTime);

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
			if (Duration.Finished)
			{
				if (Delay.EndForwards)
				{
					if (MustStopWhenFinished)
					{
						if (Delay.Duration.Current >= StopGracePeriod)
							return;
					}

					Delay.Active = true;
					Delay.StopAfter = true;
					Delay.ResetDurationAfter = true;
				}
				else
				{
					if (MustStopWhenFinished)
						Duration.Reset(StopGracePeriod);
					else
						Reset();

					return;
				}
			}
			else
			{
				var firstHalf = Duration.InFirstHalf;

				if ((inFirstHalf_ && !firstHalf) && Delay.Halfway)
				{
					inFirstHalf_ = firstHalf;
					Delay.Active = true;
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
				return Duration.FirstHalfProgress;
			else
				return Duration.SecondHalfProgress;
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			return Duration.InFirstHalf;
		}

		public override void Reset()
		{
			Duration.Reset();
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
			var o = n.AsObject("UnsyncedModifier");
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

		private bool DoDelay(float deltaTime)
		{
			Delay.Duration.Tick(deltaTime);

			if (!Delay.Duration.Finished)
				return true;

			Delay.Duration.Reset();
			Delay.Active = false;

			if (Delay.StopAfter)
			{
				Delay.StopAfter = false;

				if (Delay.ResetDurationAfter)
				{
					Delay.ResetDurationAfter = false;

					if (MustStopWhenFinished)
						Duration.Reset(StopGracePeriod);
					else
						Reset();
				}

				return false;
			}

			return true;
		}
	}


	sealed class StepProgressSyncedModifier : BasicModifierSync
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


	sealed class OtherModifierSyncedModifier : BasicModifierSync
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

		private void CopyTo(OtherModifierSyncedModifier m, int cloneFlags)
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
