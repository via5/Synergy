using System.Collections.Generic;

namespace Synergy
{
	class DelayWidgets
	{
		private Delay currentDelay_ = null;
		private bool supportsHalfMove_ = true;

		private readonly Checkbox halfway_;
		private readonly Checkbox endForwards_;
		private readonly Checkbox endBackwards_;
		private readonly DurationWidgets durationWidgets_;

		public DelayWidgets(int flags = 0)
		{
			halfway_ = new Checkbox(
				"Halfway", false, DelayHalfwayChanged, flags);

			endForwards_ = new Checkbox(
				"Forwards end", false, DelayForwardsEndChanged, flags);

			endBackwards_ = new Checkbox(
				"Backwards end", false, DelayBackwardsEndChanged, flags);

			durationWidgets_ = new DurationWidgets(
				"Delay", DelayTypeChanged, flags);

			HalfMove = false;
		}

		public void SetValue(Delay d)
		{
			currentDelay_ = d;

			if (d == null)
			{
				halfway_.Value = false;
				endForwards_.Value = false;
				endBackwards_.Value = false;
				durationWidgets_.SetValue(null);
			}
			else
			{
				halfway_.Value = d.Halfway;
				endForwards_.Value = d.EndForwards;
				endBackwards_.Value = d.EndBackwards;
				durationWidgets_.SetValue(d.Duration);
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

			list.AddRange(durationWidgets_.GetWidgets());

			return list;
		}

		private void DelayTypeChanged(IDuration d)
		{
			if (currentDelay_ != null)
			{
				currentDelay_.Duration = d;
				Synergy.Instance.UI.NeedsReset("delay type changed");
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
	}

}
