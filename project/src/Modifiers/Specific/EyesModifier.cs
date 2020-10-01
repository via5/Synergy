using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	class EyesTargetFactory : BasicFactory<IEyesTarget>
	{
		public override List<IEyesTarget> GetAllObjects()
		{
			return new List<IEyesTarget>()
			{
				new RigidbodyEyesTarget(),
				new ConstantEyesTarget(),
				new RandomEyesTarget()
			};
		}
	}


	interface IEyesTarget : IFactoryObject
	{
		IEyesTarget Clone(int cloneFlags);
		Vector3 Position { get; }
		void Update(Rigidbody head, Rigidbody chest);
		string Name { get; }
	}

	abstract class BasicEyesTarget : IEyesTarget
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract IEyesTarget Clone(int cloneFlags);
		public abstract Vector3 Position { get; }
		public abstract void Update(Rigidbody head, Rigidbody chest);

		public abstract string Name { get; }

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}
	}

	class RigidbodyEyesTarget : BasicEyesTarget
	{
		public override string GetFactoryTypeName() { return "rigidbody"; }
		public override string GetDisplayName() { return "Rigidbody"; }

		private Atom atom_ = null;
		private Rigidbody receiver_ = null;
		private Vector3 offset_ = new Vector3();
		private Vector3 pos_ = new Vector3();

		public RigidbodyEyesTarget()
			: this(null, null)
		{
		}

		public RigidbodyEyesTarget(Atom a, Rigidbody rb)
		{
			atom_ = a;
			receiver_ = rb;
		}

		public static Rigidbody GetPreferredTarget(Atom a)
		{
			var head = Utilities.FindRigidbody(a, "head");
			if (head != null)
				return head;

			var o = Utilities.FindRigidbody(a, "object");
			if (o != null)
				return o;

			var c = Utilities.FindRigidbody(a, "control");
			if (c != null)
				return c;

			return null;
		}

		public override IEyesTarget Clone(int cloneFlags)
		{
			var t = new RigidbodyEyesTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(RigidbodyEyesTarget t, int cloneFlags)
		{
			t.receiver_ = receiver_;
			t.offset_ = offset_;
		}

		public override string Name
		{
			get
			{
				string s = "RB";

				if (atom_ != null)
					s += " " + atom_.uid;

				if (receiver_ != null)
					s += " " + receiver_.name;

				return s;
			}
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				if (value != null && receiver_ != null)
					receiver_ = Utilities.FindRigidbody(value, receiver_.name);
				else
					receiver_ = null;

				atom_ = value;
			}
		}

		public Rigidbody Receiver
		{
			get { return receiver_; }
			set { receiver_ = value; }
		}

		public Vector3 Offset
		{
			get { return offset_; }
			set { offset_ = value; }
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override void Update(Rigidbody head, Rigidbody chest)
		{
			if (receiver_ == null)
				return;

			pos_ = receiver_.position + offset_;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (atom_ != null)
			{
				if (J.Node.SaveType == SaveTypes.Preset)
					o.Add("atom", Utilities.PresetAtomPlaceholder);
				else
					o.Add("atom", atom_.uid);
			}

			if (receiver_ != null)
				o.Add("receiver", receiver_.name);

			o.Add("offset", J.Wrappers.ToJSON(offset_));

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("RigidbodyEyesTarget");
			if (o == null)
				return false;

			if (o.HasKey("atom"))
			{
				var atomUID = o.Get("atom").AsString();
				if (atomUID != null)
				{
					if (atomUID == Utilities.PresetAtomPlaceholder)
						atom_ = Synergy.Instance.DefaultAtom;
					else
						atom_ = SuperController.singleton.GetAtomByUid(atomUID);

					if (atom_ == null)
						Synergy.LogError("atom '" + atomUID + "' not found");
				}
			}

			o.OptRigidbody("receiver", atom_, ref receiver_);

			if (o.HasKey("offset"))
				J.Wrappers.FromJSON(o.Get("offset"), ref offset_);

			return true;
		}
	}

	class ConstantEyesTarget : BasicEyesTarget
	{
		public override string GetFactoryTypeName() { return "constant"; }
		public override string GetDisplayName() { return "Constant"; }

		private Vector3 offset_ = new Vector3();
		private Atom atom_ = null;
		private Rigidbody rel_ = null;

		private Vector3 pos_ = new Vector3();


		public ConstantEyesTarget()
			: this(new Vector3(), null, null)
		{
		}

		public ConstantEyesTarget(
			Vector3 offset, Atom a, Rigidbody rel)
		{
			atom_ = a;
			offset_ = offset;
			rel_ = rel;
		}

		public static Rigidbody GetPreferredTarget(Atom a)
		{
			var chest = Utilities.FindRigidbody(a, "chest");
			if (chest != null)
				return chest;

			var head = Utilities.FindRigidbody(a, "head");
			if (head != null)
				return head;

			var c = Utilities.FindRigidbody(a, "control");
			if (c != null)
				return c;

			return null;
		}

		public override IEyesTarget Clone(int cloneFlags)
		{
			var t = new ConstantEyesTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(ConstantEyesTarget t, int cloneFlags)
		{
			t.offset_ = offset_;
			t.rel_ = rel_;
		}

		public override string Name
		{
			get
			{
				string s = "Constant";
				if (rel_ != null)
					s += " " + rel_.name;

				return s;
			}
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public Vector3 Offset
		{
			get { return offset_; }
			set { offset_ = value; }
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				if (value != null && rel_ != null)
					rel_ = Utilities.FindRigidbody(value, rel_.name);
				else
					rel_ = null;

				atom_ = value;
			}
		}

		public Rigidbody RelativeTo
		{
			get { return rel_; }
			set { rel_ = value; }
		}

		public override void Update(Rigidbody head, Rigidbody chest)
		{
			if (rel_ == null)
				pos_ = head.position + offset_;
			else
				pos_ = rel_.position + rel_.rotation * offset_;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("offset", J.Wrappers.ToJSON(offset_));

			if (atom_ != null)
			{
				if (J.Node.SaveType == SaveTypes.Preset)
					o.Add("atom", Utilities.PresetAtomPlaceholder);
				else
					o.Add("atom", atom_.uid);
			}

			if (rel_ != null)
				o.Add("relative", rel_.name);

			return o;
		}

		public override bool FromJSON(J.Node n)
		{
			if (!base.FromJSON(n))
				return false;

			var o = n.AsObject("ConstantEyesTarget");
			if (o == null)
				return false;

			if (o.HasKey("offset"))
				J.Wrappers.FromJSON(o.Get("offset"), ref offset_);

			if (o.HasKey("atom"))
			{
				var atomUID = o.Get("atom").AsString();
				if (atomUID != null)
				{
					if (atomUID == Utilities.PresetAtomPlaceholder)
						atom_ = Synergy.Instance.DefaultAtom;
					else
						atom_ = SuperController.singleton.GetAtomByUid(atomUID);

					if (atom_ == null)
						Synergy.LogError("atom '" + atomUID + "' not found");
				}
			}

			o.OptRigidbody("relative", atom_, ref rel_);

			return true;
		}
	}

	class RandomEyesTarget : BasicEyesTarget
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

		private Atom atom_ = null;
		private Rigidbody rel_ = null;

		private Vector3 pos_ = new Vector3();


		public RandomEyesTarget()
			: this(null, null)
		{
		}

		public RandomEyesTarget(Atom a, Rigidbody rel)
		{
			atom_ = a;
			rel_ = rel;
		}

		public override IEyesTarget Clone(int cloneFlags)
		{
			var t = new RandomEyesTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(RandomEyesTarget t, int cloneFlags)
		{
			t.distance_ = distance_;
			t.centerX_ = centerX_;
			t.centerY_ = centerY_;
			t.xRange_ = xRange_;
			t.yRange_ = yRange_;
			t.avoidXRange_ = avoidXRange_;
			t.avoidYRange_ = avoidYRange_;
		}

		public override string Name
		{
			get
			{
				return "Random";
			}
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public Atom Atom
		{
			get
			{
				return atom_;
			}

			set
			{
				if (value != null && rel_ != null)
					rel_ = Utilities.FindRigidbody(value, rel_.name);
				else
					rel_ = null;

				atom_ = value;
			}
		}

		public Rigidbody RelativeTo
		{
			get { return rel_; }
			set { rel_ = value; }
		}

		public float Distance
		{
			get { return distance_; }
			set { distance_ = value; }
		}

		public float CenterX
		{
			get { return centerX_; }
			set { centerX_ = value; }
		}

		public float CenterY
		{
			get { return centerY_; }
			set { centerY_ = value; }
		}

		public float RangeX
		{
			get { return xRange_; }
			set { xRange_ = value; }
		}

		public float RangeY
		{
			get { return yRange_; }
			set { yRange_ = value; }
		}

		public float AvoidRangeX
		{
			get { return avoidXRange_; }
			set { avoidXRange_ = value; }
		}

		public float AvoidRangeY
		{
			get { return avoidYRange_; }
			set { avoidYRange_ = value; }
		}

		public override void Update(Rigidbody head, Rigidbody chest)
		{
			var rel = rel_ ?? chest ?? head;

			Vector3 fwd = rel.rotation * Vector3.forward;
			Vector3 ver = rel.rotation * Vector3.up;
			Vector3 hor = rel.rotation * Vector3.right;

			float x=0, y=0;

			if (avoidXRange_ == 0 && avoidYRange_ == 0)
			{
				x = Utilities.RandomFloat(
					centerX_ - xRange_,
					centerX_ + xRange_);

				y = Utilities.RandomFloat(
					centerY_ - yRange_,
					centerY_ + yRange_);
			}
			else
			{
				var av = new List<int>();

				if (avoidXRange_ < xRange_)
				{
					av.Add(0);
					av.Add(2);
				}

				if (avoidYRange_ < yRange_)
				{
					av.Add(1);
					av.Add(3);
				}

				if (av.Count > 0)
				{
					int i = Utilities.RandomInt(0, av.Count);
					i = av[i];

					if (i == 0)
					{
						// left
						x = Utilities.RandomFloat(0, xRange_ - avoidXRange_);
						x = centerX_ - avoidXRange_ - x;

						y = Utilities.RandomFloat(0, yRange_ * 2);
						y = centerY_ - yRange_ + y;
					}
					else if (i == 1)
					{
						// top
						x = Utilities.RandomFloat(0, xRange_ * 2);
						x = centerX_ - xRange_ + x;

						y = Utilities.RandomFloat(0, yRange_ - avoidYRange_);
						y = centerY_ + avoidYRange_ + y;
					}
					else if (i == 2)
					{
						// right
						x = Utilities.RandomFloat(0, xRange_ - avoidXRange_);
						x = centerX_ + avoidXRange_ + x;

						y = Utilities.RandomFloat(0, yRange_ * 2);
						y = centerY_ - yRange_ + y;
					}
					else if (i == 3)
					{
						// bottom
						x = Utilities.RandomFloat(0, xRange_ * 2);
						x = centerX_ - xRange_ + x;

						y = Utilities.RandomFloat(0, yRange_ - avoidYRange_);
						y = centerY_ - avoidYRange_ - y;
					}
				}
			}

			pos_ = rel.position +
				fwd * distance_ +
				ver * y +
				hor * x;
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			if (atom_ != null)
			{
				if (J.Node.SaveType == SaveTypes.Preset)
					o.Add("atom", Utilities.PresetAtomPlaceholder);
				else
					o.Add("atom", atom_.uid);
			}

			if (rel_ != null)
				o.Add("relative", rel_.name);

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

			var o = n.AsObject("RandomEyesTarget");
			if (o == null)
				return false;

			if (o.HasKey("atom"))
			{
				var atomUID = o.Get("atom").AsString();
				if (atomUID != null)
				{
					if (atomUID == Utilities.PresetAtomPlaceholder)
						atom_ = Synergy.Instance.DefaultAtom;
					else
						atom_ = SuperController.singleton.GetAtomByUid(atomUID);

					if (atom_ == null)
						Synergy.LogError("atom '" + atomUID + "' not found");
				}
			}

			o.OptRigidbody("relative", atom_, ref rel_);

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


	class EyesTargetContainer : IJsonable
	{
		private bool enabled_ = true;
		private IEyesTarget target_ = null;

		public EyesTargetContainer(IEyesTarget t = null)
		{
			target_ = t;
		}

		public EyesTargetContainer Clone(int cloneFlags)
		{
			var t = new EyesTargetContainer();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(EyesTargetContainer c, int cloneFlags)
		{
			c.target_ = target_?.Clone(cloneFlags);
			c.enabled_ = enabled_;
		}

		public bool Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public IEyesTarget Target
		{
			get
			{
				return target_;
			}

			set
			{
				target_ = value;
			}
		}

		public string Name
		{
			get
			{
				if (target_ == null)
					return "Target";
				else
					return target_.Name;
			}
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("enabled", enabled_);
			o.Add("target", target_);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			var o = n.AsObject("EyesTargetContainer");
			if (o == null)
				return false;

			o.Opt("enabled", ref enabled_);
			o.Opt<EyesTargetFactory, IEyesTarget>("target", ref target_);

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
		private Rigidbody chest_ = null;

		private List<EyesTargetContainer> targets_ =
			new List<EyesTargetContainer>();

		private RandomizableTime saccadeTime_ =
			new RandomizableTime(1, 0.2f, 0);
		private FloatParameter saccadeMin_ = new FloatParameter(
			"SaccadeMin", 0.01f, 0.01f);
		private FloatParameter saccadeMax_ = new FloatParameter(
			"SaccadeMax", 0.02f, 0.01f);

		private FloatParameter minDistance_ = new FloatParameter(
			"MinDistance", 0.5f, 0.1f);

		private int current_ = -1;
		private float lastProgress_ = -1;
		private bool lastFirstHalf_ = false;
		private Vector3 saccadeOffset_ = new Vector3();


		public EyesModifier()
		{
			if (!Utilities.AtomHasEyes(Atom))
				Atom = null;

			UpdateAtom();
		}

		public RandomizableTime SaccadeTime
		{
			get
			{
				return saccadeTime_;
			}
		}

		public float SaccadeMin
		{
			get { return saccadeMin_.Value; }
			set { saccadeMin_.Value = value; }
		}

		public FloatParameter SaccadeMinParameter
		{
			get { return saccadeMin_; }
		}

		public float SaccadeMax
		{
			get { return saccadeMax_.Value; }
			set { saccadeMax_.Value = value; }
		}

		public FloatParameter SaccadeMaxParameter
		{
			get { return saccadeMax_; }
		}

		public float MinDistance
		{
			get { return minDistance_.Value; }
			set { minDistance_.Value = value; }
		}

		public FloatParameter MinDistanceParameter
		{
			get { return minDistance_; }
		}

		public List<EyesTargetContainer> Targets
		{
			get { return new List<EyesTargetContainer>(targets_); }
		}

		public Rigidbody Head
		{
			get { return head_; }
		}

		public Rigidbody EyeTarget
		{
			get { return eyes_; }
		}

		public Vector3 CurrentSaccadeOffset
		{
			get { return saccadeOffset_; }
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
			m.chest_ = chest_;

			m.targets_.Clear();
			foreach (var t in targets_)
				m.targets_.Add(t.Clone(cloneFlags));

			m.saccadeTime_ = saccadeTime_.Clone(cloneFlags);

			if (!Bits.IsSet(cloneFlags, Utilities.CloneZero))
			{
				m.saccadeMin_.Value = saccadeMin_.Value;
				m.saccadeMax_.Value = saccadeMax_.Value;
				m.saccadeOffset_ = saccadeOffset_;
			}

			m.minDistance_.Value = minDistance_.Value;
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

		public EyesTargetContainer AddTarget(EyesTargetContainer t=null)
		{
			if (t == null)
				t = new EyesTargetContainer();

			targets_.Add(t);
			return t;
		}

		public override void Reset()
		{
			base.Reset();
			lastFirstHalf_ = false;
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

				saccadeOffset_.x = Utilities.RandomFloat(
					SaccadeMin, SaccadeMax);

				saccadeOffset_.y = Utilities.RandomFloat(
					SaccadeMin, SaccadeMax);

				saccadeOffset_.z = Utilities.RandomFloat(
					SaccadeMin, SaccadeMax);
			}


			if (progress != lastProgress_)
			{
				lastProgress_ = progress;

				if (CurrentIndex == -1 || progress == 1)
				{
					lastFirstHalf_ = firstHalf;
					Next();
				}
			}
		}

		private void Next()
		{
			if (targets_.Count == 0)
			{
				CurrentIndex = -1;
				return;
			}

			var i = CurrentIndex;
			current_ = -1;

			var start = i;
			if (start < 0)
				start = 0;

			for (; ;)
			{
				++i;
				if (i >= targets_.Count)
					i = 0;

				if (targets_[i].Enabled)
				{
					CurrentIndex = i;
					break;
				}

				if (i == start)
				{
					// nothing enabled
					CurrentIndex = -1;
					break;
				}
			}
		}

		public int CurrentIndex
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

		public IEyesTarget CurrentTarget
		{
			get
			{
				if (current_ >= 0 && current_ < targets_.Count)
					return targets_[current_].Target;
				else
					return null;
			}
		}

		private void TargetChanged()
		{
			var t = CurrentTarget;
			if (t == null)
				return;

			t.Update(head_, chest_);
		}

		protected override void DoSet(bool paused)
		{
			base.DoSet(paused);

			if (paused)
				return;

			if (eyes_ == null || head_ == null)
				return;

			var t = CurrentTarget;
			if (t == null)
				return;

			var pos = t.Position + saccadeOffset_;

			var distanceToTarget = Vector3.Distance(head_.position, pos);
			if (distanceToTarget < MinDistance)
			{
				var add = MinDistance - distanceToTarget;
				var dir = (pos - head_.position).normalized;

				pos += (dir * add);
			}


			eyes_.position = pos;
		}

		protected override string MakeName()
		{
			if (Atom == null)
				return "EY";
			else
				return "EY " + Atom.uid;
		}

		protected override void AtomChanged()
		{
			base.AtomChanged();
			UpdateAtom();
		}

		public override J.Node ToJSON()
		{
			var o = base.ToJSON().AsObject();

			o.Add("targets", targets_);
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

			targets_.Clear();

			var targetsArray = o.Get("targets").AsArray();
			if (targetsArray != null)
			{
				targetsArray.ForEach((node) =>
				{
					var tc = new EyesTargetContainer();
					if (tc.FromJSON(node))
						targets_.Add(tc);
				});
			}

			o.Opt("saccadeTime", ref saccadeTime_);
			o.Opt("saccadeMin", saccadeMin_);
			o.Opt("saccadeMax", saccadeMax_);
			o.Opt("minDistance", minDistance_);

			UpdateAtom();

			return true;
		}

		private void UpdateAtom()
		{
			if (Atom == null)
			{
				head_ = null;
				eyes_ = null;
				chest_ = null;
				return;
			}

			head_ = Utilities.FindRigidbody(Atom, "headControl");
			eyes_ = Utilities.FindRigidbody(Atom, "eyeTargetControl");
			chest_ = Utilities.FindRigidbody(Atom, "chestControl");

			if (chest_ == null)
				Synergy.LogError("atom " + Atom.uid + " has no chest");

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
