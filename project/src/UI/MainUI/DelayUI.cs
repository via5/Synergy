using System.Collections.Generic;

namespace Synergy
{
	class DelayWidgets
	{
		private readonly int flags_;
		private Delay currentDelay_ = null;
		private bool supportsHalfMove_ = true;

		private readonly Checkbox halfway_;
		private readonly Checkbox endForwards_;
		private readonly Checkbox endBackwards_;
		private readonly Checkbox sameDelay_;

		private readonly DurationWidgets durationWidgets_;
		private readonly DurationWidgets halfwayDurationWidgets_;
		private readonly DurationWidgets endForwardsDurationWidgets_;
		private readonly DurationWidgets endBackwardsDurationWidgets_;

		public DelayWidgets(int flags = 0)
		{
			flags_ = flags;

			halfway_ = new Checkbox(
				"Halfway", false, DelayHalfwayChanged, flags);

			endForwards_ = new Checkbox(
				"Forwards end", false, DelayForwardsEndChanged, flags);

			endBackwards_ = new Checkbox(
				"Backwards end", false, DelayBackwardsEndChanged, flags);

			sameDelay_ = new Checkbox(
				"Same delay for all", false, SameDelayChanged, flags);

			durationWidgets_ = new DurationWidgets(
				"Delay", SingleDelayTypeChanged, flags);

			halfwayDurationWidgets_ = new DurationWidgets(
				"Halfway delay", HalfwayDelayTypeChanged, flags);

			endForwardsDurationWidgets_ = new DurationWidgets(
				"End forwards delay", EndForwardsDelayTypeChanged, flags);

			endBackwardsDurationWidgets_ = new DurationWidgets(
				"End backwards delay", EndBackwardsDelayTypeChanged, flags);

			HalfMove = false;
		}

		public void SetValue(Delay d)
		{
			currentDelay_ = d;

			if (d == null)
			{
				halfway_.Value = false;
				halfway_.Parameter = null;
				endForwards_.Value = false;
				endForwards_.Parameter = null;
				endBackwards_.Value = false;
				endBackwards_.Parameter = null;
				sameDelay_.Value = false;
				durationWidgets_.SetValue(null);
				halfwayDurationWidgets_.SetValue(null);
				endForwardsDurationWidgets_.SetValue(null);
				endBackwardsDurationWidgets_.SetValue(null);
			}
			else
			{
				halfway_.Parameter = d.HalfwayParameter;
				endForwards_.Parameter = d.EndForwardsParameter;
				endBackwards_.Parameter = d.EndBackwardsParameter;
				sameDelay_.Value = d.SameDelay;
				durationWidgets_.SetValue(d.SingleDuration);
				halfwayDurationWidgets_.SetValue(d.HalfwayDuration);
				endForwardsDurationWidgets_.SetValue(d.EndForwardsDuration);
				endBackwardsDurationWidgets_.SetValue(d.EndBackwardsDuration);
			}
		}

		public bool SupportsHalfMove
		{
			set
			{
				supportsHalfMove_ = value;
			}
		}

		public bool HalfMove
		{
			set
			{
				if (value)
				{
					halfway_.Enabled = false;
					endForwards_.Enabled = true;
					endBackwards_.Enabled = true;
				}
				else
				{
					halfway_.Enabled = true;
					endForwards_.Enabled = true;
					endBackwards_.Enabled = false;
				}
			}
		}

		public List<IWidget> GetWidgets()
		{
			var list = new List<IWidget>();

			list.Add(halfway_);
			list.Add(endForwards_);

			if (supportsHalfMove_)
				list.Add(endBackwards_);

			list.Add(sameDelay_);

			if (currentDelay_?.SameDelay ?? true)
			{
				list.AddRange(durationWidgets_.GetWidgets());
			}
			else
			{
				list.AddRange(halfwayDurationWidgets_.GetWidgets());
				list.Add(new SmallSpacer(flags_));
				list.AddRange(endForwardsDurationWidgets_.GetWidgets());
				list.Add(new SmallSpacer(flags_));
				list.AddRange(endBackwardsDurationWidgets_.GetWidgets());
			}

			return list;
		}

		private void SingleDelayTypeChanged(IDuration d)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.SingleDuration = d;
				Synergy.Instance.MainUI.NeedsReset("single delay type changed");
			}
		}

		private void HalfwayDelayTypeChanged(IDuration d)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.HalfwayDuration = d;
				Synergy.Instance.MainUI.NeedsReset("halfway delay type changed");
			}
		}

		private void EndForwardsDelayTypeChanged(IDuration d)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.EndForwardsDuration = d;
				Synergy.Instance.MainUI.NeedsReset("end forwards delay type changed");
			}
		}

		private void EndBackwardsDelayTypeChanged(IDuration d)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.EndBackwardsDuration = d;
				Synergy.Instance.MainUI.NeedsReset("end backwards delay type changed");
			}
		}

		private void DelayHalfwayChanged(bool b)
		{
			if (currentDelay_ != null)
				currentDelay_.Halfway = b;
		}

		private void DelayForwardsEndChanged(bool b)
		{
			if (currentDelay_ != null)
				currentDelay_.EndForwards = b;
		}

		private void DelayBackwardsEndChanged(bool b)
		{
			if (currentDelay_ != null)
				currentDelay_.EndBackwards = b;
		}

		private void SameDelayChanged(bool b)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.SameDelay = b;
				Synergy.Instance.MainUI.NeedsReset("same delay changed");
			}
		}
	}
}
