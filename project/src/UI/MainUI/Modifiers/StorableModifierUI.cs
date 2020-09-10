using System.Collections.Generic;

namespace Synergy
{
	class StorableModifierUI : AtomWithMovementUI
	{
		public override string ModifierType
		{
			get { return StorableModifier.FactoryTypeName; }
		}

		private StorableModifier modifier_ = null;
		private readonly StringList storables_;
		private readonly StringList parameters_;


		public StorableModifierUI(MainUI ui)
			: base(ui)
		{
			storables_ = new StringList(
				"Storable", StorableChanged, Widget.Right);

			parameters_ = new StringList(
				"Parameter", ParameterChanged, Widget.Right);

			storables_.OnOpen += UpdateStorables;
			parameters_.OnOpen += UpdateParameters;
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as StorableModifier;
			if (modifier_ == null)
				return;

			UpdateStorables();
			UpdateParameters();

			AddAtomWidgets(m);

			widgets_.AddToUI(storables_);
			widgets_.AddToUI(parameters_);

			AddAtomWithMovementWidgets(m);

			base.AddToTopUI(m);
		}

		private void UpdateStorables()
		{
			List<string> list;

			var a = modifier_?.Atom;
			if (a == null)
				list = new List<string>();
			else
				list = a.GetStorableIDs();

			Utilities.NatSort(list);
			storables_.Choices = list;
		}

		private void UpdateParameters()
		{
			List<string> list;

			var s = modifier_?.Storable;
			if (s == null)
				list = new List<string>();
			else
				list = s.GetAllParamAndActionNames();

			Utilities.NatSort(list);
			parameters_.Choices = list;
		}

		private void StorableChanged(string s)
		{
			if (modifier_ == null)
				return;

			modifier_.SetStorable(s);
			parameters_.Value = modifier_.Parameter?.Name ?? "";
		}

		private void ParameterChanged(string s)
		{
			modifier_.SetParameter(s);
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			if (modifier_ == null)
				return;

			storables_.Value = modifier_.Storable?.name ?? "";
			parameters_.Value = modifier_.Parameter?.Name ?? "";
		}
	}
}
