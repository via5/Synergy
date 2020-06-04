using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class Step : IJsonable
	{
		private bool enabled_ = true;
		private string name_ = null;
		private IDuration duration_ = null;
		private RandomizableTime repeat_ = null;
		private Delay delay_ = null;
		private bool halfMove_ = false;
		private List<ModifierContainer> modifiers_ = null;

		private bool inFirstHalf_ = true;
		private List<ModifierContainer> enabledModifiers_ = null;

		private IModifier waitingFor_ = null;
		private float gracePeriod_ = -1;


		public Step()
		{
			Clear();
		}

		public void Clear()
		{
			if (modifiers_ != null)
			{
				while (modifiers_.Count > 0)
					DeleteModifier(modifiers_[0]);
			}

			enabled_ = true;
			name_ = null;
			duration_ = new RandomDuration();
			repeat_ = new RandomizableTime(0);
			delay_ = new Delay();
			halfMove_ = false;
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

			s.enabled_ = enabled_;

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				s.duration_ = duration_?.Clone(cloneFlags);
				s.repeat_ = repeat_.Clone(cloneFlags);
				s.delay_ = delay_.Clone(cloneFlags);
			}

			s.halfMove_ = halfMove_;

			foreach (var m in modifiers_)
				s.AddModifier(m.Clone(cloneFlags));
		}


		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public string UserDefinedName
		{
			get { return name_; }
			set { name_ = value; }
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

		public IDuration Duration
		{
			get { return duration_; }
			set { duration_ = value; }
		}

		public RandomizableTime Repeat
		{
			get { return repeat_; }
			set { repeat_ = value; }
		}

		public Delay Delay
		{
			get { return delay_; }
			set { delay_ = value; }
		}

		public bool HalfMove
		{
			get { return halfMove_; }
			set { halfMove_ = value; }
		}

		public float TotalProgress
		{
			get
			{
				if (duration_ == null)
					return 1;

				return duration_.TotalProgress;
			}
		}

		public bool InFirstHalfTotal
		{
			get
			{
				if (duration_ == null)
					return false;

				return duration_.InFirstHalfTotal;
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


		public void AboutToBeRemoved()
		{
			foreach (var m in modifiers_)
				m.AboutToBeRemoved();
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("enabled", Enabled);
			o.Add("name", name_);
			o.Add("duration", duration_);
			o.Add("repeat", repeat_);
			o.Add("delay", delay_);
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

			o.Opt("enabled", ref enabled_);
			o.Opt("name", ref name_);
			o.Opt<DurationFactory, IDuration>("duration", ref duration_);
			o.Opt("repeat", ref repeat_);
			o.Opt("delay", ref delay_);
			o.Opt("halfMove", ref halfMove_);
			o.Opt("modifiers", ref modifiers_);

			foreach (var m in modifiers_)
			{
				m.Step = this;

				if (m.Modifier != null)
					m.Modifier.ParentStep = this;
			}

			return true;
		}

		public void AddModifier(ModifierContainer m)
		{
			m.Step = this;
			modifiers_.Add(m);
		}

		public void AddEmptyModifier()
		{
			AddModifier(new ModifierContainer());
		}

		public void DeleteModifier(ModifierContainer m)
		{
			m.AboutToBeRemoved();

			// todo: needs a generic notification
			foreach (var sm in modifiers_)
			{
				if (sm.Modifier != null)
				{
					var om = sm.Modifier.ModifierSync as OtherModifierSyncedModifier;
					if (om != null)
						om.OtherModifier = null;
				}
			}

			modifiers_.Remove(m);
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
			duration_.Reset();
			repeat_.Reset();

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
				if (m.Modifier != null && m.Enabled)
					enabledModifiers_.Add(m);
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
				if (delay_.Active)
					return DoDelay(deltaTime);

				if (halfMove_)
				{
					if (repeat_.Finished)
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
				Enabled = false;
				return false;
			}
		}

		public void TickPaused(float deltaTime)
		{
			DoModifierTicksPaused(deltaTime);
		}

		private bool DoDelay(float deltaTime)
		{
			bool firstHalf = duration_.InFirstHalf;
			float progress;

			if (firstHalf)
				progress = duration_.FirstHalfProgress;
			else
				progress = duration_.SecondHalfProgress;

			delay_.Duration.Tick(deltaTime);
			DoModifierTicksDelayed(deltaTime, progress, firstHalf);

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
					Reset();
				}

				return false;
			}

			return true;
		}

		private bool DoHalfMove(float deltaTime, bool stepForwards)
		{
			duration_.Tick(deltaTime);

			if (stepForwards)
			{
				float progress = duration_.FirstHalfProgress;
				DoModifierTicks(deltaTime, progress, true);

				if (progress == 1.0f)
				{
					if (delay_.Halfway || delay_.EndForwards)
					{
						delay_.Active = true;
						delay_.StopAfter = true;
					}
					else
					{
						return false;
					}
				}
			}
			else
			{
				float progress = duration_.SecondHalfProgress;
				DoModifierTicks(deltaTime, progress, false);

				if (duration_.Finished && !HasUnfinishedModifiers())
				{
					waitingFor_ = null;
					gracePeriod_ = -1;

					if (delay_.EndBackwards)
					{
						delay_.Active = true;
						delay_.StopAfter = true;
						delay_.ResetDurationAfter = true;
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
				if (delay_.EndBackwards)
				{
					delay_.Active = true;
					delay_.StopAfter = true;
					delay_.ResetDurationAfter = true;

					return true;
				}

				duration_.Tick(deltaTime);
				repeat_.Tick(deltaTime);

				return false;
			}

			duration_.Tick(deltaTime);
			repeat_.Tick(deltaTime);


			bool firstHalf = duration_.InFirstHalf;
			float progress;

			if (firstHalf)
				progress = duration_.FirstHalfProgress;
			else
				progress = duration_.SecondHalfProgress;

			DoModifierTicks(deltaTime, progress, firstHalf);


			if (duration_.Finished)
			{
				if (repeat_.Finished && !halfMove_)
				{
					if (Synergy.Instance.Manager.IsOnlyEnabledStep(this) ||
					    !HasUnfinishedModifiers())
					{
						waitingFor_ = null;
						gracePeriod_ = -1;

						if (delay_.EndForwards)
						{
							delay_.Active = true;
							delay_.StopAfter = true;
							delay_.ResetDurationAfter = true;
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
					duration_.Reset();
				}
			}
			else
			{
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

		public void Set(bool paused)
		{
			foreach (var m in enabledModifiers_)
				m.Modifier?.Set(paused);
		}
	}
}
