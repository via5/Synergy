using System.Collections.Generic;

namespace Synergy
{
	class DurationMonitorWidgets
	{
		private readonly string name_;
		private readonly int flags_;

		private IDuration duration_ = null;
		private IDurationMonitor ui_ = null;

		public DurationMonitorWidgets(string name, int flags)
		{
			name_ = name;
			flags_ = flags;
		}

		public List<IWidget> GetWidgets(IDuration d)
		{
			duration_ = d;

			if (duration_ != null)
			{
				if (ui_ == null || ui_.DurationType != d.GetFactoryTypeName())
				{
					ui_ = MonitorUI.CreateDurationMonitor(
						name_, duration_, flags_);
				}
			}

			if (ui_ == null)
				return new List<IWidget>();

			return ui_.GetWidgets(d);
		}

		public void Update()
		{
			if (ui_ != null)
				ui_.Update();
		}
	}


	interface IDurationMonitor
	{
		string DurationType { get; }
		void AddToUI(IDuration m);
		List<IWidget> GetWidgets(IDuration d);
		void RemoveFromUI();
		void Update();
	}


	abstract class BasicDurationMonitor : IDurationMonitor
	{
		protected readonly int flags_;
		protected readonly WidgetList widgets_ = new WidgetList();

		protected BasicDurationMonitor(int flags)
		{
			flags_ = flags;
		}

