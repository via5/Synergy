using System.Collections.Generic;
using UnityEngine;

namespace Synergy
{
	interface IEyesModifierTargetUI
	{
		List<Widget> GetWidgets();
	}

	abstract class BasicEyesModifierTargetUI : IEyesModifierTargetUI
	{
		protected readonly EyesModifierTargetUIContainer parent_;

		private EyesTargetContainer target_ = null;

		public BasicEyesModifierTargetUI(
			EyesModifierTargetUIContainer parent, EyesTargetContainer t)
		{
			parent_ = parent;
			target_ = t;
		}

		public virtual List<Widget> GetWidgets()
		{
			return new List<Widget>() { };
		}
	}

	class PlayerEyesTargetUI : BasicEyesModifierTargetUI
	{
		public PlayerEyesTargetUI(
			EyesModifierTargetUIContainer parent, EyesTargetContainer t)
				: base(parent, t)
		{
		}
	}



	class RigidbodyEyesTargetUI : BasicEyesModifierTargetUI
	{
		private RigidbodyEyesTarget target_ = null;

		private readonly AtomList atom_;
		private readonly ForceReceiverList receiver_;
		private readonly Vector3UI offset_;

		public RigidbodyEyesTargetUI(
			EyesModifierTargetUIContainer parent, EyesTargetContainer tc)
				: base(parent, tc)
		{
			target_ = tc.Target as RigidbodyEyesTarget;

			atom_ = new AtomList(
				"Atom", target_?.Atom?.uid, AtomChanged,
				null, Widget.Right);

			receiver_ = new ForceReceiverList(
				"Receiver", target_?.Receiver?.name,
				ReceiverChanged, Widget.Right);

			offset_ = new Vector3UI(
				"Offset", Widget.Right,
				new FloatRange(-10, 10), OffsetChanged);

			offset_.Value = target_.Offset;
			receiver_.Atom = target_.Atom;
		}

		public override List<Widget> GetWidgets()
		{
			var list = base.GetWidgets();

			list.AddRange(new List<Widget>()
			{
				atom_, receiver_
			});

			list.AddRange(offset_.GetWidgets());

			return list;
		}

		private void AtomChanged(Atom a)
		{
			target_.Atom = a;
			receiver_.Atom = a;

			if (target_.Receiver == null)
			{
				var pt = RigidbodyEyesTarget.GetPreferredTarget(a);

				if (pt == null)
				{
					receiver_.Value = "";
					target_.Receiver = null;
				}
				else
				{
					receiver_.Value = pt.name;
					target_.Receiver = pt;
				}
			}
			else
			{
				receiver_.Value = target_.Receiver.name;
			}

			parent_.NameChanged();
		}

		private void ReceiverChanged(Rigidbody rb)
		{
			target_.Receiver = rb;
			parent_.NameChanged();
		}

		private void OffsetChanged(Vector3 v)
		{
			target_.Offset = v;
			parent_.NameChanged();
		}
	}


	class RandomEyesTargetUI : BasicEyesModifierTargetUI
	{
		private RandomEyesTarget target_ = null;

		private readonly AtomList atom_;
		private readonly ForceReceiverList rel_;
		private readonly FloatSlider distance_;
		private readonly FloatSlider centerX_;
		private readonly FloatSlider centerY_;
		private readonly FloatSlider xRange_;
		private readonly FloatSlider yRange_;
		private readonly FloatSlider avoidXRange_;
		private readonly FloatSlider avoidYRange_;


