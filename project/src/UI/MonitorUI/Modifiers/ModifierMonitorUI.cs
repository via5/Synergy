namespace Synergy
{
	interface IModifierMonitor
	{
		string ModifierType { get; }
		void AddToUI(IModifier m);
		void RemoveFromUI();
		void Update();
	}


	abstract class BasicModifierMonitor : IModifierMonitor
	{
		protected readonly WidgetList widgets_ = new WidgetList();
		private readonly Label calls_;
		private IModifierSyncMonitor syncMonitor_ = null;
		private readonly FloatSlider timeRemaining_;

		private IModifier modifier_ = null;

		public BasicModifierMonitor()
		{
			calls_ = new Label("", Widget.Right);
			timeRemaining_ = new FloatSlider(
				"Time remaining", null, Widget.Right);
		}

		public abstract string ModifierType { get; }

		public virtual void AddToUI(IModifier m)
		{
			modifier_ = m;

			widgets_.AddToUI(calls_);

			if (m?.ModifierSync == null)
			{
				syncMonitor_ = null;
			}
			else
			{
				if (syncMonitor_ == null ||
					syncMonitor_.SyncType != m.ModifierSync.GetFactoryTypeName())
				{
					syncMonitor_ = CreateSyncMonitor(
						m?.ModifierSync, Widget.Right);
				}
			}

			if (syncMonitor_ != null)
				syncMonitor_.AddToUI(m.ModifierSync);

			widgets_.AddToUI(timeRemaining_);
		}

		public virtual void RemoveFromUI()
		{
			if (syncMonitor_ != null)
				syncMonitor_.RemoveFromUI();

			widgets_.RemoveFromUI();
		}

		public virtual void Update()
		{
			calls_.Text =
				"Ticks: " +
				(modifier_?.TickCalls.ToString() ?? "-") + " " +
				"Sets: " +
				(modifier_?.SetCalls.ToString() ?? "-");

			if (syncMonitor_ != null)
				syncMonitor_.Update();

			timeRemaining_.Value = modifier_?.TimeRemaining ?? 0;
		}

		private IModifierSyncMonitor CreateSyncMonitor(
			IModifierSync m, int flags)
		{
			if (m is DurationSyncedModifier)
				return new DurationSyncedModifierMonitor(flags);
			else if (m is UnsyncedModifier)
				return new UnsyncedModifierModifier(flags);
			else
				return null;
		}
	}


	abstract class ModifierWithMovementMonitor : BasicModifierMonitor
	{
		private AtomWithMovementModifier modifier_ = null;
		private MovementMonitorWidgets movement_;

		public ModifierWithMovementMonitor()
		{
			movement_ = new MovementMonitorWidgets(Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			base.AddToUI(m);

			modifier_ = m as AtomWithMovementModifier;
			if (modifier_ == null)
				return;

			foreach (var w in movement_.GetWidgets())
				widgets_.AddToUI(w);
		}

		public override void Update()
		{
			base.Update();

			var mv = modifier_?.Movement;
			if (mv == null)
				return;

			movement_.Update(mv);
		}
	}
}
