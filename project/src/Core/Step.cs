using LeapInternal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	sealed class Step : IJsonable
	{
		public delegate void StepCallback(Step s);
		public event StepCallback StepNameChanged;

		public delegate void ModifierCallback(IModifier m);
		public event ModifierCallback ModifierNameChanged;

		public delegate void Callback();
		public event Callback ModifiersChanged;


		private readonly BoolParameter enabled_ =
			new BoolParameter("Enabled", true);

		private string name_ = null;
		private readonly ExplicitHolder<IDuration> duration_ =
			new ExplicitHolder<IDuration>();

		private ExplicitHolder<RandomizableTime> repeat_ =
			new ExplicitHolder<RandomizableTime>();

		private readonly ExplicitHolder<Delay> delay_ =
			new ExplicitHolder<Delay>();

		private readonly BoolParameter halfMove_ =
			new BoolParameter("HalfMove", false);

		private List<ModifierContainer> modifiers_ = null;

		private bool inFirstHalf_ = true;
		private List<ModifierContainer> enabledModifiers_ = null;

		private IModifier waitingFor_ = null;
		private float gracePeriod_ = -1;
		private bool useGracePeriod_ = true;


		public Step()
		{
			Clear();
		}

		public void RelinquishModifiers()
		{
			modifiers_ = new List<ModifierContainer>();
			enabledModifiers_ = new List<ModifierContainer>();
		}

		public void Clear()
		{
			if (modifiers_ != null)
			{
				while (modifiers_.Count > 0)
					DeleteModifier(modifiers_[0]);
			}

			enabled_.Value = true;
			name_ = null;
			Duration = new RandomDuration();
			Repeat = new RandomizableTime(0);
			Delay = new Delay(new RandomDuration(), false, false);
			halfMove_.Value = false;
			inFirstHalf_ = true;
			modifiers_ = new List<ModifierContainer>();
			enabledModifiers_ = new List<ModifierContainer>();
			waitingFor_ = null;
			gracePeriod_ = -1;
		}

		public Step Clone(int cloneFlags = 0)
		{
			var s = new Step();
			CopyTo(s, cloneFlags);
			return s;
		}

		private void CopyTo(Step s, int cloneFlags)
		{
			s.Clear();

			s.enabled_.Value = enabled_.Value;

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				s.Duration = Duration?.Clone(cloneFlags);
				s.Repeat = Repeat.Clone(cloneFlags);
				s.Delay = Delay.Clone(cloneFlags);
			}

			s.halfMove_.Value = halfMove_.Value;
			s.useGracePeriod_ = useGracePeriod_;

			foreach (var m in modifiers_)
				s.AddModifier(m.Clone(cloneFlags));
		}

		public void Added()
		{
			enabled_.BaseName = Name;
			halfMove_.BaseName = Name;
		}

		public void Removed()
		{
			Duration = null;
			Repeat = null;
			Delay = null;

			enabled_.Unregister();
			halfMove_.Unregister();

			foreach (var m in modifiers_)
				m.Removed();
		}


		public bool Enabled
		{
			get { return enabled_.Value; }
			set { enabled_.Value = value; }
		}

		public BoolParameter EnabledParameter
		{
			get { return enabled_; }
		}

		public string UserDefinedName
		{
			get
			{
				return name_;
			}

			set
			{
				name_ = value;
				StepNameChanged?.Invoke(this);
			}
		}

		public string Name
		{
			get
			{
				if (name_ == null)
				{
					var i = Synergy.Instance.Manager.IndexOfStep(this);
					return "Step " + (i + 1).ToString();
				}
				else
				{
					return name_;
				}
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public List<ModifierContainer> Modifiers
		{
			get { return modifiers_; }
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

		public RandomizableTime Repeat
		{
			get
			{
				return repeat_.HeldValue;
			}

			set
			{
				repeat_.HeldValue?.Removed();
				repeat_.Set(value);
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
				delay_.HeldValue?.Removed();
				delay_.Set(value);
			}
		}

		public bool HalfMove
		{
			get { return halfMove_.Value; }
			set { halfMove_.Value = value; }
		}

		public bool UseGracePeriod
		{
			get { return useGracePeriod_; }
			set { useGracePeriod_ = value; }
		}

		public BoolParameter HalfMoveParameter
		{
			get { return halfMove_; }
		}

		public float TotalProgress
		{
			get
			{
				if (duration_.HeldValue == null)
					return 1;

				return Duration.TotalProgress;
			}
		}

		public bool InFirstHalfTotal
		{
			get
			{
				if (duration_.HeldValue == null)
					return false;

				return Duration.InFirstHalfTotal;
			}
		}

		public IModifier WaitingFor
		{
			get { return waitingFor_; }
		}

		public float GracePeriod
		{
			get { return gracePeriod_; }
		}


		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("enabled", enabled_);
			o.Add("name", name_);
			o.Add("duration", Duration);
			o.Add("repeat", Repeat);
			o.Add("delay", Delay);
			o.Add("halfMove", halfMove_);
			o.Add("useGracePeriod", useGracePeriod_);
			o.Add("modifiers", modifiers_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			Clear();

			var o = n.AsObject("Step");
			if (o == null)
				return false;

			o.Opt("enabled", enabled_);
			o.Opt("name", ref name_);
			o.Opt("halfMove", halfMove_);
			o.Opt("useGracePeriod", ref useGracePeriod_);
			o.Opt("modifiers", ref modifiers_);

			{
				IDuration d = null;
				o.Opt<DurationFactory, IDuration>("duration", ref d);
				Duration = d;
			}

			{
				RandomizableTime t = null;
				o.Opt("repeat", ref t);
				Repeat = t;
			}

			{
				Delay d = null;
				o.Opt("delay", ref d);
				Delay = d;
			}

			foreach (var m in modifiers_)
				m.ParentStep = this;

			return true;
		}

		public void AddModifier(ModifierContainer m)
		{
			m.ParentStep = this;
			modifiers_.Add(m);
			m.Added();
			ModifiersChanged?.Invoke();
		}

		public ModifierContainer AddEmptyModifier()
		{
			var m = new ModifierContainer();
			AddModifier(m);
			return m;
		}

		public void DeleteModifier(ModifierContainer m)
		{
			m.Removed();

			if (m.Modifier != null)
			{
				foreach (var sm in modifiers_)
					sm.ModifierSync?.OtherModifierRemoved(m.Modifier);
			}

			modifiers_.Remove(m);
			ModifiersChanged?.Invoke();
		}

		public void FireModifierNameChanged(IModifier m)
		{
			ModifierNameChanged?.Invoke(m);
		}

		public int IndexOfModifier(IModifier m)
		{
			for (int i = 0; i < modifiers_.Count; ++i)
			{
				if (modifiers_[i].Modifier == m)
					return i;
			}

			return -1;
		}

		public int IndexOfModifier(ModifierContainer m)
		{
			return modifiers_.IndexOf(m);
		}

		public void EnableAll()
		{
			foreach (var m in modifiers_)
				m.Enabled = true;
		}

		public void DisableAllExcept(ModifierContainer except)
		{
			foreach (var m in modifiers_)
			{
				if (m == except)
					m.Enabled = true;
				else
					m.Enabled = false;
			}
		}

		public void Reset()
		{
			Duration.Reset();
			Repeat.Reset();

			if (!Synergy.Instance.Manager.IsOnlyEnabledStep(this))
			{
				foreach (var m in modifiers_)
				{
					if (m.Modifier != null)
						m.Modifier.Reset();
				}
			}
		}

		private void GatherEnabledModifiers()
		{
			enabledModifiers_.Clear();

			foreach (var m in modifiers_)
			{
				if (m.Modifier == null)
					continue;

				if (m.Enabled)
					enabledModifiers_.Add(m);
				else
					m.Modifier.Reset();

			}
		}

		public void Resume()
		{
			GatherEnabledModifiers();

			foreach (var m in enabledModifiers_)
				m?.Modifier?.Resume();
		}

		public bool Tick(float deltaTime, bool stepForwards)
		{
			try
			{
				if (Delay.Active)
					return DoDelay(deltaTime);

				if (HalfMove)
				{
					if (Repeat.Finished)
					{
						return DoHalfMove(deltaTime, stepForwards);
					}
					else
					{
						DoFullMove(deltaTime, stepForwards);
						return true;
					}
				}
				else
				{
					return DoFullMove(deltaTime, stepForwards);
				}
			}
			catch (Exception e)
			{
				Synergy.LogError(e.ToString());
				enabled_.Value = false;
				return false;
			}
		}

		private bool NeedsTickPaused()
		{
			// modifiers in TickPaused() will reset movements, which is
			// important when a modifier has a minimum movement that's not 0
			//
			// if TickPaused() isn't called, the modifier will be stuck at that
			// minimum even when the step isn't active
			//
			// however, if the step is a half move, TickPaused() must not be
			// called while the half move is active (that is, this step or a
			// later step is currently executing) because the modifier must not
			// be reset, since that would prevent the half move from working


			// if this is not a half move, always call TickPaused()
			if (!HalfMove)
				return true;

			// if this step is not active, always call TickPaused()
			if (!Synergy.Instance.Manager.IsStepActive(this))
				return true;

			// this step is a half move and it's either executing or a later
			// step is; don't call TickPaused() so modifiers don't get reset
			return false;
		}

		public void TickPaused(float deltaTime)
		{
			if (NeedsTickPaused())
				DoModifierTicksPaused(deltaTime);
		}

		private bool DoDelay(float deltaTime)
		{
			bool firstHalf = Duration.InFirstHalf;
			float progress;

			if (firstHalf)
				progress = Duration.FirstHalfProgress;
			else
				progress = Duration.SecondHalfProgress;

			Delay.Duration.Tick(deltaTime);
			DoModifierTicksDelayed(deltaTime, progress, firstHalf);

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
					Reset();
				}

				return false;
			}

			return true;
		}

		private bool DoHalfMove(float deltaTime, bool stepForwards)
		{
			Duration.Tick(deltaTime);

			bool firstHalf = Duration.InFirstHalf;
			float progress;

			if (firstHalf)
				progress = Duration.FirstHalfProgress;
			else
				progress = Duration.SecondHalfProgress;

			DoModifierTicks(deltaTime, progress, firstHalf);


			if (stepForwards)
			{
				if (Duration.FirstHalfFinished)
				{
					if (Delay.Halfway || Delay.EndForwards)
					{
						Delay.Active = true;
						Delay.StopAfter = true;
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				if (Duration.Finished && !HasUnfinishedModifiers())
				{
					waitingFor_ = null;
					gracePeriod_ = -1;

					if (Delay.EndBackwards)
					{
						Delay.Active = true;
						Delay.StopAfter = true;
						Delay.ResetDurationAfter = true;
					}
					else
					{
						Reset();
						return false;
					}
				}
			}

			return true;
		}

		private bool DoFullMove(float deltaTime, bool stepForwards)
		{
			if (!stepForwards)
			{
				if (Delay.EndBackwards)
				{
					Delay.Active = true;
					Delay.StopAfter = true;
					Delay.ResetDurationAfter = true;

					return true;
				}

				Duration.Tick(deltaTime);
				Repeat.Tick(deltaTime);

				return false;
			}

			Duration.Tick(deltaTime);
			Repeat.Tick(deltaTime);


			bool firstHalf = Duration.InFirstHalf;
			float progress;

			if (firstHalf)
				progress = Duration.FirstHalfProgress;
			else
				progress = Duration.SecondHalfProgress;

			DoModifierTicks(deltaTime, progress, firstHalf);


			if (Duration.Finished)
			{
				if (Repeat.Finished && !HalfMove)
				{
					if (Synergy.Instance.Manager.IsOnlyEnabledStep(this) ||
					    !HasUnfinishedModifiers())
					{
						waitingFor_ = null;
						gracePeriod_ = -1;

						if (Delay.EndForwards)
						{
							Delay.Active = true;
							Delay.StopAfter = true;
							Delay.ResetDurationAfter = true;
						}
						else
						{
							Reset();
							return false;
						}
					}
				}
				else
				{
					Duration.Reset();
				}
			}
			else
			{
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

			return true;
		}

		private void DoModifierTicks(
			float deltaTime, float progress, bool forwards)
		{
			foreach (var m in modifiers_)
				m.Modifier?.Tick(deltaTime, progress, forwards);
		}

		private void DoModifierTicksPaused(float deltaTime)
		{
			foreach (var m in modifiers_)
				m.Modifier?.TickPaused(deltaTime);
		}

		private void DoModifierTicksDelayed(
			float deltaTime, float progress, bool forwards)
		{
			foreach (var m in modifiers_)
				m.Modifier?.TickDelayed(deltaTime, progress, forwards);
		}

		private bool HasUnfinishedModifiers()
		{
			if (useGracePeriod_)
			{
				float longestRemaining = -1;

				foreach (var m in enabledModifiers_)
				{
					if (m.Modifier == null)
						continue;

					if (!m.Modifier.Finished)
					{
						if (m.Modifier.TimeRemaining > longestRemaining)
						{
							waitingFor_ = m.Modifier;
							longestRemaining = m.Modifier.TimeRemaining;
						}
					}
				}

				if (longestRemaining <= 0)
				{
					waitingFor_ = null;
					return false;
				}

				gracePeriod_ = longestRemaining;

				foreach (var sm in enabledModifiers_)
					sm.Modifier?.Stop(longestRemaining);

				return true;
			}
			else
			{
				return false;
			}
		}

		public void Set(bool paused)
		{
			foreach (var m in enabledModifiers_)
				m.Modifier?.Set(paused);
		}
	}
}