		public RandomEyesTargetUI(
			EyesModifierTargetUIContainer parent, EyesTargetContainer tc)
				: base(parent, tc)
		{
			target_ = tc.Target as RandomEyesTarget;

			var r = new FloatRange(0, 10);

			atom_ = new AtomList(
				"Relative atom", target_?.Atom?.uid, AtomChanged,
				null, Widget.Right);

			rel_ = new ForceReceiverList(
				"Relative receiver", target_?.RelativeTo?.name,
				ReceiverChanged, Widget.Right);

			distance_ = new FloatSlider(
				"Distance", target_.Distance, r, DistanceChanged, Widget.Right);

			centerX_ = new FloatSlider(
				"Offset X", target_.CenterX, r, CenterXChanged, Widget.Right);

			centerY_ = new FloatSlider(
				"Offset Y", target_.CenterY, r, CenterYChanged, Widget.Right);

			xRange_ = new FloatSlider(
				"Range X", target_.RangeX, r, RangeXChanged, Widget.Right);

			yRange_ = new FloatSlider(
				"Range Y", target_.RangeY, r, RangeYChanged, Widget.Right);

			avoidXRange_ = new FloatSlider(
				"Avoid range X", target_.AvoidRangeX, r,
				AvoidRangeXChanged, Widget.Right);

			avoidYRange_ = new FloatSlider(
				"Avoid range Y", target_.AvoidRangeY, r,
				AvoidRangeYChanged, Widget.Right);

			rel_.Atom = target_.Atom;
		}

		public override List<Widget> GetWidgets()
		{
			var list = base.GetWidgets();

			list.AddRange(new List<Widget>()
			{
				atom_,
				rel_,
				distance_,
				centerX_,
				centerY_,
				xRange_,
				yRange_,
				avoidXRange_,
				avoidYRange_
			});

			return list;
		}

		private void AtomChanged(Atom a)
		{
			target_.Atom = a;
			rel_.Atom = a;

			if (target_.RelativeTo == null)
			{
				var pt = RigidbodyEyesTarget.GetPreferredTarget(a);

				if (pt == null)
				{
					rel_.Value = "";
					target_.RelativeTo = null;
				}
				else
				{
					rel_.Value = pt.name;
					target_.RelativeTo = pt;
				}
			}
			else
			{
				rel_.Value = target_.RelativeTo.name;
			}

			parent_.NameChanged();
		}

		private void ReceiverChanged(Rigidbody rb)
		{
			target_.RelativeTo = rb;
			parent_.NameChanged();
		}

		private void DistanceChanged(float f)
		{
			target_.Distance = f;
		}

		private void CenterXChanged(float f)
		{
			target_.CenterX = f;
		}

		private void CenterYChanged(float f)
		{
			target_.CenterY = f;
		}

		private void RangeXChanged(float f)
		{
			target_.RangeX = f;
		}

		private void RangeYChanged(float f)
		{
			target_.RangeY = f;
		}

		private void AvoidRangeXChanged(float f)
		{
			target_.AvoidRangeX = f;
		}

		private void AvoidRangeYChanged(float f)
		{
			target_.AvoidRangeY = f;
		}
	}


	class EyesModifierTargetUIContainer
	{
		private readonly Collapsible collapsible_;

		private EyesModifier modifier_ = null;
		private EyesTargetContainer container_ = null;

		private readonly ConfirmableButton delete_ = null;
		private readonly
			FactoryStringList<EyesTargetFactory, IEyesTarget> types_;
		private readonly Checkbox enabled_;

		private IEyesModifierTargetUI ui_ = null;
		private bool stale_ = true;


		public EyesModifierTargetUIContainer(EyesModifier m, EyesTargetContainer t)
		{
			modifier_ = m;
			container_ = t;

			delete_ = new ConfirmableButton(
				"Delete target", DeleteTarget, Widget.Right);

			types_ = new FactoryStringList<EyesTargetFactory, IEyesTarget>(
				"Type", TypeChanged, Widget.Right);

			enabled_ = new Checkbox(
				"Enabled", t.Enabled, EnabledChanged, Widget.Right);

			collapsible_ = new Collapsible(
				container_.Name, null, Widget.Right);

			UpdateWidgets();
		}

		public Collapsible Collapsible
		{
			get
			{
				UpdateWidgets();
				return collapsible_;
			}
		}

		public void NameChanged()
		{
			collapsible_.Text = container_.Name;
		}

		private void DeleteTarget()
		{
			if (container_ == null || modifier_ == null)
				return;

			modifier_.RemoveTarget(container_);
			Synergy.Instance.UI.NeedsReset("eyes target removed");
		}

