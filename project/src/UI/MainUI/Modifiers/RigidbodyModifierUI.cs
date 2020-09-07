using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	using RigidbodyMovementTypeStringList = FactoryStringList<
		RigidbodyMovementTypeFactory, IRigidbodyMovementType>;


	class Vector3UI
	{
		public delegate void Callback(Vector3 v);
		public event Callback Changed;

		private readonly FloatSlider x_, y_, z_;

		public Vector3UI(string name, int flags, FloatRange range, Callback changed)
		{
			Changed = changed;

			x_ = new FloatSlider(
				MakeCaption(name, "X"), 0, new FloatRange(range),
				OnChanged, flags);

			y_ = new FloatSlider(
				MakeCaption(name, "Y"), 0, new FloatRange(range),
				OnChanged, flags);

			z_ = new FloatSlider(
				MakeCaption(name, "Z"), 0, new FloatRange(range),
				OnChanged, flags);
		}

		private string MakeCaption(string name, string more)
		{
			if (name == "")
				return more;
			else
				return name + " " + more;
		}

		public Vector3 Value
		{
			get
			{
				return new Vector3(x_.Value, y_.Value, z_.Value);
			}

			set
			{
				x_.Value = value.x;
				y_.Value = value.y;
				z_.Value = value.z;
			}
		}

		public List<Widget> GetWidgets()
		{
			return new List<Widget>()
			{
				x_, y_, z_
			};
		}

		private void OnChanged(float f)
		{
			Changed(Value);
		}
	}


	class RigidbodyModifierUI : AtomWithMovementUI
	{
		public override string ModifierType
		{
			get { return RigidbodyModifier.FactoryTypeName; }
		}

		private RigidbodyModifier modifier_ = null;
		private readonly ForceReceiverList receiver_;
		private readonly RigidbodyMovementTypeStringList moveType_;
		private readonly StringList dirType_;
		private readonly Vector3UI dir_;

		public RigidbodyModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasForceReceivers)
		{
			receiver_ = new ForceReceiverList(
				"Receiver", "", ReceiverChanged, Widget.Right);

			moveType_ = new RigidbodyMovementTypeStringList(
				"Move type", "", MoveTypeChanged, Widget.Right);

			dirType_ = new StringList(
				"Direction", "X",
				new List<string>() { "X", "Y", "Z", "Custom" },
				MoveDirectionChanged, Widget.Right);

			dir_ = new Vector3UI(
				"", Widget.Right | Widget.Constrained, new FloatRange(-1, 1),
				MoveCustomDirectionChanged);
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
			dir_.Value = modifier_.Direction;

			AddAtomWidgets(m);

			widgets_.AddToUI(receiver_);
			widgets_.AddToUI(moveType_);
			widgets_.AddToUI(dirType_);

			if (dirType_.Value == "Custom")
			{
				foreach (var w in dir_.GetWidgets())
					widgets_.AddToUI(w);
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

		void MoveCustomDirectionChanged(Vector3 v)
		{
			if (modifier_ != null)
				modifier_.Direction = v;
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
