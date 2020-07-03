using System.Collections.Generic;
using UnityEngine;

namespace Synergy.NewUI
{
	class RigidbodyModifierPanel : BasicModifierPanel
	{
		private readonly AtomComboBox atom_ = new AtomComboBox(
			Utilities.AtomHasRigidbodies);

		private readonly RigidBodyComboBox receiver_ =
			new RigidBodyComboBox();

		private readonly FactoryComboBox<
			RigidbodyMovementTypeFactory, IRigidbodyMovementType>
				movementType_ = new FactoryComboBox<
					RigidbodyMovementTypeFactory, IRigidbodyMovementType>();

		private readonly FactoryComboBox<EasingFactory, IEasing> easing_ =
			new FactoryComboBox<EasingFactory, IEasing>();

		private readonly DirectionPanel dir_ = new DirectionPanel();

		private readonly MovementPanel min_ = new MovementPanel(S("Minimum"));
		private readonly MovementPanel max_ = new MovementPanel(S("Maximum"));

		private RigidbodyModifier modifier_ = null;
		private bool ignore_ = false;


		public RigidbodyModifierPanel()
		{
			Layout = new UI.VerticalFlow(30);

			var w = new UI.Panel();
			var gl = new UI.GridLayout(4);
			gl.HorizontalSpacing = 20;
			gl.VerticalSpacing = 20;
			gl.HorizontalStretch = new List<bool>() { false, true, false, true };
			w.Layout = gl;

			w.Add(new UI.Label(S("Atom")));
			w.Add(atom_);
			w.Add(new UI.Label(S("Receiver")));
			w.Add(receiver_);
			w.Add(new UI.Label(S("Move type")));
			w.Add(movementType_);
			w.Add(new UI.Label(S("Easing")));
			w.Add(easing_);
			Add(w);
			Add(dir_);

			Add(min_);
			Add(max_);

			atom_.AtomSelectionChanged += OnAtomChanged;
			receiver_.RigidbodySelectionChanged += OnRigidbodyChanged;
			movementType_.FactoryTypeChanged += OnMovementTypeChanged;
			easing_.FactoryTypeChanged += OnEasingChanged;
			dir_.Changed += OnDirectionChanged;
		}

		public override string Title
		{
			get { return S("Rigidbody"); }
		}

		public override bool Accepts(IModifier m)
		{
			return m is RigidbodyModifier;
		}

		public override void Set(IModifier m)
		{
			modifier_ = m as RigidbodyModifier;

			using (new ScopedFlag((b) => ignore_ = b))
			{
				atom_.Select(modifier_.Atom);
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
				movementType_.Select(modifier_.Type);
				easing_.Select(modifier_.Movement.Easing);
				dir_.Set(modifier_.Direction);
				min_.Set(modifier_.Movement.Minimum);
				max_.Set(modifier_.Movement.Maximum);
			}
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;
			modifier_.Receiver = Utilities.FindRigidbody(
				a, receiver_.Selected);

			using (new ScopedFlag((b) => ignore_ = b))
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
		}

		private void OnRigidbodyChanged(Rigidbody rb)
		{
			if (ignore_)
				return;

			modifier_.Receiver = rb;
		}

		private void OnMovementTypeChanged(IRigidbodyMovementType type)
		{
			if (ignore_)
				return;

			modifier_.Type = type;
		}

		private void OnEasingChanged(IEasing easing)
		{
			if (ignore_)
				return;

			modifier_.Movement.Easing = easing;
		}

		private void OnDirectionChanged(Vector3 v)
		{
			modifier_.Direction = v;
		}
	}


	class RigidBodyComboBox : UI.ComboBox<string>
	{
		public delegate void RigidbodyCallback(Rigidbody atom);
		public event RigidbodyCallback RigidbodySelectionChanged;

		private Atom atom_ = null;
		private bool dirty_ = false;

		public RigidBodyComboBox()
		{
			UpdateList(null);

			SelectionChanged += (string uid) =>
			{
				RigidbodySelectionChanged?.Invoke(SelectedRigidbody);
			};
		}

		public void Set(Atom atom, Rigidbody rb)
		{
			atom_ = atom;
			UpdateList(rb?.name);
		}

		public Rigidbody SelectedRigidbody
		{
			get
			{
				var name = Selected;
				if (string.IsNullOrEmpty(name))
					return null;

				if (atom_ == null)
					return null;

				return Utilities.FindRigidbody(atom_, name);
			}
		}

		public void Select(Rigidbody rb)
		{
			Select(rb?.name);
		}

		protected override void OnOpen()
		{
			if (dirty_)
			{
				UpdateList(Selected);
				dirty_ = false;
			}

			base.OnOpen();
		}

		private void UpdateList(string sel)
		{
			var list = new List<string>();

			list.Add(null);

			if (atom_ != null)
			{
				foreach (var fr in atom_.forceReceivers)
				{
					var rb = fr.GetComponent<Rigidbody>();
					if (rb != null)
						list.Add(rb.name);
				}
			}

			list.Sort();
			SetItems(list, sel);
		}
	}
}
