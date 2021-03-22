using System.Collections.Generic;
using UI = SynergyUI;

namespace Synergy.NewUI
{
	class TimelineModifierPanel : BasicModifierPanel
	{
		private TimelineModifier modifier_ = null;
		private readonly IgnoreFlag ignore_ = new IgnoreFlag();

		private readonly AtomComboBox atom_ = new AtomComboBox();
		private readonly UI.ComboBox<string> anim_ = new UI.ComboBox<string>();
		private readonly IntegrationSettingWidget gazeActive_ = new IntegrationSettingWidget();
		private readonly IntegrationSettingWidget gazeInactive_ = new IntegrationSettingWidget();
		private readonly IntegrationSettingWidget blinkActive_ = new IntegrationSettingWidget();
		private readonly IntegrationSettingWidget blinkInactive_ = new IntegrationSettingWidget();
		private readonly UI.CheckBox inhibitEyeModifiers_ = new UI.CheckBox();
		private readonly DelayWidgets delay_ = new DelayWidgets();


		public TimelineModifierPanel()
		{
			//Layout = new UI.BorderLayout();
			Layout = new UI.GridLayout(2, 10);

			//var top = new UI.Panel(new UI.GridLayout(2, 10));
			var top = this;

			top.Add(new UI.Label(S("Atom")));
			top.Add(atom_);

			top.Add(new UI.Label(S("Timeline animation")));
			top.Add(anim_);

			top.Add(new UI.Label(Integration.Gaze.Name + " (active)"));
			top.Add(gazeActive_);
			top.Add(new UI.Label(Integration.Gaze.Name + " (inactive)"));
			top.Add(gazeInactive_);

			top.Add(new UI.Label(Integration.Blink.Name + " (active)"));
			top.Add(blinkActive_);
			top.Add(new UI.Label(Integration.Blink.Name + " (inactive)"));
			top.Add(blinkInactive_);

			top.Add(new UI.Label(S("Disable eye modifiers")));
			top.Add(inhibitEyeModifiers_);

			//Add(top, UI.BorderLayout.Center);

			atom_.AtomSelectionChanged += OnAtomChanged;
		}

		public override string Title
		{
			get { return S("Timeline"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is TimelineModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as TimelineModifier;
			if (modifier_ == null)
				return;

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				UpdateAnimations();
			});
		}


		private void OnAtomChanged(Atom atom)
		{
			if (ignore_ || modifier_ == null)
				return;

			modifier_.Atom = atom;
			UpdateAnimations();
		}

		private void UpdateAnimations()
		{
			var list = new List<string>();
			list.Add("");

			if (modifier_ != null)
				list.AddRange(modifier_.Timeline.Animations);

			anim_.SetItems(list, modifier_.Animation);
		}
	}
}
