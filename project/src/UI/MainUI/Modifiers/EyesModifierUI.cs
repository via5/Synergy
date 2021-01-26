using System;
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
			//if (previews_.Enabled)
			//	previews_.Update();
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



	interface IEyesPreviewTarget
	{
		void Destroy();
		void Set(EyesModifier m, EyesTargetContainer tc, float alpha);
		void SetAlpha(float f);
		bool Accepts(IEyesTarget target);
		void Update();
	}


	public class EyesPreviewStyle
	{
		public static Color RigidbodyTargetColor = new Color(1, 0, 0, 1);
		public static Color RandomTargetColor = new Color(0, 0, 1, 1);
		public static Color RandomPlane = new Color(0, 0, 1, 1);
		public static Color RandomPlaneAvoid = new Color(1, 0, 1, 0.5f);
	}


	abstract class BasicEyesPreviewTarget : IEyesPreviewTarget
	{
		public static float DefaultSphereScaleF = 0.1f;

		public static Vector3 DefaultSphereScale = new Vector3(
			DefaultSphereScaleF, DefaultSphereScaleF, DefaultSphereScaleF);


		protected EyesModifier modifier_ = null;
		protected EyesTargetContainer target_;
		private float alpha_ = 0;

		protected BasicEyesPreviewTarget(
			EyesModifier m, EyesTargetContainer tc, float alpha)
		{
			modifier_ = m;
			target_ = tc;
			alpha_ = alpha;
			Create();
		}

		public static IEyesPreviewTarget Create(
			EyesModifier m, EyesTargetContainer tc, float alpha)
		{
			if (tc?.Target == null)
				return null;

			if (tc.Target is RigidbodyEyesTarget)
				return new EyesPreviewRigidbody(m, tc, alpha);
			else if (tc.Target is RandomEyesTarget)
				return new EyesPreviewRandom(m, tc, alpha);
			else if (tc.Target is PlayerEyesTarget)
				return new EyesPreviewPlayer(m, tc, alpha);

			Synergy.LogError("previews: unknown eyes target");
			return null;
		}

		public void Set(EyesModifier m, EyesTargetContainer tc, float alpha)
		{
			modifier_ = m;
			target_ = tc;
			alpha_ = alpha;
		}

		public void SetAlpha(float f)
		{
			alpha_ = f;
			UpdateAlpha();
		}

		private void Create()
		{
			Destroy();
			DoCreate();
			UpdateAlpha();
		}

		public void Destroy()
		{
			DoDestroy();
		}

		public abstract bool Accepts(IEyesTarget target);
		public abstract void Update();

		protected abstract void DoCreate();
		protected abstract void DoDestroy();
		protected abstract void UpdateAlpha();

		protected GameObject CreateObject(PrimitiveType t, Color c)
		{
			var o = GameObject.CreatePrimitive(t);

			foreach (var collider in o.GetComponents<Collider>())
			{
				collider.enabled = false;
				UnityEngine.Object.Destroy(collider);
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

		protected void SetAlpha(GameObject o, Color c, float baseAlpha)
		{
			c = new Color(c.r, c.g, c.b, baseAlpha * alpha_);

			var r = o.GetComponent<Renderer>();
			r.material.color = c;
		}
	}


	abstract class EyesPreviewWithTarget : BasicEyesPreviewTarget
	{
		private GameObject sphere_ = null;
		private GameObject adjustedSphere_ = null;
		private Color color_ = new Color();

		public EyesPreviewWithTarget(EyesModifier m, EyesTargetContainer tc, float alpha)
			: base(m, tc, alpha)
		{
		}

		public override void Update()
		{
			sphere_.SetActive(target_.Enabled);
			sphere_.transform.localPosition = target_.Target.Position;

			adjustedSphere_.SetActive(target_.Enabled);
			adjustedSphere_.transform.localPosition =
				modifier_.AdjustedPosition(target_.Target.Position);
		}

		protected void CreateTargets(Color color)
		{
			color_ = color;

			if (target_.Target == null)
				return;

			sphere_ = CreateObject(PrimitiveType.Sphere, color);

			sphere_.transform.localPosition = target_.Target.Position;
			sphere_.transform.localScale = DefaultSphereScale;


			adjustedSphere_ = CreateObject(PrimitiveType.Sphere, color);

			adjustedSphere_.transform.localPosition =
				modifier_.AdjustedPosition(target_.Target.Position);

			adjustedSphere_.transform.localScale = DefaultSphereScale / 2;
		}

		protected override void UpdateAlpha()
		{
			if (sphere_ != null)
				SetAlpha(sphere_, color_, 1);

			if (adjustedSphere_ != null)
				SetAlpha(adjustedSphere_, color_, 0.5f);
		}

		protected override void DoDestroy()
		{
			if (sphere_ != null)
			{
				UnityEngine.Object.Destroy(sphere_);
				sphere_ = null;
			}

			if (adjustedSphere_ != null)
			{
				UnityEngine.Object.Destroy(adjustedSphere_);
				adjustedSphere_ = null;
			}
		}
	}


	class EyesPreviewRigidbody : EyesPreviewWithTarget
	{
		public EyesPreviewRigidbody(EyesModifier m, EyesTargetContainer tc, float alpha)
			: base(m, tc, alpha)
		{
		}

		public override bool Accepts(IEyesTarget target)
		{
			return (target is RigidbodyEyesTarget);
		}

		protected override void DoCreate()
		{
			CreateTargets(EyesPreviewStyle.RigidbodyTargetColor);
		}
	}


	class EyesPreviewRandom : EyesPreviewWithTarget
	{
		public GameObject plane_ = null;
		public GameObject planeAvoid_ = null;

		public EyesPreviewRandom(EyesModifier m, EyesTargetContainer tc, float alpha)
			: base(m, tc, alpha)
		{
		}

		public override bool Accepts(IEyesTarget target)
		{
			return (target is RandomEyesTarget);
		}

		public override void Update()
		{
			base.Update();

			plane_.SetActive(target_.Enabled);
			planeAvoid_.SetActive(target_.Enabled);

			var rt = target_.Target as RandomEyesTarget;

			var rel = rt.RelativeTo ?? modifier_.Head;

			Vector3 fwd = rel.rotation * Vector3.forward;
			Vector3 ver = rel.rotation * Vector3.up;
			Vector3 hor = rel.rotation * Vector3.right;

			plane_.transform.localPosition =
				rel.position +
				fwd * rt.Distance +
				ver * rt.CenterY +
				hor * rt.CenterX;

			plane_.transform.localScale = new Vector3(
				rt.RangeX * 2, rt.RangeY * 2, 0.01f);

			plane_.transform.localRotation = rel.rotation;

			if (rt.AvoidRangeX == 0 && rt.AvoidRangeY == 0)
			{
				planeAvoid_.SetActive(false);
			}
			else
			{
				planeAvoid_.SetActive(target_.Enabled);

				planeAvoid_.transform.localPosition =
					rel.position +
					fwd * rt.Distance +
					ver * rt.CenterY +
					hor * rt.CenterX;

				planeAvoid_.transform.localScale = new Vector3(
					rt.AvoidRangeX * 2, rt.AvoidRangeY * 2, 0.01f);

				planeAvoid_.transform.localRotation =
					Quaternion.LookRotation(fwd);
			}
		}

		protected override void DoCreate()
		{
			CreateTargets(EyesPreviewStyle.RigidbodyTargetColor);

			plane_ = CreateObject(
				PrimitiveType.Cube, EyesPreviewStyle.RandomPlane);

			planeAvoid_ = CreateObject(
				PrimitiveType.Cube,
				EyesPreviewStyle.RandomPlaneAvoid);
		}

		protected override void DoDestroy()
		{
			base.DoDestroy();

			if (plane_ != null)
			{
				UnityEngine.Object.Destroy(plane_);
				plane_ = null;
			}

			if (planeAvoid_ != null)
			{
				UnityEngine.Object.Destroy(planeAvoid_);
				planeAvoid_ = null;
			}
		}

		protected override void UpdateAlpha()
		{
			base.UpdateAlpha();

			if (plane_ != null)
				SetAlpha(plane_, EyesPreviewStyle.RandomPlane, 1);

			if (planeAvoid_ != null)
				SetAlpha(planeAvoid_, EyesPreviewStyle.RandomPlaneAvoid, 1);
		}
	}


	class EyesPreviewPlayer : BasicEyesPreviewTarget
	{
		public EyesPreviewPlayer(EyesModifier m, EyesTargetContainer tc, float alpha)
			: base(m, tc, alpha)
		{
		}

		public override bool Accepts(IEyesTarget target)
		{
			return (target is PlayerEyesTarget);
		}

		public override void Update()
		{
			// no-op
		}

		protected override void DoCreate()
		{
			// no-op
		}

		protected override void DoDestroy()
		{
			// no-op
		}

		protected override void UpdateAlpha()
		{
			// no-op
		}
	}


	class EyesPreviewContainer
	{
		private IEyesPreviewTarget preview_ = null;

		public void SetAlpha(float f)
		{
			if (preview_ != null)
				preview_.SetAlpha(f);
		}

		public void Destroy()
		{
			if (preview_ != null)
				preview_.Destroy();
		}

		public void Update()
		{
			if (preview_ != null)
				preview_.Update();
		}

		public void Set(EyesModifier modifier, EyesTargetContainer target, float alpha)
		{
			if (preview_ == null)
			{
				preview_ = BasicEyesPreviewTarget.Create(modifier, target, alpha);
			}
			else if (modifier == null || target == null)
			{
				preview_.Destroy();
				preview_ = null;
			}
			else if (!preview_.Accepts(target.Target))
			{
				preview_.Destroy();
				preview_ = BasicEyesPreviewTarget.Create(modifier, target, alpha);
			}
			else
			{
				preview_.Set(modifier, target, alpha);
			}
		}
	}


	class EyesPreviews : IDisposable
	{

		private bool enabled_ = false;
		private float alpha_ = 0.3f;
		private EyesModifier modifier_ = null;
		private readonly List<EyesPreviewContainer> previews_ =
			new List<EyesPreviewContainer>();
		private Timer timer_ = null;

		public EyesPreviews()
		{
			Synergy.Instance.PluginStateChanged += OnPluginStateChanged;
		}

		public void Dispose()
		{
			Synergy.Instance.PluginStateChanged -= OnPluginStateChanged;
		}

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

			timer_ = Synergy.Instance.CreateTimer(
				0.1f, () => Update(), Timer.Repeat);

			foreach (var t in modifier_.Targets)
				previews_.Add(new EyesPreviewContainer());

			UpdateTargets();
		}

		public void Destroy()
		{
			foreach (var o in previews_)
				o.Destroy();

			previews_.Clear();

			if (timer_ != null)
				timer_.Destroy();
		}

		public void Update()
		{
			if (modifier_ == null)
				return;

			if (!UpdateTargets())
				Create();
		}

		private bool UpdateTargets()
		{
			if (previews_.Count != modifier_.Targets.Count)
				return false;

			bool okay = true;

			for (int i=0; i<previews_.Count; ++i)
			{
				var preview = previews_[i];
				var target = modifier_.Targets[i];

				preview.Set(modifier_, target, alpha_);
				preview.Update();
			}

			return okay;
		}

		private void OnPluginStateChanged(bool b)
		{
			if (!Enabled)
				return;

			if (b)
				Create();
			else
				Destroy();
		}
	}
}