		private void TypeChanged(IEyesTarget t)
		{
			if (container_ == null)
				return;

			container_.Target = t;
			stale_ = true;
			NameChanged();

			Synergy.Instance.UI.NeedsReset("eyes target type changed");
		}

		private void UpdateWidgets()
		{
			if (!stale_)
				return;

			stale_ = false;

			collapsible_.Clear();
			collapsible_.Add(delete_);
			collapsible_.Add(types_);
			collapsible_.Add(enabled_);

			var t = container_.Target;

			types_.Value = t;

			if (t is RigidbodyEyesTarget)
				ui_ = new RigidbodyEyesTargetUI(this, container_);
			else if (t is RandomEyesTarget)
				ui_ = new RandomEyesTargetUI(this, container_);
			else if (t is PlayerEyesTarget)
				ui_ = new PlayerEyesTargetUI(this, container_);
			else
				ui_ = null;

			if (ui_ != null)
			{
				foreach (var w in ui_.GetWidgets())
					collapsible_.Add(w);
			}

			collapsible_.Add(new SmallSpacer(Widget.Right));
		}

		private void EnabledChanged(bool b)
		{
			if (container_ == null)
				return;

			container_.Enabled = b;
		}
	}


	class EyesModifierUI : AtomModifierUI
	{
		public override string ModifierType
		{
			get { return EyesModifier.FactoryTypeName; }
		}


		private EyesModifier modifier_ = null;
		private readonly Collapsible saccade_;
		private readonly RandomizableTimeWidgets saccadeTime_;
		private readonly FloatSlider saccadeMin_, saccadeMax_;
		private readonly FloatSlider minDistance_;
		private readonly Collapsible focusDurationCollapsible_;
		private readonly RandomizableTimeWidgets focusDuration_;
		private readonly StringList gaze_;
		private readonly StringList blink_;

		private readonly Button addTarget_;
		private readonly Checkbox previewsEnabled_;
		private readonly FloatSlider previewsAlpha_;
		private readonly List<EyesModifierTargetUIContainer> targets_ =
			new List<EyesModifierTargetUIContainer>();

		private readonly EyesPreviews previews_ = new EyesPreviews();

		public EyesModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasEyes)
		{
			saccade_ = new Collapsible("Saccade", null, Widget.Right);

			saccadeTime_ = new RandomizableTimeWidgets(
				"Saccade interval", Widget.Right);

			saccadeMin_ = new FloatSlider(
				"Saccade minimum (x10)", 0, new FloatRange(0, 1),
				SaccadeMinChanged, Widget.Right);

			saccadeMax_ = new FloatSlider(
				"Saccade maximum (x10)", 0, new FloatRange(0, 1),
				SaccadeMaxChanged, Widget.Right);

			minDistance_ = new FloatSlider(
				"Minimum distance (avoids cross-eyed)", 0, new FloatRange(0, 1),
				MinDistanceChanged, Widget.Right);

			focusDurationCollapsible_ = new Collapsible(
				"Focus time", null, Widget.Right);

			focusDuration_ = new RandomizableTimeWidgets(
				"Focus time", Widget.Right);

			gaze_ = new StringList(
				"MacGruber's Gaze", GazeChanged, Widget.Right);

			blink_ = new StringList("Blink", BlinkChanged, Widget.Right);

			addTarget_ = new Button("Add target", AddTarget, Widget.Right);

			previewsEnabled_ = new Checkbox(
				"Show previews", PreviewsChanged, Widget.Right);

			previewsAlpha_ = new FloatSlider(
				"Previews alpha", 0.3f, new FloatRange(0, 1),
				PreviewsAlphaChanged, Widget.Right);

			foreach (var w in saccadeTime_.GetWidgets())
				saccade_.Add(w);

			saccade_.Add(saccadeMin_);
			saccade_.Add(saccadeMax_);
			saccade_.Add(new SmallSpacer(Widget.Right));

			foreach (var w in focusDuration_.GetWidgets())
				focusDurationCollapsible_.Add(w);

			focusDurationCollapsible_.Add(new SmallSpacer(Widget.Right));
		}


