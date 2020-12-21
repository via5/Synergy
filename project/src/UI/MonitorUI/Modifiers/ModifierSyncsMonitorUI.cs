namespace Synergy
{
	interface IModifierSyncMonitor
	{
		string SyncType { get; }
		void AddToUI(IModifierSync s);
		void RemoveFromUI();
		void Update();
	}

	abstract class BasicModifierSyncMonitor : IModifierSyncMonitor
	{
		protected readonly int flags_;

		private IModifierSync sync_ = null;
		private FloatSlider progress_;
		protected WidgetList widgets_ = new WidgetList();

		public BasicModifierSyncMonitor(int flags)
		{
			flags_ = flags;
			progress_ = new FloatSlider("Duration progress", null, flags);
		}

		public abstract string SyncType { get; }

		public virtual void AddToUI(IModifierSync s)
		{
			sync_ = s;
			widgets_.AddToUI(progress_);
		}

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}

		public virtual void Update()
		{
			if (sync_ != null)
				progress_.Set(0, 1, sync_.DurationProgress);
		}
	}

	class DurationSyncedModifierMonitor : BasicModifierSyncMonitor
	{
		public override string SyncType
		{
			get { return DurationSyncedModifier.FactoryTypeName; }
		}

		public DurationSyncedModifierMonitor(int flags)
			: base(flags)
		{
		}
	}

	class UnsyncedModifierModifier : BasicModifierSyncMonitor
	{
		public override string SyncType
		{
			get { return UnsyncedModifier.FactoryTypeName; }
		}

		private IDurationMonitor duration_ = null;
		private DelayMonitor delay_;

		public UnsyncedModifierModifier(int flags)
			: base(flags)
		{
			delay_ = new DelayMonitor(flags);
		}

		public override void AddToUI(IModifierSync s)
		{
			var us = s as UnsyncedModifier;
			if (us == null)
				return;

			if (duration_ == null ||
				duration_.DurationType != us.Duration.GetFactoryTypeName())
			{
				duration_ = MonitorUI.CreateDurationMonitor(
					"Unsynced duration", us.Duration, flags_);
			}

			if (duration_ != null)
				duration_.AddToUI(us.Duration);

			foreach (var w in delay_.GetWidgets(us.Delay))
				widgets_.AddToUI(w);

			base.AddToUI(s);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			if (duration_ != null)
				duration_.RemoveFromUI();
		}

		public override void Update()
		{
			base.Update();

			if (duration_ != null)
				duration_.Update();

			delay_.Update();
		}
	}
}
