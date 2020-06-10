using System.Collections.Generic;

namespace Synergy
{
	class DurationWidgets : CompoundWidget
	{
		public delegate void TypeChangedCallback(IDuration d);

		private readonly FactoryStringList<
			DurationFactory, IDuration> durationType_;

		private readonly string name_;
		private readonly TypeChangedCallback callback_;

		private IDurationUI durationUI_ = null;

		public DurationWidgets(
			string name, TypeChangedCallback callback = null, int flags = 0)
				: base(flags)
		{
			name_ = name;
			callback_ = callback;

			string typeLabel = "";

			if (name == "")
				typeLabel = "Duration type";
			else
				typeLabel = name + " duration type";

			durationType_ = new FactoryStringList<DurationFactory, IDuration>(
				typeLabel, "", TypeChanged, flags);
		}

		public override bool Enabled
		{
			get
			{
				return base.Enabled;
			}

			set
			{
				base.Enabled = value;

				foreach (var w in GetWidgets())
					w.Enabled = value;
			}
		}

		public void SetValue(IDuration d)
		{
			durationType_.Value = d;

			if (d != null)
			{
				if (durationUI_ == null ||
					durationUI_.DurationType != d.GetFactoryTypeName())
				{
					durationUI_ = CreateDurationUI(d);
				}
			}

			if (durationUI_ != null)
				durationUI_.SetValue(d);
		}

		public List<IWidget> GetWidgets()
		{
			var list = new List<IWidget>()
			{
				durationType_
			};

			if (durationUI_ != null)
				list.AddRange(durationUI_.GetWidgets());

			return list;
		}

		protected override void DoAddToUI()
		{
			foreach (var w in GetWidgets())
				w.AddToUI();
		}

		protected override void DoRemoveFromUI()
		{
			foreach (var w in GetWidgets())
				w.RemoveFromUI();
		}

		private IDurationUI CreateDurationUI(IDuration d)
		{
			if (d is RandomDuration)
				return new RandomDurationUI(name_, flags_);
			else if (d is RampDuration)
				return new RampDurationUI(name_, flags_);
			else
				return null;
		}

		private void TypeChanged(IDuration d)
		{
			callback_?.Invoke(d);
		}
	}


	interface IDurationUI
	{
		string DurationType { get; }
		void SetValue(IDuration d);
		List<IWidget> GetWidgets();
	}

	abstract class BasicDurationUI : IDurationUI
	{
		private readonly string name_;

		protected BasicDurationUI(string name)
		{
			name_ = name;
		}

		public abstract string DurationType { get; }
		public abstract void SetValue(IDuration d);
		public abstract List<IWidget> GetWidgets();

		protected string MakeText(string s)
		{
			if (name_ == "" || name_ == "Duration")
				return s;
			else
				return name_ + " " + char.ToLower(s[0]) + s.Substring(1);
		}
	}


	class RandomDurationUI : BasicDurationUI
	{
		public override string DurationType
		{
			get { return RandomDuration.FactoryTypeName; }
		}


		private readonly RandomizableTimeWidgets durationWidgets_;

		public RandomDurationUI(string name, int flags = 0)
			: base(name)
		{
			durationWidgets_ = new RandomizableTimeWidgets(
				MakeText("Duration"), flags);
		}

		public override void SetValue(IDuration d)
		{
			var rd = d as RandomDuration;
			durationWidgets_.SetValue(rd?.Time, new FloatRange(0, 10));
		}

		public override List<IWidget> GetWidgets()
		{
			return durationWidgets_.GetWidgets();
		}
	}


	class RampDurationUI : BasicDurationUI
	{
		public override string DurationType
		{
			get { return RampDuration.FactoryTypeName; }
		}


		private readonly FloatSlider over_;
		private readonly FloatSlider min_;
		private readonly FloatSlider max_;
		private readonly FloatSlider hold_;
		private readonly Checkbox rampUp_;
		private readonly Checkbox rampDown_;
		private readonly FactoryStringList<
			EasingFactory, IEasing> easing_;

		private RampDuration duration_ = null;


		public RampDurationUI(string name, int flags = 0)
			: base(name)
		{
			over_ = new FloatSlider(
				MakeText("Ramp time"), DurationChanged, flags);

			min_ = new FloatSlider(
				MakeText("Minimum duration"), MinimumChanged, flags);

			max_ = new FloatSlider(
				MakeText("Maximum duration"), MaximumChanged, flags);

			hold_ = new FloatSlider(
				MakeText("Hold maximum"), HoldChanged, flags);

			rampUp_ = new Checkbox(
				MakeText("Ramp up"), RampUpChanged, flags);

			rampDown_ = new Checkbox(
				MakeText("Ramp down"), RampDownChanged, flags);

			easing_ = new FactoryStringList<EasingFactory, IEasing>(
				MakeText("Easing"), EasingChanged, flags);
		}

		public override void SetValue(IDuration d)
		{
			duration_ = d as RampDuration;

			if (duration_ == null)
			{
				over_.Value = 0;
				min_.Value = 0;
				max_.Value = 0;
				hold_.Value = 0;
				rampUp_.Value = false;
				rampDown_.Value = false;
				rampUp_.Parameter = null;
				rampDown_.Parameter = null;
			}
			else
			{
				over_.Parameter = duration_.OverParameter;
				min_.Parameter = duration_.MinimumParameter;
				max_.Parameter = duration_.MaximumParameter;
				hold_.Parameter = duration_.HoldParameter;
				rampUp_.Parameter = duration_.RampUpParameter;
				rampDown_.Parameter = duration_.RampDownParameter;
			}

			easing_.Value = duration_.Easing;
		}

		public override List<IWidget> GetWidgets()
		{
			return new List<IWidget>()
			{
				over_,
				min_,
				max_,
				hold_,
				rampUp_,
				rampDown_,
				easing_
			};
		}

		private void DurationChanged(float f)
		{
			if (duration_ != null)
				duration_.Over = f;
		}

		private void MinimumChanged(float f)
		{
			if (duration_ != null)
				duration_.Minimum = f;
		}

		private void MaximumChanged(float f)
		{
			if (duration_ != null)
				duration_.Maximum = f;
		}

		private void HoldChanged(float f)
		{
			if (duration_ != null)
				duration_.Hold = f;
		}

		private void RampDownChanged(bool b)
		{
			if (duration_ != null)
				duration_.RampDown = b;
		}

		private void RampUpChanged(bool b)
		{
			if (duration_ != null)
				duration_.RampUp = b;
		}

		private void EasingChanged(IEasing v)
		{
			if (duration_ != null)
				duration_.Easing = v;
		}
	}
}