		public override void AddToTopUI(IModifier m)
		{
			var changed = (m != modifier_);

			modifier_ = m as EyesModifier;

			previews_.Modifier = modifier_;
			previews_.Create();

			if (modifier_ == null)
				return;

			if (modifier_.Targets.Count != targets_.Count)
				changed = true;

			if (changed)
			{
				targets_.Clear();

				foreach (var t in modifier_.Targets)
				{
					targets_.Add(
						new EyesModifierTargetUIContainer(modifier_, t));
				}
			}


			saccadeTime_.SetValue(
				modifier_.SaccadeTime, new FloatRange(0, 5));
			focusDuration_.SetValue(
				modifier_.FocusDuration, new FloatRange(0, 1));
			saccadeMin_.Parameter = modifier_.SaccadeMinParameter;
			saccadeMax_.Parameter = modifier_.SaccadeMaxParameter;
			minDistance_.Parameter = modifier_.MinDistanceParameter;
			previewsEnabled_.Value = previews_.Enabled;
			previewsAlpha_.Value = previews_.Alpha;

			UpdateGaze();
			UpdateBlink();

			AddAtomWidgets(m);
			widgets_.AddToUI(saccade_);
			widgets_.AddToUI(focusDurationCollapsible_);
			widgets_.AddToUI(minDistance_);
			widgets_.AddToUI(gaze_);
			widgets_.AddToUI(blink_);
			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			widgets_.AddToUI(addTarget_);
			widgets_.AddToUI(previewsEnabled_);
			widgets_.AddToUI(previewsAlpha_);

			if (targets_.Count > 0)
			{
				widgets_.AddToUI(new SmallSpacer(Widget.Right));
				foreach (var t in targets_)
					widgets_.AddToUI(t.Collapsible);
			}

			widgets_.AddToUI(new LargeSpacer(Widget.Right));
			widgets_.AddToUI(new LargeSpacer(Widget.Right));

			base.AddToTopUI(m);
		}

		public override void RemoveFromUI()
		{
			base.RemoveFromUI();
			previews_.Destroy();
		}

