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

		private bool paused_ = false;

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

		private const float EnabledCheckInterval = 1;
		private float nextEnabledCheck_ = 0;


		public Step()
		{
			Clear();
		}

		public void DeferredInit()
		{
			foreach (var m in modifiers_)
				m.DeferredInit();
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
		}

		public void PluginEnabled(bool b)
		{
			foreach (var m in enabledModifiers_)
				m.PluginEnabled(b);
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
			get
			{
				return enabled_.Value;
			}

			set
			{
				if (value != enabled_.Value)
				{
					enabled_.Value = value;
					if (!enabled_.Value)
						Reset();
				}
			}
		}

		public BoolParameter EnabledParameter
		{
			get { return enabled_; }
		}

		public bool Paused
		{
			get { return paused_; }
			set { paused_ = value; }
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

		public List<ModifierContainer> Modifiers
		{
			get { return modifiers_; }
		}

		public List<ModifierContainer> EnabledModifiers
		{
			get { return enabledModifiers_; }
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

		public float TimeRemainingInDirection
		{
			get
			{
				float longestHardDuration = 0;
				foreach (var m in modifiers_)
				{
					if (m.Modifier == null)
						continue;

					if (m.Modifier.HardDuration)
					{
						longestHardDuration = Math.Max(
							longestHardDuration, m.Modifier.TimeRemaining);
					}
				}

				float stepRemaining;

				if (HalfMove)
				{
					stepRemaining = Duration.TimeRemainingInHalf;
					longestHardDuration /= 2;
				}
				else
				{
					stepRemaining = Duration.TimeRemaining;
				}

				return Math.Max(stepRemaining, longestHardDuration);
			}
		}

		public bool MustStopEventually
		{
			get
			{
				return !Synergy.Instance.Manager.IsOnlyEnabledStep(this);
			}
		}


		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("enabled", enabled_);
			o.Add("paused", paused_);
			o.Add("name", name_);
			o.Add("duration", Duration);
			o.Add("repeat", Repeat);
			o.Add("delay", Delay);
			o.Add("halfMove", halfMove_);
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
			o.Opt("paused", ref paused_);
			o.Opt("name", ref name_);
			o.Opt("halfMove", halfMove_);
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

		public void DeleteModifier(ModifierContainer mc)
		{
			// remember the modifier, the container nulls it in Removed()
			var m = mc.Modifier;

			mc.Removed();

			if (m != null)
			{
				foreach (var sm in modifiers_)
					sm.ModifierSync?.OtherModifierRemoved(m);
			}

			modifiers_.Remove(mc);
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
				m.Enabled = (m == except);
		}

		public void Reset()
		{
			Duration.Reset();
			Repeat.Reset();
			Delay.Reset();

			if (!Synergy.Instance.Manager.IsOnlyEnabledStep(this))
			{
				foreach (var m in modifiers_)
				{
					if (m.Modifier != null)
						m.Modifier.Reset();
				}
			}
		}

		public void ForceGatherEnabledModifiers()
		{
			GatherEnabledModifiers(false);
		}

		private void GatherEnabledModifiers(bool resetDisabled=true)
		{
			enabledModifiers_.Clear();

			foreach (var m in modifiers_)
			{
				if (m.Modifier == null)
					continue;

				if (m.Enabled)
					enabledModifiers_.Add(m);
				else if (resetDisabled)
					m.Modifier.Reset();
			}

			nextEnabledCheck_ = 0;
		}

		public void Resume()
		{
			GatherEnabledModifiers();

			Duration?.Resume();
			Repeat?.Resume();
			Delay?.Resume();

			foreach (var m in enabledModifiers_)
				m?.Modifier?.Resume();
		}

		public bool Tick(float deltaTime, bool stepForwards)
		{
			if (paused_)
				return false;

			nextEnabledCheck_ += deltaTime;
			if (nextEnabledCheck_ >= EnabledCheckInterval)
				GatherEnabledModifiers();

			try
			{
				if (Delay.ActiveType != Delay.None)
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

			if (paused_)
				return false;

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

			Delay.ActiveDuration.Tick(deltaTime);
			DoModifierTicksDelayed(deltaTime, progress, firstHalf);

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
					if (Delay.Halfway)
					{
						Delay.ActiveType = Delay.HalfwayType;
						Delay.StopAfter = true;
					}
					else if (Delay.EndForwards)
					{
						Delay.ActiveType = Delay.EndForwardsType;
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
				if (Duration.Finished)
				{
					if (Delay.EndBackwards)
					{
						Delay.ActiveType = Delay.EndBackwardsType;
						Delay.StopAfter = true;
						Delay.ResetDurationAfter = true;
					}
					else
					{
						return ResetIfHardDurationsFinished();
					}
				}
			}

			return true;
		}

		private bool ResetIfHardDurationsFinished()
		{
			foreach (var m in modifiers_)
			{
				if (m.Modifier == null)
					continue;

				if (m.Modifier.HardDuration)
				{
					if (!m.Modifier.Finished)
						return true;
				}
			}

			Reset();
			return false;
		}

		private bool DoFullMove(float deltaTime, bool stepForwards)
		{
			if (!stepForwards)
			{
				if (Delay.EndBackwards)
				{
					Delay.ActiveType = Delay.EndBackwardsType;
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
					if (Delay.EndForwards)
					{
						Delay.ActiveType = Delay.EndForwardsType;
						Delay.StopAfter = true;
						Delay.ResetDurationAfter = true;
					}
					else
					{
						return ResetIfHardDurationsFinished();
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
					Delay.ActiveType = Delay.HalfwayType;
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

		public void Set(bool paused)
		{
			foreach (var m in enabledModifiers_)
				m.Modifier?.Set(paused);
		}
	}
}
