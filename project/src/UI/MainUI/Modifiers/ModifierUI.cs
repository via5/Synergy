using System;
using System.Collections.Generic;

namespace Synergy
{
	using ModifierStringList = FactoryStringList<ModifierFactory, IModifier>;
	using ModifierSyncStringList = FactoryStringList<
		ModifierSyncFactory, IModifierSync>;

	class ModifierUI : IDisposable
	{
		private readonly MainUI ui_;
		private ModifierContainer currentModifier_ = null;

		private readonly Header header_;
		private readonly ConfirmableButton delete_;
		private readonly Checkbox enabled_;
		private readonly Button disableOthers_;
		private readonly Button enableAll_;
		private readonly ModifierStringList type_;
		private readonly ModifierSyncStringList modifierSync_;
		private IModifierSyncUI modifierSyncUI_ = null;
		private ISpecificModifierUI specificUI_ = null;

		private readonly WidgetList widgets_ = new WidgetList();

		public ModifierUI(MainUI ui)
		{
			ui_ = ui;

			header_ = new Header("", Widget.Right);

			delete_ = new ConfirmableButton(
				"Delete modifier", DeleteModifier, Widget.Right);

			enabled_ = new Checkbox(
				"Modifier enabled", true, EnabledChanged, Widget.Right);

			disableOthers_ = new Button(
				"Disable other modifiers", DisableOthers, Widget.Right);

			enableAll_ = new Button(
				"Enable all modifiers", EnableAll, Widget.Right);

			type_ = new ModifierStringList(
				"Type", "", TypeChanged, Widget.Right);

			modifierSync_  = new ModifierSyncStringList(
				"Sync", ModifierSyncChanged, Widget.Right);
		}

		public void Dispose()
		{
			ListenForModifierEvents(null);
		}

		public void AddToUI(ModifierContainer sm)
		{
			ListenForModifierEvents(sm?.Modifier);

			currentModifier_ = sm;
			if (currentModifier_ == null)
				return;

			header_.Text = currentModifier_.Name;
			enabled_.Value = sm.Enabled;

			var m = currentModifier_.Modifier;

			type_.Value = m;

			modifierSync_.Value = m?.ModifierSync;

			if (m == null)
			{
				modifierSyncUI_ = null;
				specificUI_ = null;
			}
			else
			{
				if (modifierSyncUI_ == null ||
					modifierSyncUI_.SyncType != m.ModifierSync.GetFactoryTypeName())
				{
					modifierSyncUI_ = CreateModifierSyncUI(m.ModifierSync);
				}

				if (specificUI_ == null ||
					specificUI_.ModifierType != m.GetFactoryTypeName())
				{
					specificUI_ = CreateSpecificUI(m);
				}
			}

			widgets_.AddToUI(header_);
			widgets_.AddToUI(delete_);
			widgets_.AddToUI(enabled_);
			widgets_.AddToUI(disableOthers_);
			widgets_.AddToUI(enableAll_);
			widgets_.AddToUI(type_);
			widgets_.AddToUI(modifierSync_);

			if (modifierSyncUI_ != null)
			{
				modifierSyncUI_.AddToUI(m.ModifierSync);
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
			}

			if (specificUI_ != null)
			{
				specificUI_.AddToTopUI(m);
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
			}

			if (specificUI_ != null)
			{
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
				specificUI_.AddToBottomUI(m);
			}
		}

		public void RemoveFromUI()
		{
			widgets_.RemoveFromUI();

			if (modifierSyncUI_ != null)
				modifierSyncUI_.RemoveFromUI();

			if (specificUI_ != null)
				specificUI_.RemoveFromUI();
		}

		private void DeleteModifier()
		{
			if (currentModifier_ != null)
			{
				if (currentModifier_.Step != null)
				{
					currentModifier_.Step.DeleteModifier(currentModifier_);
					Synergy.Instance.UI.NeedsReset("modifier deleted");
				}
			}
		}

		private ISpecificModifierUI CreateSpecificUI(IModifier m)
		{
			if (m is RigidbodyModifier)
				return new RigidbodyModifierUI(ui_);
			else if (m is MorphModifier)
				return new MorphModifierUI(ui_);
			else if (m is LightModifier)
				return new LightModifierUI(ui_);
			else if (m is AudioModifier)
				return new AudioModifierUI(ui_);
			else
				return null;
		}

		private IModifierSyncUI CreateModifierSyncUI(IModifierSync m)
		{
			if (m is DurationSyncedModifier)
				return new DurationSyncModifierUI(Widget.Right);
			else if (m is UnsyncedModifier)
				return new UnsyncedModifierUI(Widget.Right);
			else if (m is OtherModifierSyncedModifier)
				return new OtherModifierSyncedModifierUI(Widget.Right);
			else
				return null;
		}

		protected virtual void ListenForModifierEvents(IModifier newModifier)
		{
			var oldModifier = currentModifier_?.Modifier;

			if (oldModifier != null)
				oldModifier.NameChanged -= NameChanged;

			if (newModifier != null)
				newModifier.NameChanged += NameChanged;
		}

		private void NameChanged()
		{
			if (currentModifier_ != null)
				header_.Text = currentModifier_.Name;
			else
				header_.Text = "";
		}

		private void TypeChanged(IModifier m)
		{
			if (currentModifier_ == null)
				return;

			ListenForModifierEvents(m);

			currentModifier_.Modifier = m;
			ui_.NeedsReset("modifier type changed");
		}