		public override void PluginEnabled(bool b)
		{
			if (previews_.Enabled)
			{
				if (b)
					previews_.Create();
				else
					previews_.Destroy();
			}
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);
			UpdateGaze();
			UpdateBlink();
		}

		public override void Update()
		{
			if (previews_.Enabled)
				previews_.Update();
		}

		private void SaccadeMinChanged(float f)
		{
			if (modifier_ == null)
				return;

			modifier_.SaccadeMin = f;
		}

		private void SaccadeMaxChanged(float f)
		{
			if (modifier_ == null)
				return;

			modifier_.SaccadeMax = f;
		}

		private void MinDistanceChanged(float f)
		{
			if (modifier_ == null)
				return;

			modifier_.MinDistance = f;
		}

		private void GazeChanged(string s)
		{
			if (modifier_ == null)
				return;

			if (s == "disable")
				modifier_.GazeSetting = EyesModifier.SettingDisable;
			else if (s == "enable")
				modifier_.GazeSetting = EyesModifier.SettingEnable;
			else
				modifier_.GazeSetting = EyesModifier.SettingIgnore;
		}

		private void BlinkChanged(string s)
		{
			if (modifier_ == null)
				return;

			if (s == "disable")
				modifier_.BlinkSetting = EyesModifier.SettingDisable;
			else if (s == "enable")
				modifier_.BlinkSetting = EyesModifier.SettingEnable;
			else
				modifier_.BlinkSetting = EyesModifier.SettingIgnore;
		}

		private void AddTarget()
		{
			if (modifier_ == null)
				return;

			var t = modifier_.AddTarget();
			targets_.Add(new EyesModifierTargetUIContainer(modifier_, t));

			Synergy.Instance.UI.NeedsReset("eyes target added");
		}

		private void PreviewsChanged(bool b)
		{
			previews_.Enabled = b;
		}

		private void PreviewsAlphaChanged(float f)
		{
			previews_.Alpha = f;
		}

		private void UpdateGaze()
		{
			if (modifier_ == null || !modifier_.Gaze.Available())
			{
				gaze_.Choices = new List<string>() { };
				gaze_.DisplayChoices = new List<string>() { };
				gaze_.Value = "Not found";
				gaze_.Enabled = false;
				return;
			}

			UpdateSetting(gaze_, modifier_.GazeSetting);
		}

		private void UpdateBlink()
		{
			UpdateSetting(blink_, modifier_.BlinkSetting);
		}

		private void UpdateSetting(StringList list, int setting)
		{
			list.Choices = new List<string>() { "ignore", "enable", "disable" };
			list.DisplayChoices = new List<string>() { "Don't change", "Enable", "Disable" };
			list.Enabled = true;

			switch (setting)
			{
				case EyesModifier.SettingIgnore:
					list.Value = "ignore";
					break;

				case EyesModifier.SettingEnable:
					list.Value = "enable";
					break;

				case EyesModifier.SettingDisable:
					list.Value = "disable";
					break;
			}
		}
	}


	class EyesPreviews
	{
		class Preview
		{
			public GameObject target = null;
			public GameObject adjustedTarget = null;
			public GameObject plane = null;
			public GameObject planeAvoid = null;
			public EyesModifier modifier = null;
			public EyesTargetContainer t;
			private float alpha_ = 1;
			private static float AlphaFactor = 3.3f;
			private static float DefaultSphereScaleF = 0.1f;
			private static Vector3 DefaultSphereScale = new Vector3(
				DefaultSphereScaleF, DefaultSphereScaleF, DefaultSphereScaleF);

			public Preview(EyesModifier m, EyesTargetContainer tc, float alpha)
			{
				modifier = m;
				t = tc;
				alpha_ = alpha * AlphaFactor;
				Create();
			}

			private void Create()
			{
				Destroy();

				if (t.Target == null)
					return;

				target = CreateObject(
					PrimitiveType.Sphere, GetColor(t.Target, 0.5f));

				target.transform.localPosition = t.Target.Position;
				target.transform.localScale = DefaultSphereScale;

				adjustedTarget = CreateObject(
					PrimitiveType.Sphere, GetColor(t.Target, 0.1f));

				adjustedTarget.transform.localPosition =
					modifier.AdjustedPosition(t.Target.Position);
				adjustedTarget.transform.localScale = DefaultSphereScale / 2;

				if (t.Target is RandomEyesTarget)
				{
					plane = CreateObject(
						PrimitiveType.Cube, GetColor(t.Target, 0.02f));

					planeAvoid = CreateObject(
						PrimitiveType.Cube, new Color(1, 0, 0, 0.1f));
				}
			}

			public void SetAlpha(float f)
			{
				alpha_ = f * AlphaFactor;

				if (target != null)
					SetAlpha(target, GetColor(t.Target, 0.5f));

				if (adjustedTarget != null)
					SetAlpha(adjustedTarget, GetColor(t.Target, 0.1f));

				if (t.Target is RandomEyesTarget)
				{
					if (plane != null)
						SetAlpha(plane, GetColor(t.Target, 0.02f));

					if (planeAvoid != null)
						SetAlpha(planeAvoid, GetColor(t.Target, 0.1f));
				}
			}

			public void Destroy()
			{
				if (target != null)
				{
					Object.Destroy(target);
					target = null;
				}

				if (adjustedTarget != null)
				{
					Object.Destroy(adjustedTarget);
					adjustedTarget = null;
				}

				if (plane != null)
				{
					Object.Destroy(plane);
					plane = null;
				}

				if (planeAvoid != null)
				{
					Object.Destroy(planeAvoid);
					planeAvoid = null;
				}
			}

			private GameObject CreateObject(PrimitiveType t, Color c)
			{
				var o = GameObject.CreatePrimitive(t);

				foreach (var collider in o.GetComponents<Collider>())
				{
					collider.enabled = false;
					Object.Destroy(collider);
				}

				var material = new Material(Shader.Find("Battlehub/RTGizmos/Handles"));

				material.color = c;
				material.SetFloat("_Offset", 1f);
				material.SetFloat("_MinAlpha", 1f);

				var r = o.GetComponent<Renderer>();
				r.material = material;

				o.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

				return o;
			}

			private void SetAlpha(GameObject o, Color c)
			{
				var r = o.GetComponent<Renderer>();
				r.material.color = c;
			}

			private Color GetColor(IEyesTarget t, float a)
			{
				a = Utilities.Clamp(a * alpha_, 0, 1);

				if (t is RigidbodyEyesTarget)
					return new Color(1, 0, 0, a);
				else if (t is RandomEyesTarget)
					return new Color(0, 0, 1, a);
				else
					return new Color(1, 1, 1, a);
			}
		}


		private bool enabled_ = false;
		private float alpha_ = 0.3f;
		private EyesModifier modifier_ = null;
		private readonly List<Preview> previews_ = new List<Preview>();

		public EyesModifier Modifier
		{
			set
			{
				modifier_ = value;

				if (enabled_)
					Create();
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled_;
			}

			set
			{
				enabled_ = value;

				if (enabled_)
					Create();
				else
					Destroy();
			}
		}

		public float Alpha
		{
			get
			{
				return alpha_;
			}

			set
			{
				alpha_ = value;

				if (enabled_)
				{
					foreach (var p in previews_)
						p.SetAlpha(alpha_);
				}
			}
		}

		public void Create()
		{
			Destroy();

			if (modifier_ == null || !enabled_)
				return;

			foreach (var t in modifier_.Targets)
				previews_.Add(new Preview(modifier_, t, alpha_));
		}

		public void Destroy()
		{
			foreach (var o in previews_)
				o.Destroy();

			previews_.Clear();
		}

		public void Update()
		{
			if (modifier_ == null)
				return;

			foreach (var p in previews_)
			{
				if (p.t?.Target == null)
					continue;

				p.target.SetActive(p.t.Enabled);
				p.target.transform.localPosition = p.t.Target.Position;

				p.adjustedTarget.SetActive(p.t.Enabled);
				p.adjustedTarget.transform.localPosition =
					modifier_.AdjustedPosition(p.t.Target.Position);

				if (p.t.Target is RandomEyesTarget)
				{
					p.plane.SetActive(p.t.Enabled);
					p.planeAvoid.SetActive(p.t.Enabled);
					UpdateRandomPlane(p, p.t.Target as RandomEyesTarget);
				}
			}
		}

		private void UpdateRandomPlane(Preview p, RandomEyesTarget rt)
		{
			var rel = rt.RelativeTo ?? modifier_.Head;

			Vector3 fwd = rel.rotation * Vector3.forward;
			Vector3 ver = rel.rotation * Vector3.up;
			Vector3 hor = rel.rotation * Vector3.right;

			p.plane.transform.localPosition =
				rel.position +
				fwd * rt.Distance +
				ver * rt.CenterY +
				hor * rt.CenterX;

			p.plane.transform.localScale = new Vector3(
				rt.RangeX*2, rt.RangeY*2, 0.01f);

			p.plane.transform.localRotation = rel.rotation;

			if (rt.AvoidRangeX == 0 && rt.AvoidRangeY == 0)
			{
				p.planeAvoid.SetActive(false);
			}
			else
			{
				p.planeAvoid.SetActive(p.t.Enabled);

				p.planeAvoid.transform.localPosition =
					rel.position +
					fwd * rt.Distance +
					ver * rt.CenterY +
					hor * rt.CenterX;

				p.planeAvoid.transform.localScale = new Vector3(
					rt.AvoidRangeX * 2, rt.AvoidRangeY * 2, 0.01f);

				p.planeAvoid.transform.localRotation =
					Quaternion.LookRotation(fwd);
			}
		}
	}
}
