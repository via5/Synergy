using System.Collections.Generic;

namespace Synergy
{
	interface IModifierSync : IFactoryObject
	{
		ModifierContainer ParentModifierContainer { get; set; }

		IModifierSync Clone(int cloneFlags = 0);
		void Removed();
		void OtherModifierRemoved(ModifierContainer mc);

		void Resume();
		bool Tick(float deltaTime);
		void TickPaused(float deltaTime);
		bool TickDelayed(float deltaTime);
		void PostTick();
		float GetProgress(IModifier m, float stepProgress, bool stepForwards);
		bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards);
		bool Finished { get; }
		float TimeRemaining { get; }
		float CurrentDuration { get; }
		float DurationProgress { get; }
		void Reset();
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

		private ModifierContainer parent_ = null;

		public ModifierContainer ParentModifierContainer
		{
			get { return parent_; }
			set { parent_ = value; }
		}

		public Step ParentStep
		{
			get { return parent_?.ParentStep; }
		}

		public virtual void Resume()
		{
			// no-op
		}

		public abstract IModifierSync Clone(int cloneFlags = 0);

		public virtual void Removed()
		{
			ParentModifierContainer = null;
		}

		public virtual void OtherModifierRemoved(ModifierContainer mc)
		{
			// no-op
		}

		public abstract bool Finished { get; }
		public abstract float TimeRemaining { get; }
		public abstract float CurrentDuration { get; }
		public abstract float DurationProgress { get; }

