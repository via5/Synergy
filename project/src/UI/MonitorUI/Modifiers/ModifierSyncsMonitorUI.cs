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

		public BasicModifierSyncMonitor(int flags)
		{
			flags_ = flags;
		}

		public abstract string SyncType { get; }

		public virtual void AddToUI(IModifierSync s)
		{
			sync_ = s;
		}

		public virtual void RemoveFromUI()
		{
		}

		public virtual void Update()
		{
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
		private WidgetList widgets_ = new WidgetList();

		public UnsyncedModifierModifier(int flags)
			: base(flags)
		{
			delay_ = new DelayMonitor(flags);
		}

		public override void AddToUI(IModifierSync s)
		{
			base.AddToUI(s);

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
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			widgets_.RemoveFromUI();

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
