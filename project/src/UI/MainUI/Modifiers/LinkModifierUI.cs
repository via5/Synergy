using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class LinkModifierControllerUI
	{
		private readonly LinkModifier modifier_;
		private readonly LinkModifierController mc_;

		private readonly Collapsible collapsible_;
		private readonly ConfirmableButton delete_;
		private readonly FreeControllerList controller_;
		private readonly AtomList atom_;
		private readonly LinkTargetList rb_;
		private readonly PositionStateList position_;
		private readonly RotationStateList rotation_;

		public LinkModifierControllerUI(
			LinkModifier m, LinkModifierController mc)
		{
			modifier_ = m;
			mc_ = mc;

			collapsible_ = new Collapsible(mc_.Name, null, Widget.Right);

			delete_ = new ConfirmableButton("Delete", Delete, Widget.Right);

			controller_ = new FreeControllerList(
				"Controller", mc_.ControllerName,
				ControllerChanged, Widget.Right);

			atom_ = new AtomList(
				"Link to atom", mc_.AtomName, AtomChanged, null,
				Widget.Right | Widget.AllowNone);

			rb_ = new LinkTargetList(
				"Link to", mc_.RigidbodyName, RigidbodyChanged,
				Widget.Right | Widget.AllowNone);

			position_ = new PositionStateList(
				"Position", "", PositionChanged,
				Widget.Right | Widget.AllowNone);

			rotation_ = new RotationStateList(
				"Rotation", "", RotationChanged,
				Widget.Right | Widget.AllowNone);

			Update();

			collapsible_.Add(delete_);
			collapsible_.Add(controller_);
			collapsible_.Add(atom_);
			collapsible_.Add(rb_);
			collapsible_.Add(position_);
			collapsible_.Add(rotation_);
			collapsible_.Add(new SmallSpacer(Widget.Right));
		}

		public Collapsible Collapsible
		{
			get { return collapsible_; }
		}

		private void Delete()
		{
			if (modifier_ == null || mc_ == null)
				return;

			modifier_.RemoveController(mc_);
			Synergy.Instance.UI.NeedsReset("link target removed");
		}

		private void ControllerChanged(FreeControllerV3 fc)
		{
			if (mc_ == null)
				return;

			mc_.ControllerName = fc?.name ?? "";
			Update();
		}

		private void AtomChanged(Atom a)
		{
			if (mc_ == null)
				return;

			mc_.AtomName = a?.uid ?? "";
			Update();
		}

		private void RigidbodyChanged(Rigidbody rb)
		{
			if (mc_ == null)
				return;

			mc_.RigidbodyName = rb?.name ?? "";
			Update();
		}

		private void PositionChanged(int i)
		{
			if (mc_ == null)
				return;

			mc_.Position = i;
		}

		private void RotationChanged(int i)
		{
			if (mc_ == null)
				return;

			mc_.Rotation = i;
		}

		private void Update()
		{
			controller_.Atom = modifier_.Atom;
			controller_.Value = mc_.ControllerName;
			atom_.Value = mc_.AtomName;
			rb_.Atom = mc_.Atom;
			rb_.Value = mc_.RigidbodyName;
			position_.Value = mc_.Position;
			rotation_.Value = mc_.Rotation;
			collapsible_.Text = mc_.Name;
		}
	}


	class LinkModifierUI : AtomModifierUI
	{
		private LinkModifier modifier_ = null;

		private readonly Button add_;
		private List<LinkModifierControllerUI> controllers_ =
			new List<LinkModifierControllerUI>();

		public override string ModifierType
		{
			get { return LinkModifier.FactoryTypeName; }
		}


		public LinkModifierUI(MainUI ui)
			: base(ui)
		{
			add_ = new Button("Add target", Add, Widget.Right);
		}

		public override void AddToTopUI(IModifier m)
		{
			var changed = (m != modifier_);

			modifier_ = m as LinkModifier;
			if (modifier_ == null)
				return;

			AddAtomWidgets(m);
			base.AddToTopUI(m);

			if (modifier_.Controllers.Count != controllers_.Count)
				changed = true;

			if (changed)
			{
				controllers_.Clear();

				foreach (var c in modifier_.Controllers)
					controllers_.Add(new LinkModifierControllerUI(modifier_, c));
			}

			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			widgets_.AddToUI(add_);

			if (controllers_.Count > 0)
			{
				widgets_.AddToUI(new SmallSpacer(Widget.Right));

				foreach (var c in controllers_)
					widgets_.AddToUI(c.Collapsible);

				widgets_.AddToUI(new LargeSpacer(Widget.Right));
				widgets_.AddToUI(new LargeSpacer(Widget.Right));
			}
		}

		private void Add()
		{
			if (modifier_ == null)
				return;

			var mc = new LinkModifierController();
			modifier_.AddController(mc);
			controllers_.Add(new LinkModifierControllerUI(modifier_, mc));

			Synergy.Instance.UI.NeedsReset("link target added");
		}
	}
}
