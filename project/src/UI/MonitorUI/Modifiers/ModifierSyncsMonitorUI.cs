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
		private readonly Checkbox mustStop_;
		private readonly FloatSlider gracePeriod_;

		public BasicModifierSyncMonitor(int flags)
		{
			flags_ = flags;

			mustStop_ = new Checkbox(
				"Must stop", null, flags | Widget.Disabled);

			gracePeriod_ = new FloatSlider(
				"Grace period", null, flags | Widget.Disabled);
		}

		public abstract string SyncType { get; }

		public virtual void AddToUI(IModifierSync s)
		{
			sync_ = s;
			mustStop_.AddToUI();
			gracePeriod_.AddToUI();
		}

		public virtual void RemoveFromUI()
		{
			mustStop_.RemoveFromUI();
			gracePeriod_.RemoveFromUI();
		}

		public virtual void Update()
		{
			if (sync_ == null || !sync_.MustStopWhenFinished)
			{
				mustStop_.Value = false;
				gracePeriod_.Value = -1;
			}
			else
			{
				mustStop_.Value = true;
				gracePeriod_.Value = sync_.StopGracePeriod;
				gracePeriod_.Range = new FloatRange(0, 5);
			}
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
		private IDurationMonitor delay_;

		public UnsyncedModifierModifier(int flags)
			: base(flags)
		{
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

			delay_ = MonitorUI.CreateDurationMonitor(
				"Delay", us.Delay.Duration, flags_);

			if (duration_ != null)
				duration_.AddToUI(us.Duration);

			delay_.AddToUI(us.Delay.Duration);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();

			if (duration_ != null)
				duration_.RemoveFromUI();

			delay_.RemoveFromUI();
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
