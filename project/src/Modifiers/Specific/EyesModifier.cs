using System;
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
				new RandomEyesTarget(),
				new PlayerEyesTarget()
			};
		}
	}


	interface IEyesTarget : IFactoryObject
	{
		IEyesTarget Clone(int cloneFlags);
		Vector3 Position { get; }
		bool Valid { get; }
		void Update(Rigidbody head, Rigidbody chest);
		string Name { get; }
		string LookMode { get; }
	}

	abstract class BasicEyesTarget : IEyesTarget
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		public abstract IEyesTarget Clone(int cloneFlags);
		public abstract Vector3 Position { get; }
		public abstract bool Valid { get; }
		public abstract void Update(Rigidbody head, Rigidbody chest);

		public abstract string Name { get; }

		public virtual string LookMode
		{
			get { return "Target"; }
		}

		public virtual J.Node ToJSON()
		{
			return new J.Object();
		}

		public virtual bool FromJSON(J.Node n)
		{
			return true;
		}
	}


	class PlayerEyesTarget : BasicEyesTarget
	{
		public static string FactoryTypeName { get; } = "player";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Player";
		public override string GetDisplayName() { return DisplayName; }

		public PlayerEyesTarget()
		{
		}

		public static Rigidbody GetPreferredTarget(Atom a)
		{
			return null;
		}

		public override IEyesTarget Clone(int cloneFlags)
		{
			var t = new PlayerEyesTarget();
			CopyTo(t, cloneFlags);
			return t;
		}

		private void CopyTo(PlayerEyesTarget t, int cloneFlags)
		{
		}

		public override string Name
		{
			get { return "Player"; }
		}

		public override string LookMode
		{
			get { return "Player"; }
		}

		public override Vector3 Position
		{
			get { return Utilities.CenterEyePosition(); }
		}

		public override bool Valid
		{
			get { return true; }
		}

		public override void Update(Rigidbody head, Rigidbody chest)
		{
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

			var o = n.AsObject("PlayerTargetType");
			if (o == null)
				return false;

			return true;
		}
	}


	class RigidbodyEyesTarget : BasicEyesTarget
	{
		public static string FactoryTypeName { get; } = "rigidbody";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Rigidbody";
		public override string GetDisplayName() { return DisplayName; }

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
			t.atom_ = atom_;
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

				if (receiver_ == null && value != null)
					receiver_ = GetPreferredTarget(value);

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

		public override bool Valid
		{
			get { return receiver_ != null; }
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
				if (J.Node.SaveContext.UsePlaceholder)
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

			// migration from constant eye target
			o.OptRigidbody("relative", atom_, ref receiver_);

			if (receiver_ == null)
				o.OptRigidbody("receiver", atom_, ref receiver_);

			if (o.HasKey("offset"))
				J.Wrappers.FromJSON(o.Get("offset"), ref offset_);

			return true;
		}
	}

	class RandomEyesTarget : BasicEyesTarget
	{
		public static string FactoryTypeName { get; } = "random";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random";
		public override string GetDisplayName() { return DisplayName; }

		private float distance_ = 1.0f;
		private float centerX_ = 0;
		private float centerY_ = 0;
		private float xRange_ = 2;
		private float yRange_ = 2;
		private float avoidXRange_ = 0;
		private float avoidYRange_ = 0;

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

		public static Rigidbody GetPreferredTarget(Atom a)
		{
			var chest = Utilities.FindRigidbody(a, "chest");
			if (chest != null)
				return chest;

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
				string s = "R";

				if (atom_ != null)
					s += " " + atom_.uid;

				if (rel_ != null)
					s += " " + rel_.name;

				return s;
			}
		}

		public override Vector3 Position
		{
			get { return pos_; }
		}

		public override bool Valid
		{
			get { return (rel_ != null); }
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

				if (rel_ == null && value != null)
					rel_ = GetPreferredTarget(value);

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
				bool avoidHor = false;
				bool avoidVer = false;

				if (avoidXRange_ < xRange_)
					avoidHor = true;

				if (avoidYRange_ < yRange_)
					avoidVer = true;

				if (avoidHor || avoidVer)
				{
					int side;

					if (avoidHor && avoidVer)
					{
						side = Utilities.RandomInt(0, 4);
					}
					else
					{
						int i = Utilities.RandomInt(0, 2);

						if (avoidHor)
						{
							if (i == 0)
								side = 0;
							else
								side = 2;
						}
						else
						{
							if (i == 0)
								side = 1;
							else
								side = 3;
						}
					}


					if (side == 0)
					{
						// left
						x = Utilities.RandomFloat(0, xRange_ - avoidXRange_);
						x = centerX_ - avoidXRange_ - x;

						y = Utilities.RandomFloat(0, yRange_ * 2);
						y = centerY_ - yRange_ + y;
					}
					else if (side == 1)
					{
						// top
						x = Utilities.RandomFloat(0, xRange_ * 2);
						x = centerX_ - xRange_ + x;

						y = Utilities.RandomFloat(0, yRange_ - avoidYRange_);
						y = centerY_ + avoidYRange_ + y;
					}
					else if (side == 2)
					{
						// right
						x = Utilities.RandomFloat(0, xRange_ - avoidXRange_);
						x = centerX_ + avoidXRange_ + x;

						y = Utilities.RandomFloat(0, yRange_ * 2);
						y = centerY_ - yRange_ + y;
					}
					else if (side == 3)
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
				if (J.Node.SaveContext.ForPreset)
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

			// migration: constant target removed, was redundant with rigidbody
			if (o.HasChildObject("target"))
			{
				var t = o.Get("target").AsObject();

				string type = "";
				t.Opt("factoryTypeName", ref type);

				if (type == "constant")
				{
					Synergy.LogInfo("found constant eye target, converting to rigidbody");
					target_ = new RigidbodyEyesTarget();
					return target_.FromJSON(t);
				}
			}


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

		public const int SettingIgnore = 0;
		public const int SettingEnable = 1;
		public const int SettingDisable = 2;

		private Rigidbody head_ = null;
		private Rigidbody eyes_ = null;
		private Rigidbody chest_ = null;
		private JSONStorableStringChooser lookMode_ = null;

		private List<EyesTargetContainer> targets_ =
			new List<EyesTargetContainer>();

		private ShuffledOrder order_ = new ShuffledOrder();

		private RandomizableTime saccadeTime_ =
			new RandomizableTime(1, 0.2f, 0);
		private FloatParameter saccadeMin_ = new FloatParameter(
			"SaccadeMin", 0.1f, 0.01f);
		private FloatParameter saccadeMax_ = new FloatParameter(
			"SaccadeMax", 0.2f, 0.01f);

		private FloatParameter minDistance_ = new FloatParameter(
			"MinDistance", 0.5f, 0.1f);

		private int gazeSetting_ = SettingIgnore;
		private Integration.Gaze gaze_ = new Integration.Gaze();

		private int blinkSetting_ = SettingIgnore;
		private JSONStorableBool blink_ = null;

		private int currentOrder_ = -1;
		private float lastProgress_ = -1;
		private Vector3 saccadeOffset_ = new Vector3();
		private Vector3 last_ = new Vector3();
		private float focusProgress_ = 0;
		private RandomizableTime focusDuration_ = new RandomizableTime(0.5f, 0, 0);
		private float currentFocusDuration_ = -1;

		public EyesModifier()
		{
			if (!Utilities.AtomHasEyes(Atom))
				Atom = null;

			UpdateAtom();
		}

		public RandomizableTime SaccadeTime
		{
			get { return saccadeTime_; }
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

		public RandomizableTime FocusDuration
		{
			get { return focusDuration_; }
		}

		public float CurrentFocusDuration
		{
			get { return currentFocusDuration_; }
		}

		public float FocusProgressNormalized
		{
			get
			{
				if (currentFocusDuration_ > 0)
					return focusProgress_ / currentFocusDuration_;
				else
					return 0;
			}
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

		public bool GazeAvailable
		{
			get { return gaze_.Available(); }
		}

		public int GazeSetting
		{
			get { return gazeSetting_; }
			set { gazeSetting_ = value; }
		}

		public Integration.Gaze Gaze
		{
			get
			{
				return gaze_;
			}
		}

		public bool BlinkAvailable
		{
			get { return EnsureBlink(); }
		}

		public int BlinkSetting
		{
			get { return blinkSetting_; }
			set { blinkSetting_ = value; }
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
			m.lookMode_ = lookMode_;
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
			m.focusDuration_ = focusDuration_.Clone(cloneFlags);
			m.gazeSetting_ = gazeSetting_;
			m.gaze_ = gaze_.Clone(cloneFlags);
			m.blinkSetting_ = blinkSetting_;
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

		public void RemoveTarget(EyesTargetContainer t)
		{
			targets_.Remove(t);
		}

		public override void Reset()
		{
			base.Reset();
			saccadeTime_.Reset();
		}

		protected override void DoTick(
			float deltaTime, float progress, bool firstHalf)
		{
			base.DoTick(deltaTime, progress, firstHalf);
			if (head_ == null || eyes_ == null)
				return;

			var tc = CurrentTargetContainer;
			if (tc != null && !tc.Enabled)
			{
				// target got disabled
				Next();
			}


			saccadeTime_.Tick(deltaTime);
			if (saccadeTime_.Finished)
			{
				saccadeTime_.Reset();

				saccadeOffset_.x = Utilities.RandomFloat(
					SaccadeMin / 10, SaccadeMax / 10);

				saccadeOffset_.y = Utilities.RandomFloat(
					SaccadeMin / 10, SaccadeMax / 10);

				saccadeOffset_.z = Utilities.RandomFloat(
					SaccadeMin / 10, SaccadeMax / 10);
			}

			focusDuration_.Tick(deltaTime);
			if (focusDuration_.Finished)
				focusDuration_.Reset();

			CheckFocus(deltaTime);

			if (progress != lastProgress_)
			{
				if (CurrentOrderIndex == -1 || (progress < lastProgress_ && firstHalf))
					Next();

				lastProgress_ = progress;
			}
		}

		private void Next()
		{
			if (targets_.Count == 0)
			{
				SetOrderIndex(-1);
				CheckGaze();
				CheckBlink();
				return;
			}

			if (targets_.Count != order_.Count)
				order_.Shuffle(targets_.Count);

			var i = CurrentOrderIndex;
			SetOrderIndex(-1);

			var start = i;

			for (; ;)
			{
				++i;
				if (i >= order_.Count)
				{
					order_.Shuffle(targets_.Count);
					i = 0;
				}

				if (GetTarget(i).Enabled)
				{
					SetOrderIndex(i);
					break;
				}


				if (start < 0)
				{
					// no index when started
					start = 0;
				}
				else if (i == start)
				{
					// nothing enabled
					SetOrderIndex(-1);
					break;
				}
			}
		}

		public EyesTargetContainer GetTarget(int orderIndex)
		{
			if (orderIndex < 0 || orderIndex >= order_.Count)
				return null;

			var i = order_.Get(orderIndex);
			if (i < 0 || i >= targets_.Count)
				return null;

			return targets_[i];
		}

		public int CurrentOrderIndex
		{
			get
			{
				return currentOrder_;
			}
		}

		public int CurrentRealIndex
		{
			get
			{
				if (CurrentOrderIndex < 0 || CurrentOrderIndex >= order_.Count)
					return -1;

				var i = order_.Get(CurrentOrderIndex);
				if (i < 0 || i >= targets_.Count)
					return -1;

				return i;
			}
		}

		private void SetOrderIndex(int i)
		{
			if (currentOrder_ != i)
			{
				currentOrder_ = i;
				TargetChanged();
			}
		}

		public EyesTargetContainer CurrentTargetContainer
		{
			get
			{
				var i = CurrentRealIndex;
				if (i < 0 || i >= targets_.Count)
					return null;

				return targets_[i];
			}
		}

		public IEyesTarget CurrentTarget
		{
			get
			{
				return CurrentTargetContainer?.Target;
			}
		}

		private void TargetChanged()
		{
			var t = CurrentTarget;
			if (t == null)
				return;

			bool needsFocus = true;

			if (eyes_ == null)
			{
				last_ = new Vector3();
			}
			else if (lookMode_ != null && lookMode_.val == "Player" && t.LookMode != "Player")
			{
				// going from player to target, the last position is the
				// player's eyes
				last_ = Utilities.CenterEyePosition();
			}
			else if (lookMode_ != null && lookMode_.val == "Player" && t.LookMode == "Player")
			{
				// look is already in player mode and that's what the target
				// wants to, no focusing needed
				needsFocus = false;
			}
			else
			{
				last_ = eyes_.position;
			}

			t.Update(head_, chest_);
			CheckGaze();
			CheckBlink();

			if (needsFocus)
				StartFocus();
			else
				StopFocus();
		}

		private void StartFocus()
		{
			focusProgress_ = 0;
			currentFocusDuration_ = focusDuration_.Current;

			// look must be in target mode so focusing works, regardless of what
			// the target's EyeMode wants
			if (lookMode_ != null)
				lookMode_.val = "Target";
		}

		private void StopFocus()
		{
			currentFocusDuration_ = focusDuration_.Current;
			focusProgress_ = currentFocusDuration_;

			// focusing is done, set the look mode to what the target
			// actually wants
			if (lookMode_ != null)
			{
				var t = CurrentTarget;
				if (t != null)
					lookMode_.val = t.LookMode;
			}
		}

		private void CheckFocus(float deltaTime)
		{
			if (currentFocusDuration_ < 0)
				currentFocusDuration_ = focusDuration_.Current;

			if (focusProgress_ < currentFocusDuration_)
			{
				focusProgress_ = Utilities.Clamp(
					focusProgress_ + deltaTime, 0, currentFocusDuration_);

				if (focusProgress_ >= currentFocusDuration_)
					StopFocus();
			}
		}

		private void CheckGaze()
		{
			bool e;

			if (gazeSetting_ == SettingEnable)
				e = true;
			else if (gazeSetting_ == SettingDisable)
				e = false;
			else
				return;

			if (!gaze_.SetEnabled(Atom, e))
			{
				Synergy.LogError(
					"gaze: can't set value, changing setting to Ignore");

				gazeSetting_ = SettingIgnore;
			}
		}

		private bool EnsureBlink()
		{
			if (blink_ == null && Atom != null)
			{
				var ec = Atom.GetStorableByID("EyelidControl");
				if (ec == null)
				{
					Synergy.LogError(
						"blink: EyelidControl not found, " +
						"changing setting to Ignore");

					return false;
				}

				blink_ = ec.GetBoolJSONParam("blinkEnabled");
				if (blink_ == null)
				{
					Synergy.LogError(
						"blink: blinkEnabled not found in EyelidControl, " +
						"changing setting to Ignore");

					return false;
				}
			}

			return true;
		}

		private void CheckBlink()
		{
			if (Atom == null)
				return;

			bool e;

			if (blinkSetting_ == SettingEnable)
				e = true;
			else if (blinkSetting_ == SettingDisable)
				e = false;
			else
				return;

			if (!EnsureBlink())
			{
				blinkSetting_ = SettingIgnore;
				return;
			}


			try
			{
				blink_.val = e;
			}
			catch(Exception ex)
			{
				Synergy.LogError(
					"blink: can't set value on blinkEnabled, " +
					ex.ToString() + ", changing setting to Ignore");

				blinkSetting_ = SettingIgnore;
			}
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

			if (t.Valid)
			{
				var pos = AdjustedPosition(t.Position + saccadeOffset_);

				if (currentFocusDuration_ > 0)
				{
					float focus = FocusProgressNormalized;
					pos.x = Mathf.Lerp(last_.x, pos.x, focus);
					pos.y = Mathf.Lerp(last_.y, pos.y, focus);
					pos.z = Mathf.Lerp(last_.z, pos.z, focus);
				}

				eyes_.position = pos;
			}
		}

		public Vector3 AdjustedPosition(Vector3 pos)
		{
			if (head_ == null)
				return pos;

			var distanceToTarget = Vector3.Distance(head_.position, pos);
			if (distanceToTarget < MinDistance)
			{
				var add = MinDistance - distanceToTarget;
				var dir = (pos - head_.position).normalized;

				pos += (dir * add);
			}

			return pos;
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
			o.Add("focusDuration", focusDuration_);
			o.Add("gaze", gazeSetting_);
			o.Add("blink", blinkSetting_);

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
			o.Opt("focusDuration", ref focusDuration_);
			o.Opt("gaze", ref gazeSetting_);
			o.Opt("blink", ref blinkSetting_);

			UpdateAtom();

			return true;
		}

		private void UpdateAtom()
		{
			gaze_.Atom = Atom;
			blink_ = null;

			if (Atom == null)
			{
				head_ = null;
				eyes_ = null;
				lookMode_ = null;
				chest_ = null;
				return;
			}

			head_ = Utilities.FindRigidbody(Atom, "headControl");
			eyes_ = Utilities.FindRigidbody(Atom, "eyeTargetControl");
			chest_ = Utilities.FindRigidbody(Atom, "chestControl");

			lookMode_ = null;

			var eyesStorable = Atom.GetStorableByID("Eyes");
			if (eyesStorable != null)
			{
				lookMode_ = eyesStorable.GetStringChooserJSONParam("lookMode");
				if (lookMode_ == null)
					Synergy.LogError("atom " + Atom.uid + " has no lookMode");
			}

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
