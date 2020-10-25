using System;
using System.Collections.Generic;
using System.Text;

namespace Synergy
{
	class StepExtenderModifierUI : AtomModifierUI
	{
		public override string ModifierType
		{
			get { return StepExtenderModifier.FactoryTypeName; }
		}


		private StepExtenderModifier modifier_ = null;
		private readonly StorableTypesList types_;
		private readonly StorableWidgets storableWidgets_;

		public StepExtenderModifierUI(MainUI ui)
			: base(ui)
		{
			types_ = new StorableTypesList(new List<string> { "float" }, Widget.Right);
			types_.OnTypeChanged += TypeChanged;

			storableWidgets_ = new StorableWidgets(Widget.Right);
			storableWidgets_.OnParameterChanged += UpdateParameter;
			storableWidgets_.Type = types_.Value;
		}

		public override void DeferredInit()
		{
			storableWidgets_.DeferredInit(modifier_?.Holder);
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as StepExtenderModifier;
			if (modifier_ == null)
				return;

			AddAtomWidgets(m);
			widgets_.AddToUI(types_);
			storableWidgets_.AddToUI(modifier_.Holder);

			widgets_.AddToUI(new LargeSpacer(Widget.Right));
			widgets_.AddToUI(new LargeSpacer(Widget.Right));
			widgets_.AddToUI(new LargeSpacer(Widget.Right));
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();
			storableWidgets_.RemoveFromUI();
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			if (modifier_ == null)
				return;

			storableWidgets_.AtomChanged();
			UpdateParameter();
		}

		private void UpdateParameter()
		{
			ui_.NeedsReset("step extender parameter changed");
		}

		private void TypeChanged(string s)
		{
			storableWidgets_.Type = s;
		}
	}
}
