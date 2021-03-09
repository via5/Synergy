using System.Collections.Generic;
using UnityEngine;
using UI = SynergyUI;

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

		private readonly DirectionPanel dir_ = new DirectionPanel();
		private readonly MovementUI movement_ = new MovementUI();

		private RigidbodyModifier modifier_ = null;
		private IgnoreFlag ignore_ = new IgnoreFlag();


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
			Add(w);
			Add(dir_);
			Add(movement_);

			atom_.AtomSelectionChanged += OnAtomChanged;
			receiver_.RigidbodySelectionChanged += OnRigidbodyChanged;
			movementType_.FactoryTypeChanged += OnMovementTypeChanged;
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

			ignore_.Do(() =>
			{
				atom_.Select(modifier_.Atom);
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
				movementType_.Select(modifier_.Type);
				dir_.Set(modifier_.Direction);
				movement_.Set(modifier_.Movement);
			});
		}

		private void OnAtomChanged(Atom a)
		{
			if (ignore_)
				return;

			modifier_.Atom = a;

			ignore_.Do(() =>
			{
				receiver_.Set(modifier_.Atom, modifier_.Receiver);
			});
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

		private void OnDirectionChanged(Vector3 v)
		{
			modifier_.Direction = v;
		}
	}


	class RigidBodyComboBox : UI.Panel
	{
		public delegate void RigidbodyCallback(Rigidbody atom);
		public event RigidbodyCallback RigidbodySelectionChanged;

		private readonly UI.ComboBox<string> cb_;
		private readonly GotoButton goto_;

		private Atom atom_ = null;
		private bool dirty_ = false;

		public RigidBodyComboBox()
		{
			cb_ = new UI.ComboBox<string>();
			goto_ = new GotoButton(OnGoto);

			Layout = new UI.BorderLayout(5);
			Add(cb_, UI.BorderLayout.Center);
			Add(goto_, UI.BorderLayout.Right);

			UpdateList(null);

			cb_.SelectionChanged += (string uid) =>
			{
				goto_.Enabled = !string.IsNullOrEmpty(uid);
				RigidbodySelectionChanged?.Invoke(SelectedRigidbody);
			};

			cb_.Filterable = true;
			cb_.Opened += OnOpen;
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
				var name = cb_.Selected;
				if (string.IsNullOrEmpty(name))
					return null;

				if (atom_ == null)
					return null;

				return Utilities.FindRigidbody(atom_, name);
			}
		}

		public void Select(Rigidbody rb)
		{
			cb_.Select(rb?.name);
		}

		private void OnGoto()
		{
			if (atom_ == null)
				return;

			var rb = SelectedRigidbody;
			if (rb == null)
				return;

			SuperController.singleton.SelectController(
				atom_.uid, rb.name + "Control");
		}

		private void OnOpen()
		{
			if (dirty_)
			{
				UpdateList(cb_.Selected);
				dirty_ = false;
			}
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
			cb_.SetItems(list, sel);
		}
	}
}
