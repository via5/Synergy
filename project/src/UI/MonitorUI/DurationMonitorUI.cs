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
		private readonly FloatSlider elapsed_;
		private readonly FloatSlider totalElapsed_;
		private readonly FloatSlider current_;
		private readonly FloatSlider holdingProgress_;

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

			elapsed_ = new FloatSlider(
				name + " elapsed", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			totalElapsed_ = new FloatSlider(
				name + " total elapsed", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			current_ = new FloatSlider(
				name + " current", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);

			holdingProgress_ = new FloatSlider(
				name + " hold progress", 0, new FloatRange(0, 0), null,
				flags_ | FloatSlider.Disabled);
		}

		public override void AddToUI(IDuration d)
		{
			duration_ = d as RampDuration;
			if (duration_ == null)
				return;

			widgets_.AddToUI(start_);
			widgets_.AddToUI(end_);
			widgets_.AddToUI(timeUp_);
			widgets_.AddToUI(timeDown_);
			widgets_.AddToUI(hold_);

			widgets_.AddToUI(elapsed_);
			widgets_.AddToUI(totalElapsed_);
			widgets_.AddToUI(current_);
			widgets_.AddToUI(holdingProgress_);
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

				elapsed_,
				totalElapsed_,
				current_,
				holdingProgress_,
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

				elapsed_.Value = 0;
				totalElapsed_.Value = 0;
				current_.Value = 0;
				holdingProgress_.Value = 0;
			}
			else
			{
				start_.Value = duration_.Range.Minimum;
				end_.Value = duration_.Range.Maximum;
				timeUp_.Value = duration_.TimeUp;
				timeDown_.Value = duration_.TimeDown;
				hold_.Value = duration_.Hold;

				elapsed_.Value = duration_.Elapsed;
				totalElapsed_.Value = duration_.TotalElapsed;
				current_.Value = duration_.Current;
				holdingProgress_.Set(0, duration_.Hold, duration_.HoldingElapsed);
			}
		}
	}
}