		public abstract bool Tick(float deltaTime);
		public abstract void TickPaused(float deltaTime);
		public abstract bool TickDelayed(float deltaTime);
		public abstract void PostTick();
		public abstract float GetProgress(IModifier m, float stepProgress, bool stepForwards);
		public abstract bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards);
		public abstract void Reset();

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
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
			get { return ParentStep?.Duration?.TimeRemaining ?? 0; }
		}

		public override float CurrentDuration
		{
			get { return ParentStep?.Duration?.Current ?? 0; }
		}

		public override float DurationProgress
		{
			get
			{
				var current = CurrentDuration;
				var remaining = TimeRemaining;

				if (current == 0)
					return 0;

				return (current - remaining) / current;
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

				if (!Finished && inFirstHalf_ && Delay.Halfway)
					t += Delay.HalfwayDuration.TimeRemaining;
				else if (Delay.ActiveType != Delay.None)
					t += Delay.ActiveDuration.TimeRemaining;

				return t;
			}
		}

		public override float CurrentDuration
		{
			get
			{
				return Duration?.Current ?? 0;
			}
		}

		public override float DurationProgress
		{
			get
			{
				var current = Duration?.Current ?? 0;
				var remaining = Duration?.TimeRemaining ?? 0;

				if (remaining == 0)
					return 1;

				return (current - remaining) / current;
			}
		}

		public override IModifierSync Clone(int cloneFlags)
		{
			var m = new UnsyncedModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		public override void Resume()
		{
			base.Resume();
			Duration?.Resume();
			Delay?.Resume();
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
			if (Delay.ActiveType != Delay.None)
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
			if (ParentStep == null)
				return;

			if (Duration.Finished)
			{
				if (Delay.EndForwards)
				{
					if (ParentStep.MustStopEventually)
					{
						if (Delay.EndForwardsDuration.Current > ParentStep.TimeRemainingInDirection)
							return;
					}

					Delay.ActiveType = Delay.EndForwardsType;
					Delay.StopAfter = true;
					Delay.ResetDurationAfter = true;
				}
				else
				{
					if (ParentStep.MustStopEventually)
					{
						Duration.Reset(ParentStep.TimeRemainingInDirection);
						ConfirmDurationForStop();
					}
					else
					{
						Reset();
					}

					return;
				}
			}
			else
			{
				var firstHalf = Duration.InFirstHalf;

				if ((inFirstHalf_ && !firstHalf) && Delay.Halfway)
				{
					inFirstHalf_ = firstHalf;

					if (ParentStep.MustStopEventually)
					{
						if ((Delay.HalfwayDuration.Current + Duration.TimeRemaining) > ParentStep.TimeRemainingInDirection)
							return;
					}

					Delay.ActiveType = Delay.HalfwayType;
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
			Delay.ActiveDuration.Tick(deltaTime);

			if (!Delay.ActiveDuration.Finished)
				return true;

			Delay.ActiveDuration.Reset();
			Delay.ActiveType = Delay.None;

			if (Delay.StopAfter)
			{
				Delay.StopAfter = false;

				if (Delay.ResetDurationAfter)
				{
					Delay.ResetDurationAfter = false;

					if (ParentStep.MustStopEventually)
					{
						Duration.Reset(ParentStep.TimeRemainingInDirection);
						ConfirmDurationForStop();
					}
					else
					{
						Reset();
					}
				}

				return false;
			}

			return true;
		}

		private void ConfirmDurationForStop()
		{
			if (!Delay.Halfway)
				return;

			var graceForDelay = ParentStep.TimeRemainingInDirection - Duration.Current;

			if (graceForDelay < Delay.HalfwayDuration.Current)
			{
				// duration + delay would be longer than time remaining,
				// cancel
				//Duration.Reset(-1);
			}
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
			get { return ParentStep?.TimeRemainingInDirection ?? 0; }
		}

		public override float CurrentDuration
		{
			get { return ParentStep?.Duration?.Current ?? 0; }
		}

		public override float DurationProgress
		{
			get
			{
				var current = CurrentDuration;
				var remaining = TimeRemaining;

				if (current == 0)
					return 0;

				return (current - remaining) / current;
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
			var d = m?.ParentStep?.Duration;
			if (d == null)
				return 1.0f;

			if (d is RandomDuration)
			{
				var rd = d as RandomDuration;

				if (rd.Time.Current == 0)
					return 0;

				return rd.Time.Elapsed /rd.Time.Current;
			}
			else if (d is RampDuration)
			{
				if (m.ParentStep.InFirstHalfTotal)
					return m.ParentStep.TotalProgress;
				else
					return 1 - m.ParentStep.TotalProgress;
			}
			else
			{
				Synergy.LogError("GetProgress: unknown duration type");
				return 1.0f;
			}
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			var d = m?.ParentStep?.Duration;
			if (d == null)
				return false;

			if (d is RandomDuration)
			{
				return true;
			}
			else if (d is RampDuration)
			{
				return m.ParentStep.InFirstHalfTotal;
			}
			else
			{
				Synergy.LogError("IsInFirstHalf: unknown duration type");
				return true;
			}
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

		private ModifierContainer mc_ = null;
		private int index_ = -1;


		public OtherModifierSyncedModifier()
		{
		}

		public OtherModifierSyncedModifier(ModifierContainer mc)
		{
			OtherModifierContainer = mc;
		}

		public ModifierContainer OtherModifierContainer
		{
			get
			{
				ResolveModifier();
				return mc_;
			}

			set
			{
				mc_ = value;

				if (mc_?.ParentStep == null)
					index_ = -1;
				else
					index_ = mc_.ParentStep.IndexOfModifier(mc_);
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
			m.mc_ = null;
			m.index_ = index_;
		}

		public override bool Finished
		{
			get { return mc_?.Modifier?.Finished ?? true; }
		}

		public override float TimeRemaining
		{
			get { return mc_?.Modifier?.TimeRemaining ?? 0; }
		}

		public override float CurrentDuration
		{
			get { return mc_?.Modifier?.CurrentDuration ?? 0; }
		}

		public override float DurationProgress
		{
			get
			{
				var current = CurrentDuration;
				var remaining = TimeRemaining;

				if (current == 0)
					return 0;

				return (current - remaining) / current;
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
			ResolveModifier();

			if (mc_?.ModifierSync == null)
				return 1;

			return mc_.ModifierSync.GetProgress(
				mc_.Modifier, stepProgress, stepForwards);
		}

		public override bool IsInFirstHalf(IModifier m, float stepProgress, bool stepForwards)
		{
			ResolveModifier();

			if (mc_?.ModifierSync == null)
				return false;

			return mc_.ModifierSync.IsInFirstHalf(
				mc_.Modifier, stepProgress, stepForwards);
		}

		public override void OtherModifierRemoved(ModifierContainer mc)
		{
			if (OtherModifierContainer == mc)
				OtherModifierContainer = null;
			else
				index_ = -1;
		}

		public override void Reset()
		{
			// no-op
		}

		public override J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("modifier", index_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			var o = n.AsObject("OtherModifierSyncedModifier");
			if (o == null)
				return false;

			o.Opt("modifier", ref index_);

			return true;
		}

		private void ResolveModifier()
		{
			if (ParentStep == null)
				return;

			if (index_ == -1 && mc_ != null)
			{
				index_ = ParentStep.IndexOfModifier(mc_);
			}
			else if (index_ >= 0 && mc_ == null)
			{
				var mods = ParentStep.Modifiers;
				if (index_ >= 0 && index_ < mods.Count)
					mc_ = mods[index_].Modifier.ParentContainer;
			}

			if (mc_ != null && mc_ == ParentModifierContainer)
			{
				Synergy.LogError("OtherModifierSyncedModifier: same modifiers");
				mc_ = null;
				index_ = -1;
			}
		}
	}
}
