using LeapInternal;
using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class EyeTargetFactory : BasicFactory<IEyeTarget>
	{
		public override List<IEyeTarget> GetAllObjects()
		{
			return new List<IEyeTarget>()
			{
				new RigidbodyEyeTarget(),
				new ConstantEyeTarget(),
				new RandomEyeTarget()
			};
		}
	}


	interface IEyeTarget : IFactoryObject
	{
		IEyeTarget Clone(int cloneFlags);
		Vector3 Position { get; }
		void Update(Rigidbody head);
	}

	abstract class BasicEyeTarget : IEyeTarget
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract IEyeTarget Clone(int cloneFlags);
		public abstract Vector3 Position { get; }
		public abstract void Update(Rigidbody head);

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}
	}

	class RigidbodyEyeTarget : BasicEyeTarget
	{
		public override string GetFactoryTypeName() { return "rigidbody"; }
		public override string GetDisplayName() { return "Rigidbody"; }

		private Atom atom_ = null;
		private Rigidbody receiver_ = null;
		private Vector3 pos_ = new Vector3();

		public RigidbodyEyeTarget()
			: this(null, null)
		{
		}

		public RigidbodyEyeTarget(Atom a, Rigidbody rb)
		{
			atom_ = a;
			receiver_ = rb;
		}

		public override IEyeTarget Clone(int cloneFlags)
		{
			var t = new RigidbodyEyeTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(RigidbodyEyeTarget t, int cloneFlags)
		{
			t.receiver_ = receiver_;
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override void Update(Rigidbody head)
		{
			if (receiver_ == null)
				return;

			pos_ = receiver_.position;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (receiver_ != null)
				o.Add("receiver", receiver_.name);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("RigidbodyEyeTarget");
			if (o == null)
				return false;

			o.OptForceReceiver("receiver", atom_, ref receiver_);

			return true;
		}
	}

	class ConstantEyeTarget : BasicEyeTarget
	{
		public override string GetFactoryTypeName() { return "constant"; }
		public override string GetDisplayName() { return "Constant"; }

		private Vector3 offset_ = new Vector2();
		private Atom atom_ = null;
		private Rigidbody rel_ = null;

		private Vector3 pos_ = new Vector3();


		public ConstantEyeTarget()
			: this(new Vector3(), null)
		{
		}

		public ConstantEyeTarget(Vector3 offset, Rigidbody rel)
		{
			offset_ = offset;
			rel_ = rel;
		}

		public override IEyeTarget Clone(int cloneFlags)
		{
			var t = new ConstantEyeTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(ConstantEyeTarget t, int cloneFlags)
		{
			t.offset_ = offset_;
			t.rel_ = rel_;
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override void Update(Rigidbody head)
		{
			if (rel_ == null)
				pos_ = head.position + offset_;
			else
				pos_ = head.position + rel_.rotation * offset_;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("offset", J.Wrappers.ToJSON(offset_));

			if (rel_ != null)
				o.Add("relative", rel_.name);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ConstantEyeTarget");
			if (o == null)
				return false;

			if (o.HasKey("offset"))
				J.Wrappers.FromJSON(o.Get("offset"), ref offset_);

			o.OptForceReceiver("relative", atom_, ref rel_);

			return true;
		}
	}

	class RandomEyeTarget : BasicEyeTarget
	{
		public override string GetFactoryTypeName() { return "random"; }
		public override string GetDisplayName() { return "Random"; }

		private float distance_ = 1.0f;
		private float centerX_ = 0;
		private float centerY_ = 0;
		private float xRange_ = 2;
		private float yRange_ = 2;
		private float avoidXRange_ = 1;
		private float avoidYRange_ = 1;

		private Vector3 pos_ = new Vector3();


		public override IEyeTarget Clone(int cloneFlags)
		{
			var t = new RandomEyeTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(RandomEyeTarget t, int cloneFlags)
		{
			t.distance_ = distance_;
			t.centerX_ = centerX_;
			t.centerY_ = centerY_;
			t.xRange_ = xRange_;
			t.yRange_ = yRange_;
			t.avoidXRange_ = avoidXRange_;
			t.avoidYRange_ = avoidYRange_;
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override void Update(Rigidbody head)
		{
			Vector3 fwd = head.rotation * Vector3.forward;
			Vector3 ver = head.rotation * Vector3.up;
			Vector3 hor = head.rotation * Vector3.right;

			var xRange = xRange_ - avoidXRange_;
			var yRange = yRange_ - avoidYRange_;

			var x = UnityEngine.Random.Range(
				centerX_ - xRange,
				centerX_ + xRange);

			var y = UnityEngine.Random.Range(
				centerY_ - yRange,
				centerY_ + yRange);

			if (x < centerX_)
				x -= avoidXRange_;
			else
				x += avoidXRange_;

			if (y < centerY_)
				y -= avoidYRange_;
			else
				y += avoidYRange_;


			pos_ = head.position +
				fwd * distance_ +
				ver * y + hor * x;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("distance", distance_);
			o.Add("xCenter", centerX_);
			o.Add("yCenter", centerY_);
			o.Add("xRange", xRange_);
			o.Add("yRange", yRange_);
			o.Add("avoidXRange_", avoidXRange_);
			o.Add("avoidYRange_", avoidYRange_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("RandomEyeTarget");
			if (o == null)
				return false;

			o.Opt("distance", ref distance_);
			o.Opt("xCenter", ref centerX_);
			o.Opt("yCenter", ref centerY_);
			o.Opt("xRange", ref xRange_);
			o.Opt("yRange", ref yRange_);
			o.Opt("avoidXRange_", ref avoidXRange_);
			o.Opt("avoidYRange_", ref avoidYRange_);

			return true;
		}
	}


	sealed class EyesModifier : AtomModifier
	{
		public static string FactoryTypeName { get; } = "eyes";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Eyes";
		public override string GetDisplayName() { return DisplayName; }


		private Rigidbody head_ = null;
		private Rigidbody eyes_ = null;

		private List<IEyeTarget> targets_ = new List<IEyeTarget>();

		private RandomizableTime saccadeTime_;
		private float saccadeMin_ = 0.005f;
		private float saccadeMax_ = 0.02f;

		private float minDistance_ = 0.5f;

		private int current_ = -1;
		private float lastProgress_ = -1;
		private Vector3 saccadeOffset_ = new Vector3();


		public EyesModifier()
		{
			saccadeTime_ = new RandomizableTime(1, 0.2f, 0);

			UpdateAtom();
		}

		public override IModifier Clone(int cloneFlags = 0)
		{
			var m = new EyesModifier();
			CopyTo(m, cloneFlags);
			return m;
		}

		private void CopyTo(EyesModifier m, int cloneFlags)
		{
			base.CopyTo(m, cloneFlags);

			m.head_ = head_;
			m.eyes_ = eyes_;

			m.targets_.Clear();
			foreach (var t in targets_)
				m.targets_.Add(t.Clone(cloneFlags));

			m.saccadeTime_ = saccadeTime_.Clone(cloneFlags);
			m.saccadeMin_ = saccadeMin_;
			m.saccadeMax_ = saccadeMax_;
			m.saccadeOffset_ = saccadeOffset_;

			m.minDistance_ = minDistance_;
		}

		public override void Removed()
		{
			base.Removed();
		}

		public override FloatRange PreferredRange
		{
			get
			{
				return new FloatRange(0, 1);
			}
		}

		public void AddTarget(IEyeTarget t)
		{
			targets_.Add(t);
		}

		public override void Reset()
		{
			base.Reset();
			Current = -1;
			saccadeTime_.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			if (head_ == null || eyes_ == null)
				return;


			saccadeTime_.Tick(deltaTime);
			if (saccadeTime_.Finished)
			{
				saccadeTime_.Reset();

				saccadeOffset_.x = UnityEngine.Random.Range(
					saccadeMin_, saccadeMax_);

				saccadeOffset_.y = UnityEngine.Random.Range(
					saccadeMin_, saccadeMax_);

				saccadeOffset_.z = UnityEngine.Random.Range(
					saccadeMin_, saccadeMax_);
			}


			if (firstHalf)
				progress /= 2;
			else
				progress = progress / 2 + 0.5f;


			if (progress < lastProgress_ )
				Current = -1;

			lastProgress_ = progress;

			float progressOnTarget = 1.0f / targets_.Count;
			float p = 0;

			for (int i = 0; i < targets_.Count; ++i)
			{
				if (progress >= p && progress <= (p + progressOnTarget))
				{
					Current = i;
					break;
				}

				p += progressOnTarget;
			}
		}

		public int Current
		{
			get
			{
				return current_;
			}

			set
			{
				if (current_ != value)
				{
					current_ = value;
					TargetChanged();
				}
			}
		}

		private void TargetChanged()
		{
			if (current_ < 0 || current_ >= targets_.Count)
				return;

			targets_[current_].Update(head_);
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (eyes_ == null || head_ == null || current_ == -1)
				return;

			var pos = targets_[current_].Position + saccadeOffset_;

			var distanceToTarget = Vector3.Distance(head_.position, pos);
			if (distanceToTarget < minDistance_)
			{
				var add = minDistance_ - distanceToTarget;
				var dir = (pos - head_.position).normalized;

				pos += (dir * add);
			}


			eyes_.position = pos;
		}

		protected override string MakeName()
		{
			return "EY";
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();
			UpdateAtom();
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.AddFactoryObjects("targets", targets_);

			o.Add("saccadeTime", saccadeTime_);
			o.Add("saccadeMin", saccadeMin_);
			o.Add("saccadeMax", saccadeMax_);
			o.Add("minDistance", minDistance_);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("EyesModifier");
			if (o == null)
				return false;

			o.OptFactoryObjects<EyeTargetFactory, IEyeTarget>(
				"targets", ref targets_);

			o.Opt("saccadeTime", ref saccadeTime_);
			o.Opt("saccadeMin", ref saccadeMin_);
			o.Opt("saccadeMax", ref saccadeMax_);
			o.Opt("minDistance", ref minDistance_);

			return true;
		}

		private void UpdateAtom()
		{
			if (Atom == null)
			{
				head_ = null;
				eyes_ = null;
				return;
			}

			head_ = Utilities.FindRigidbody(Atom, "headControl");
			eyes_ = Utilities.FindRigidbody(Atom, "eyeTargetControl");

			if (head_ != null && eyes_ != null)
				return;

			if (head_ == null)
				Synergy.LogError("atom " + Atom.uid + " has no head");

			if (eyes_ == null)
				Synergy.LogError("atom " + Atom.uid + " has no eyes");

			head_ = null;
			eyes_ = null;
		}
	}
}