		private void ModifierSyncChanged(IModifierSync s)
		{
			if (currentModifier_?.Modifier == null)
				return;

			currentModifier_.Modifier.ModifierSync = s;
			Synergy.Instance.UI.NeedsReset("modifier sync type changed");
		}

		private void EnabledChanged(bool b)
		{
			if (currentModifier_ != null)
				currentModifier_.Enabled = b;
		}

		private void DisableOthers()
		{
			currentModifier_?.Step?.DisableAllExcept(currentModifier_);
			enabled_.Value = currentModifier_?.Enabled ?? false;
		}

		private void EnableAll()
		{
			currentModifier_?.Step?.EnableAll();
			enabled_.Value = true;
		}
	}



	interface IModifierSyncUI
	{
		string SyncType { get; }
		void AddToUI(IModifierSync m);
		void RemoveFromUI();
	}


	abstract class BasicModifierSyncUI : IModifierSyncUI
	{
		public abstract string SyncType { get; }

		protected readonly int flags_;

		protected BasicModifierSyncUI(int flags)
		{
			flags_ = flags;
		}

		public abstract void AddToUI(IModifierSync m);
		public abstract void RemoveFromUI();
	}


	class DurationSyncModifierUI : BasicModifierSyncUI
	{
		public override string SyncType
		{
			get { return DurationSyncedModifier.FactoryTypeName; }
		}

		public DurationSyncModifierUI(int flags)
			: base(flags)
		{
		}

		public override void AddToUI(IModifierSync m)
		{
		}

		public override void RemoveFromUI()
		{
		}
	}


	class UnsyncedModifierUI : BasicModifierSyncUI
	{
		public override string SyncType
		{
			get { return UnsyncedModifier.FactoryTypeName; }
		}

		private readonly Collapsible durationCollapsible_;
		private readonly DurationWidgets durationWidgets_;

		private readonly Collapsible delayCollapsible_;
		private readonly DelayWidgets delayWidgets_;

		private UnsyncedModifier unsynced_ = null;

		public UnsyncedModifierUI(int flags)
			: base(flags)
		{
			durationCollapsible_ = new Collapsible(
				"Duration", null, flags_);

			durationWidgets_ = new DurationWidgets(
				"Duration", DurationTypeChanged, flags_);

			delayCollapsible_ = new Collapsible("Delay", null, flags_);
			delayWidgets_ = new DelayWidgets(flags_);
			delayWidgets_.SupportsHalfMove = false;
		}

		public override void AddToUI(IModifierSync s)
		{
			unsynced_ = s as UnsyncedModifier;
			if (unsynced_ == null)
				return;

			durationWidgets_.SetValue(unsynced_.Duration);
			delayWidgets_.SetValue(unsynced_.Delay);

			durationCollapsible_.Clear();
			durationCollapsible_.Add(durationWidgets_.GetWidgets());

			delayCollapsible_.Clear();
			delayCollapsible_.Add(delayWidgets_.GetWidgets());

			durationCollapsible_.AddToUI();
			delayCollapsible_.AddToUI();
		}

		public override void RemoveFromUI()
		{
			durationCollapsible_.RemoveFromUI();
			delayCollapsible_.RemoveFromUI();
		}

		private void DurationTypeChanged(IDuration d)
		{
			if (unsynced_ == null)
				return;

			unsynced_.Duration = d;
			Synergy.Instance.UI.NeedsReset("modifier sync duration changed");
		}
	}


	class OtherModifierSyncedModifierUI : BasicModifierSyncUI
	{
		public override string SyncType
		{
			get { return OtherModifierSyncedModifier.FactoryTypeName; }
		}

		private readonly StringList modifiers_;
		private OtherModifierSyncedModifier sync_ = null;

		public OtherModifierSyncedModifierUI(int flags)
			: base(flags)
		{
			modifiers_ = new StringList(
				"Other modifier", "", new List<string>(),
				ModifierChanged, flags_);
		}

		public override void AddToUI(IModifierSync s)
		{
			sync_ = s as OtherModifierSyncedModifier;
			if (sync_?.ParentModifier?.ParentStep == null)
				return;

			var names = new List<string>();
			bool found = false;

			foreach (var m in sync_.ParentModifier.ParentStep.Modifiers)
			{
				if (m.Modifier == sync_.ParentModifier)
					continue;

				names.Add(m.Name);

				if (sync_.OtherModifier == m.Modifier)
					found = true;
			}

			modifiers_.Choices = names;

			if (found)
			{
				modifiers_.Value = sync_.OtherModifier.Name;
			}
			else
			{
				if (sync_.OtherModifier != null)
				{
					Synergy.LogError(
						"modifier '" + sync_.OtherModifier.Name + "' " +
						"not found");
				}

				modifiers_.Value = "";
			}

			modifiers_.AddToUI();
		}

		public override void RemoveFromUI()
		{
			modifiers_.RemoveFromUI();
		}

		private void ModifierChanged(string value)
		{
			if (sync_?.ParentModifier?.ParentStep == null)
				return;

			foreach (var m in sync_.ParentModifier.ParentStep.Modifiers)
			{
				if (m.Name == value)
				{
					sync_.OtherModifier = m.Modifier;

					Synergy.Instance.UI.NeedsReset(
						"modifier sync with modifier changed");

					return;
				}
			}

			Synergy.LogError("modifier '" + value + "' not found");
		}
	}
}
