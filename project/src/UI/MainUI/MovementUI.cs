using System.Collections.Generic;

namespace Synergy
{
	using EasingStringList = FactoryStringList<EasingFactory, IEasing>;

	class MovementUI
	{
		private Movement m_ = null;

		private readonly EasingStringList easing_;
		protected readonly RandomizableFloatWidgets min_;
		protected readonly RandomizableFloatWidgets max_;

		public MovementUI(int flags = 0)
		{
			easing_ = new EasingStringList("Easing", "", EasingChanged, flags);
			min_ = new RandomizableFloatWidgets("Minimum", flags);
			max_ = new RandomizableFloatWidgets("Maximum", flags);
		}

		public void SetValue(Movement m, FloatRange preferredRange)
		{
			m_ = m;

			if (m_ != null)
			{
				if (preferredRange == null)
					preferredRange = new FloatRange(0, 1);

				easing_.Value = m_.Easing;
				min_.SetValue(m_.Minimum, preferredRange);
				max_.SetValue(m_.Maximum, preferredRange);
			}

		}

		public List<IWidget> GetWidgets()
		{
			var list = new List<IWidget>();

			list.Add(easing_);
			list.Add(new SmallSpacer(Widget.Right));

			foreach (var w in min_.GetWidgets())
				list.Add(w);

			list.Add(new SmallSpacer(Widget.Right));
			foreach (var w in max_.GetWidgets())
				list.Add(w);

			return list;
		}

		public void SetPreferredRange(FloatRange r)
		{
			if (m_ != null)
			{
				min_.SetValue(m_.Minimum, r);
				max_.SetValue(m_.Maximum, r);
			}
		}

		private void EasingChanged(IEasing e)
		{
			if (m_ != null && e != null)
				m_.Easing = e;
		}
	}
}
