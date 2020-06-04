using System.Collections.Generic;

namespace Synergy
{
	class MovementMonitorWidgets
	{
		private readonly FloatSlider magnitude_;
		private readonly FloatSlider target_;
		private readonly RandomizableFloatMonitorWidgets min_;
		private readonly RandomizableFloatMonitorWidgets max_;

		public MovementMonitorWidgets(int flags)
		{
			magnitude_ = new FloatSlider(
				"Magnitude", 0f, new FloatRange(-500f, 500f), null,
				Widget.Disabled | flags);

			target_ = new FloatSlider(
				"Target", 0f, new FloatRange(-500f, 500f), null,
				Widget.Disabled | flags);

			min_ = new RandomizableFloatMonitorWidgets("Minimum", flags);
			max_ = new RandomizableFloatMonitorWidgets("Maximum", flags);
		}

		public List<IWidget> GetWidgets()
		{
			var list = new List<IWidget>();

			foreach (var w in min_.GetWidgets())
				list.Add(w);

			foreach (var w in max_.GetWidgets())
				list.Add(w);

			list.Add(magnitude_);
			list.Add(target_);

			return list;
		}

		public void Update(Movement mv)
		{
			magnitude_.Set(mv.AvailableRange, mv.Magnitude);

			target_.Value = mv.Target;
			min_.SetValue(mv.Minimum);
			max_.SetValue(mv.Maximum);
		}
	}
}
