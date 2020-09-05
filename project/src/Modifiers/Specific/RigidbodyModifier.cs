using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

namespace Synergy
{
	sealed class RigidbodyMovementTypeFactory :
		BasicFactory<IRigidbodyMovementType>
	{
		public override List<IRigidbodyMovementType> GetAllObjects()
		{
			return new List<IRigidbodyMovementType>()
			{
				new RelativeForceMovementType(),
				new RelativeTorqueMovementType(),
				new ForceMovementType(),
				new TorqueMovementType()
			};
		}
	}


	interface IRigidbodyMovementType : IFactoryObject
	{
		IRigidbodyMovementType Clone(int cloneFlags = 0);

		void Set(Rigidbody receiver, Vector3 magnitude);
		string ShortName { get; }
	}

	abstract class BasicRigidbodyMovementType : IRigidbodyMovementType
	{
		public abstract IRigidbodyMovementType Clone(int cloneFlags = 0);

		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();
		public abstract string ShortName { get; }

		public J.Node ToJSON()
		{
			return new J.Object();
		}

		public bool FromJSON(J.Node n)
		{
			return true;
		}

		public abstract void Set(Rigidbody receiver, Vector3 magnitude);
	}

	sealed class ForceMovementType : BasicRigidbodyMovementType
	{
		public static string FactoryTypeName { get; } = "force";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Force";
		public override string GetDisplayName() { return DisplayName; }

		public override string ShortName { get { return "F"; } }

		public override IRigidbodyMovementType Clone(int cloneFlags = 0)
		{
			return new ForceMovementType();
		}

		public override void Set(Rigidbody receiver, Vector3 magnitude)
		{
			receiver.AddForce(magnitude);
		}
	}

	sealed class RelativeForceMovementType : BasicRigidbodyMovementType
	{
		public static string FactoryTypeName { get; } = "relativeForce";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Relative force";
		public override string GetDisplayName() { return DisplayName; }

		public override string ShortName { get { return "RF"; } }

		public override IRigidbodyMovementType Clone(int cloneFlags = 0)
		{
			return new RelativeForceMovementType();
		}

		public override void Set(Rigidbody receiver, Vector3 magnitude)
		{
			receiver.AddRelativeForce(magnitude);
		}
	}

	sealed class TorqueMovementType : BasicRigidbodyMovementType
	{
		public static string FactoryTypeName { get; } = "torque";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Torque";
		public override string GetDisplayName() { return DisplayName; }

		public override string ShortName { get { return "T"; } }

		public override IRigidbodyMovementType Clone(int cloneFlags = 0)
		{
			return new TorqueMovementType();
		}

		public override void Set(Rigidbody receiver, Vector3 magnitude)
		{
			receiver.AddTorque(magnitude);
		}
	}

	sealed class RelativeTorqueMovementType : BasicRigidbodyMovementType
	{
		public static string FactoryTypeName { get; } = "relativeTorque";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Relative torque";
		public override string GetDisplayName() { return DisplayName; }

		public override string ShortName { get { return "RT"; } }

		public override IRigidbodyMovementType Clone(int cloneFlags = 0)
		{
			return new RelativeTorqueMovementType();
		}

		public override void Set(Rigidbody receiver, Vector3 magnitude)
		{
			receiver.AddRelativeTorque(magnitude);
		}
	}


	sealed class RigidbodyModifier : AtomWithMovementModifier
	{
		private const float NoMagnitude = float.MinValue;

		private IRigidbodyMovementType type_ = new RelativeForceMovementType();
		private Rigidbody receiver_ = null;
		private Vector3 direction_ = new Vector3(1, 0, 0);
		private float magnitude_ = NoMagnitude;

		public RigidbodyModifier()
		{
			if (!Utilities.AtomHasRigidbodies(Atom))
				Atom = null;
		}

		public RigidbodyModifier(Atom atom, Rigidbody rb)
		{
			Atom = atom;
			receiver_ = rb;
		}

		public RigidbodyModifier(Atom atom, string rigidbodyName)
		{
			Atom = atom;
			receiver_ = Utilities.FindRigidbody(atom, rigidbodyName);
		}

		public static string FactoryTypeName { get; } = "rigidbody";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Rigidbody";
		public override string GetDisplayName() { return DisplayName; }

		public IRigidbodyMovementType Type
		{
			get
			{
				return type_;
			}

			set
			{
				type_ = value;
				FireNameChanged();
			}
		}

		public Rigidbody Receiver
		{
			get
			{
				return receiver_;
			}

			set
			{
				receiver_ = value;
				FireNameChanged();
			}
		}

		public Vector3 Direction
		{
			get { return direction_; }
			set { direction_ = value; }
		}

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(-500, 500);
			}
		}

		public float RealMagnitude
		{
			get { return magnitude_; }
		}


		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new RigidbodyModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(RigidbodyModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);
			m.type_ = type_?.Clone(cloneFlags);
			m.receiver_ = receiver_;
			m.direction_ = direction_;
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (Receiver != null)
			{
				if (magnitude_ == NoMagnitude)
					type_.Set(Receiver, new Vector3(0, 0, 0));
				else
					type_.Set(Receiver, direction_ * magnitude_);
			}
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			magnitude_ = Movement.Magnitude;
		}

		protected override void DoTickPaused(float deltaTime)
		{
			base.DoTickPaused(deltaTime);
			magnitude_ = NoMagnitude;
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();

			if (Atom == null)
			{
				Receiver = null;
			}
			else if (receiver_ != null)
			{
				var oldName = receiver_.name;
				Receiver = Utilities.FindRigidbody(Atom, oldName);
			}
		}

		protected override string MakeName()
		{
			string n = type_.ShortName + " ";

			if (Atom == null && Receiver == null)
			{
				n += "none";
			}
			else
			{
				if (Atom == null)
					n += "none";
				else
					n += Atom.name;

				n += " ";

				if (Receiver == null)
					n += "none";
				else
					n += Receiver.name;

				var dirString = Utilities.DirectionString(Direction);
				if (dirString != "")
					n += " " + dirString;
			}

			return n;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("type", type_);

			if (Receiver != null)
				o.Add("receiver", Receiver.name);

			o.Add("direction", J.Wrappers.ToJSON(direction_));

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("RigidbodyModifier");
			if (o == null)
				return false;

			o.Opt<RigidbodyMovementTypeFactory, IRigidbodyMovementType>(
				"type", ref type_);

			o.OptRigidbody("receiver", Atom, ref receiver_);

			if (o.HasKey("direction"))
				J.Wrappers.FromJSON(o.Get("direction"), ref direction_);

			return true;
		}
	}
}
