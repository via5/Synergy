using System;
using System.Collections.Generic;

namespace Synergy
{
	interface ISpecificModifierUI
	{
		void AddToTopUI(IModifier m);
		void AddToBottomUI(IModifier m);
		void RemoveFromUI();
		string ModifierType { get; }
		void PluginEnabled(bool b);
		void Update();
	}

	abstract class BasicSpecificModifierUI : ISpecificModifierUI
	{
		protected MainUI ui_;
		protected readonly WidgetList widgets_ = new WidgetList();

		public abstract string ModifierType { get; }

		protected BasicSpecificModifierUI(MainUI ui)
		{
			ui_ = ui;
		}

		public virtual void AddToTopUI(IModifier m)
		{
			// no-op
		}

		public virtual void AddToBottomUI(IModifier m)
		{
			// no-op
		}

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}

		public virtual void PluginEnabled(bool b)
		{
			// no-op
		}

		public virtual void Update()
		{
			// no-op
		}
	}

	abstract class AtomModifierUI : BasicSpecificModifierUI
	{
		private AtomModifier modifier_ = null;
		private readonly AtomList atom_;

		public AtomModifierUI(MainUI ui, AtomList.AtomPredicate pred = null)
			: base(ui)
		{
			atom_ = new AtomList("Atom", "", AtomChanged, pred, Widget.Right);
		}

		protected void AddAtomWidgets(IModifier m)
		{
			modifier_ = m as AtomModifier;
			if (modifier_ == null)
				return;

			if (modifier_.Atom == null)
				atom_.Value = "";
			else
				atom_.Value = modifier_.Atom.name;

			widgets_.AddToUI(atom_);
		}

		protected virtual void AtomChanged(Atom atom)
		{
			if (modifier_ == null)
				return;

			modifier_.Atom = atom;
		}
	}


	abstract class AtomWithMovementUI : AtomModifierUI, IDisposable
	{
		private AtomWithMovementModifier currentModifier_ = null;
		private readonly MovementUI movementUI_;

		public AtomWithMovementUI(MainUI ui, AtomList.AtomPredicate pred = null)
			: base(ui, pred)
		{
			movementUI_ = new MovementUI(Widget.Right);
		}

		public void Dispose()
		{
			if (currentModifier_ != null)
				currentModifier_.PreferredRangeChanged -= PreferredRangeChanged;
		}

		protected void AddAtomWithMovementWidgets(IModifier m)
		{
			if (currentModifier_ != null)
				currentModifier_.PreferredRangeChanged -= PreferredRangeChanged;

			currentModifier_ = m as AtomWithMovementModifier;

			if (currentModifier_ != null)
				currentModifier_.PreferredRangeChanged += PreferredRangeChanged;

			movementUI_.SetValue(
				currentModifier_?.Movement, currentModifier_?.PreferredRange);

			foreach (var w in movementUI_.GetWidgets())
				widgets_.AddToUI(w);
		}

		private void PreferredRangeChanged()
		{
			if (currentModifier_ != null)
				movementUI_.SetPreferredRange(currentModifier_.PreferredRange);
		}
	}
}