		public abstract string DurationType { get; }
		public abstract void AddToUI(IDuration m);
		public abstract List<IWidget> GetWidgets(IDuration d);
		public abstract void Update();

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}
	}


	class RandomDurationMonitor : BasicDurationMonitor
	{
		private RandomDuration duration_ = null;
		private readonly RandomizableTimeMonitorWidgets timeWidgets_;

		public override string DurationType
		{
			get { return RandomDuration.FactoryTypeName; }
		}

		public RandomDurationMonitor(string name, int flags)
			: base(flags)
		{
			timeWidgets_ = new RandomizableTimeMonitorWidgets(
				name, flags_);
		}

		public override void AddToUI(IDuration d)
		{
			duration_ = d as RandomDuration;
			if (duration_ == null)
				return;

			foreach (var w in timeWidgets_.GetWidgets())
				widgets_.AddToUI(w);
		}

		public override List<IWidget> GetWidgets(IDuration d)
		{
			duration_ = d as RandomDuration;
			return timeWidgets_.GetWidgets();
		}

		public override void Update()
		{
			timeWidgets_.SetValue(duration_?.Time);
		}
	}


	class RampDurationMonitor : BasicDurationMonitor
	{
		private RampDuration duration_ = null;

		private readonly FloatSlider start_;
		private readonly FloatSlider end_;
		private readonly FloatSlider timeUp_;
		private readonly FloatSlider timeDown_;
		private readonly FloatSlider hold_;

		private readonly FloatSlider firstHalfProgress_;
		private readonly FloatSlider secondHalfProgress_;
		private readonly Checkbox inFirstHalf_;
		private readonly FloatSlider totalProgress_;
		private readonly Checkbox inFirstHalfTotal_;
		private readonly Checkbox firstHalfFinished_;
		private readonly Checkbox finished_;
		private readonly FloatSlider timeRemaining_;
		private readonly FloatSlider timeRemainingInHalf_;
		private readonly FloatSlider current_;
		private readonly FloatSlider elapsed_;
		private readonly FloatSlider totalElapsed_;
		private readonly FloatSlider progress_;
		private readonly FloatSlider holdingProgress_;
		private readonly FloatSlider holdingElapsed_;

		public override string DurationType
		{
			get { return RampDuration.FactoryTypeName; }
		}

		public RampDurationMonitor(string name, int flags)
			: base(flags)
		{
			start_ = new FloatSlider(
				name + " start", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			end_ = new FloatSlider(
				name + " end", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			timeUp_ = new FloatSlider(
				name + " time up", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			timeDown_ = new FloatSlider(
				name + " time down", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			hold_ = new FloatSlider(
				name + " hold maximum", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			firstHalfProgress_ = new FloatSlider(
				name + " first half progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			secondHalfProgress_ = new FloatSlider(
				name + " second half progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			inFirstHalf_ = new Checkbox(
				name + " in first half", null, flags_ | Widget.Disabled);

			totalProgress_ = new FloatSlider(
				name + " total progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			inFirstHalfTotal_ = new Checkbox(
				name + " in first half total", null, flags_ | Widget.Disabled);

			firstHalfFinished_ = new Checkbox(
				name + " first half finished", null, flags_ | Widget.Disabled);

			finished_ = new Checkbox(
				name + " finished", null, flags_ | Widget.Disabled);

			timeRemaining_ = new FloatSlider(
				name + " time remaining", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			timeRemainingInHalf_ = new FloatSlider(
				name + " time remaining in half", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);


			current_ = new FloatSlider(
				name + " current", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			elapsed_ = new FloatSlider(
				name + " elapsed", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			totalElapsed_ = new FloatSlider(
				name + " total elapsed", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			progress_ = new FloatSlider(
				name + " progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			holdingProgress_ = new FloatSlider(
				name + " hold progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			holdingElapsed_ = new FloatSlider(
				name + " holding elapsed", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);
		}

		public override void AddToUI(IDuration d)
		{
			duration_ = d as RampDuration;
			if (duration_ == null)
				return;

			foreach (var w in GetWidgets(d))
				widgets_.AddToUI(w);
		}

		public override List<IWidget> GetWidgets(IDuration d)
		{
			return new List<IWidget>()
			{
				start_,
				end_,
				timeUp_,
				timeDown_,
				hold_,

				firstHalfProgress_,
				secondHalfProgress_,
				inFirstHalf_,
				totalProgress_,
				inFirstHalfTotal_,
				firstHalfFinished_,
				finished_,
				timeRemaining_,
				timeRemainingInHalf_,

				current_,
				elapsed_,
				totalElapsed_,
				progress_,

				holdingProgress_,
				holdingElapsed_
			};
		}

		public override void Update()
		{
			if (duration_ == null)
			{
				start_.Value = 0;
				end_.Value = 0;
				timeUp_.Value = 0;
				timeDown_.Value = 0;
				hold_.Value = 0;

				firstHalfProgress_.Value = 0;
				secondHalfProgress_.Value = 0;
				inFirstHalf_.Value = false;
				totalProgress_.Value = 0;
				inFirstHalfTotal_.Value = false;
				firstHalfFinished_.Value = false;
				finished_.Value = false;
				timeRemaining_.Value = 0;
				timeRemainingInHalf_.Value = 0;

				current_.Value = 0;
				elapsed_.Value = 0;
				totalElapsed_.Value = 0;
				progress_.Value = 0;
				holdingProgress_.Value = 0;
				holdingElapsed_.Value = 0;
			}
			else
			{
				start_.Value = duration_.Range.Minimum;
				end_.Value = duration_.Range.Maximum;
				timeUp_.Value = duration_.TimeUp;
				timeDown_.Value = duration_.TimeDown;
				hold_.Value = duration_.Hold;

				firstHalfProgress_.Value = duration_.FirstHalfProgress;
				secondHalfProgress_.Value = duration_.SecondHalfProgress;
				inFirstHalf_.Value = duration_.InFirstHalf;
				totalProgress_.Value = duration_.TotalProgress;
				inFirstHalfTotal_.Value = duration_.InFirstHalfTotal;
				firstHalfFinished_.Value = duration_.FirstHalfFinished;
				finished_.Value = duration_.Finished;
				timeRemaining_.Value = duration_.TimeRemaining;
				timeRemainingInHalf_.Value = duration_.TimeRemainingInHalf;

				current_.Value = duration_.Current;
				elapsed_.Value = duration_.Elapsed;
				totalElapsed_.Value = duration_.TotalElapsed;
				progress_.Value = duration_.Progress;
				holdingProgress_.Set(0, duration_.Hold, duration_.HoldingElapsed);
				holdingElapsed_.Value = duration_.HoldingElapsed;
			}
		}
	}
}
