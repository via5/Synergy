using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	using RigidbodyMovementTypeStringList = FactoryStringList<
		RigidbodyMovementTypeFactory, IRigidbodyMovementType>;

	class RigidbodyModifierUI : AtomWithMovementUI
	{
		public override string ModifierType
		{
			get { return RigidbodyModifier.FactoryTypeName; }
		}

		private RigidbodyModifier modifier_ = null;
		private readonly RigidbodyList receiver_;
		private readonly RigidbodyMovementTypeStringList moveType_;
		private readonly StringList dirType_;
		private readonly FloatSlider dirX_, dirY_, dirZ_;

		public RigidbodyModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasForceReceivers)
		{
			receiver_ = new RigidbodyList(
				"Receiver", "", ReceiverChanged, Widget.Right);

			moveType_ = new RigidbodyMovementTypeStringList(
				"Move type", "", MoveTypeChanged, Widget.Right);

			dirType_ = new StringList(
				"Direction", "X",
				new List<string>() { "X", "Y", "Z", "Custom" },
				MoveDirectionChanged, Widget.Right);

			dirX_ = new FloatSlider(
				"X", 0, new FloatRange(-1, 1),
				MoveCustomDirectionChanged,
				Widget.Constrained | Widget.Right);

			dirY_ = new FloatSlider(
				"Y", 0, new FloatRange(-1, 1),
				MoveCustomDirectionChanged,
				Widget.Constrained | Widget.Right);

			dirZ_ = new FloatSlider(
				"Z", 0, new FloatRange(-1, 1),
				MoveCustomDirectionChanged,
				Widget.Constrained | Widget.Right);
		}

		public override void AddToTopUI(IModifier m)
		{
			modifier_ = m as RigidbodyModifier;
			if (modifier_ == null)
				return;

			if (modifier_.Receiver == null)
				receiver_.Value = "";
			else
				receiver_.Value = modifier_.Receiver.name;

			receiver_.Atom = modifier_.Atom;
			moveType_.Value = modifier_.Type;

			var dirString = Utilities.DirectionString(modifier_.Direction);
			if (dirString == "")
				dirString = "Custom";

			dirType_.Value = dirString;

			dirX_.Value = modifier_.Direction.x;
			dirY_.Value = modifier_.Direction.y;
			dirZ_.Value = modifier_.Direction.z;

			AddAtomWidgets(m);

			widgets_.AddToUI(receiver_);
			widgets_.AddToUI(moveType_);
			widgets_.AddToUI(dirType_);

			if (dirType_.Value == "Custom")
			{
				widgets_.AddToUI(dirX_);
				widgets_.AddToUI(dirY_);
				widgets_.AddToUI(dirZ_);
			}

			AddAtomWithMovementWidgets(m);
			base.AddToTopUI(modifier_);
		}

		private void MoveTypeChanged(IRigidbodyMovementType t)
		{
			if (t != null && modifier_ != null)
				modifier_.Type = t;
		}

		private void ReceiverChanged(Rigidbody rb)
		{
			if (modifier_ != null)
				modifier_.Receiver = rb;
		}

		private void MoveDirectionChanged(string s)
		{
			if (modifier_ == null)
				return;

			if (s == "X")
				modifier_.Direction = new Vector3(1, 0, 0);
			else if (s == "Y")
				modifier_.Direction = new Vector3(0, 1, 0);
			else if (s == "Z")
				modifier_.Direction = new Vector3(0, 0, 1);
			else
				modifier_.Direction = new Vector3(0, 0, 0);

			ui_.NeedsReset("move direction changed");
		}

		void MoveCustomDirectionChanged(float dummy)
		{
			if (modifier_ != null)
			{
				modifier_.Direction = new Vector3(
					dirX_.Value, dirY_.Value, dirZ_.Value);
			}
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			receiver_.Atom = atom;

			if (modifier_.Receiver == null)
				receiver_.Value = "";
			else
				receiver_.Value = modifier_.Receiver.name;
		}
	}
}
