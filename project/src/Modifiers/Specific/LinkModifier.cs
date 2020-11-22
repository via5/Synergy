using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class LinkModifierController
	{
		private string controllerName_ = "";
		private string rbName_ = "";
		private int position_ = -1;
		private int rotation_ = -1;

		private FreeControllerV3 controller_ = null;
		private Rigidbody rb_ = null;
		private bool logged_ = false;

		public LinkModifierController()
		{
		}

		public LinkModifierController(
			string controllerName, string rbName, int position, int rotation)
		{
			controllerName_ = controllerName;
			rbName_ = rbName;
			position_ = position;
			rotation_ = rotation;
		}

		public LinkModifierController Clone(int cloneFlags)
		{
			var c = new LinkModifierController();
			CopyTo(c, cloneFlags);
			return c;
		}

		private void CopyTo(LinkModifierController c, int cloneFlags)
		{
			c.controllerName_ = controllerName_;
			c.rbName_ = rbName_;
			c.position_ = position_;
			c.rotation_ = rotation_;
		}

		public void AtomChanged()
		{
			controller_ = null;
			rb_ = null;
		}

		public string ControllerName
		{
			get
			{
				return controllerName_;
			}

			set
			{
				if (controllerName_ != value)
				{
					controllerName_ = value;
					controller_ = null;
					logged_ = false;
				}
			}
		}

		public string RigidbodyName
		{
			get
			{
				return rbName_;
			}

			set
			{
				if (rbName_ != value)
				{
					rbName_ = value;
					rb_ = null;
					logged_ = false;
				}
			}
		}

		public int Position
		{
			get
			{
				return position_;
			}

			set
			{
				position_ = value;
			}
		}

		public int Rotation
		{
			get
			{
				return rotation_;
			}

			set
			{
				rotation_ = value;
			}
		}

		public void Set(Atom atom)
		{
			UpdateController(atom);
			UpdateRigidbody(atom);

			if (controller_ != null)
			{
				if (rb_ != null)
				{
					if (controller_.linkToRB != rb_)
						controller_.SelectLinkToRigidbody(rb_);
				}

				if (position_ != -1)
				{
					controller_.currentPositionState =
						(FreeControllerV3.PositionState)position_;
				}

				if (rotation_ != -1)
				{
					controller_.currentRotationState =
						(FreeControllerV3.RotationState)rotation_;
				}
			}
		}

		private void UpdateController(Atom atom)
		{
			if (controller_ == null && controllerName_ != "")
			{
				controller_ = Utilities.FindFreeController(
					atom, controllerName_);

				if (controller_ == null && !logged_)
				{
					Synergy.LogError(
						$"cannot find controller {controllerName_} in atom " +
						$"{atom.uid}");

					logged_ = true;
				}
			}
		}

		private void UpdateRigidbody(Atom atom)
		{
			if (rb_ == null && rbName_ != "")
			{
				rb_ = Utilities.FindRigidbody(atom, rbName_);

				if (rb_ == null && !logged_)
				{
					Synergy.LogError(
						$"cannot find rigidbody {rbName_} in atom " +
						$"{atom.uid}");

					logged_ = true;
				}
			}
		}
	}

	sealed class LinkModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "link";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Parent Link";
		public override string GetDisplayName() { return DisplayName; }

		private readonly List<LinkModifierController> controllers_ =
			new List<LinkModifierController>();

		private bool lastFirstHalf_ = false;


		public LinkModifier(Atom a=null)
		{
			Atom = a;
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new LinkModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(LinkModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);

			m.controllers_.Clear();
			foreach (var c in controllers_)
				m.controllers_.Add(c.Clone(cloneFlags));
		}

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange();
			}
		}

		public void AddController(LinkModifierController c)
		{
			controllers_.Add(c);
		}

		public void RemoveController(LinkModifierController c)
		{
			controllers_.Remove(c);
		}

		protected override void DoTick(float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);

			if (firstHalf != lastFirstHalf_)
			{
				lastFirstHalf_ = firstHalf;

				if (Atom != null)
				{
					foreach (var c in controllers_)
						c.Set(Atom);
				}
			}
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (paused)
				return;
		}

		protected override string MakeName()
		{
			string s = "LK ";

			if (Atom == null)
				s += "none";
			else
				s += Atom.name;

			return s;
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();

			foreach (var c in controllers_)
				c.AtomChanged();
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("LinkModifier");
			if (o == null)
				return false;

			return true;
		}
	}
}
