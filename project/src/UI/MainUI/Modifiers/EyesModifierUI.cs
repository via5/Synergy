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

		public BasicEyesModifierTargetUI(EyesModifierTargetUIContainer parent)
		{
			parent_ = parent;
		}


		public abstract List<Widget> GetWidgets();
	}


	class RigidbodyEyesTargetUI : BasicEyesModifierTargetUI
	{
		private RigidbodyEyesTarget target_ = null;

		private readonly AtomList atom_;
		private readonly ForceReceiverList receiver_;

		public RigidbodyEyesTargetUI(
			EyesModifierTargetUIContainer parent, RigidbodyEyesTarget t)
				: base(parent)
		{
			target_ = t;

			atom_ = new AtomList(
				"Atom", target_?.Atom?.uid, AtomChanged,
				null, Widget.Right);

			receiver_ = new ForceReceiverList(
				"Receiver", target_?.Receiver?.name,
				ReceiverChanged, Widget.Right);
		}

		public override List<Widget> GetWidgets()
		{
			return new List<Widget>()
			{
				atom_, receiver_
			};
		}

		private void AtomChanged(Atom a)
		{
			target_.Atom = a;
			receiver_.Atom = a;

			if (target_.Receiver == null)
			{
				var pt = EyesModifier.GetPreferredTarget(a);

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
	}


	class ConstantEyesTargetUI : BasicEyesModifierTargetUI
	{
		public ConstantEyesTargetUI(
			EyesModifierTargetUIContainer parent, ConstantEyesTarget t)
				: base(parent)
		{
		}

		public override List<Widget> GetWidgets()
		{
			return new List<Widget>()
			{
			};
		}
	}


	class RandomEyesTargetUI : BasicEyesModifierTargetUI
	{
		public RandomEyesTargetUI(
			EyesModifierTargetUIContainer parent, RandomEyesTarget t)
				: base(parent)
		{
		}

		public override List<Widget> GetWidgets()
		{
			return new List<Widget>()
			{
			};
		}
	}


	class EyesModifierTargetUIContainer
	{
		private readonly Collapsible collapsible_;

		private EyesTargetContainer container_ = null;
		private readonly
			FactoryStringList<EyesTargetFactory, IEyesTarget> types_;

		private IEyesModifierTargetUI ui_ = null;
		private bool stale_ = true;


		public EyesModifierTargetUIContainer(EyesTargetContainer t)
		{
			container_ = t;

			types_ = new FactoryStringList<EyesTargetFactory, IEyesTarget>(
				"Type", TypeChanged, Widget.Right);

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
			collapsible_.Add(types_);

			var t = container_.Target;

			types_.Value = t;

			if (t is RigidbodyEyesTarget)
				ui_ = new RigidbodyEyesTargetUI(this, t as RigidbodyEyesTarget);
			else if (t is ConstantEyesTarget)
				ui_ = new ConstantEyesTargetUI(this, t as ConstantEyesTarget);
			else if (t is RandomEyesTarget)
				ui_ = new RandomEyesTargetUI(this, t as RandomEyesTarget);
			else
				ui_ = null;

			if (ui_ != null)
			{
				foreach (var w in ui_.GetWidgets())
					collapsible_.Add(w);
			}
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

		private readonly Button addTarget_;
		private readonly List<EyesModifierTargetUIContainer> targets_ =
			new List<EyesModifierTargetUIContainer>();


		public EyesModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasEyes)
		{
			saccade_ = new Collapsible("Saccade", null, Widget.Right);

			saccadeTime_ = new RandomizableTimeWidgets(
				"Saccade interval", Widget.Right);

			saccadeMin_ = new FloatSlider(
				"Saccade minimum", SaccadeMinChanged, Widget.Right);

			saccadeMax_ = new FloatSlider(
				"Saccade maximum", SaccadeMaxChanged, Widget.Right);

			minDistance_ = new FloatSlider(
				"Minimum distance (avoids cross-eyed)",
				MinDistanceChanged, Widget.Right);

			addTarget_ = new Button("Add target", AddTarget, Widget.Right);

			foreach (var w in saccadeTime_.GetWidgets())
				saccade_.Add(w);

			saccade_.Add(saccadeMin_);
			saccade_.Add(saccadeMax_);
		}

		public override void AddToTopUI(IModifier m)
		{
			var changed = (m != modifier_);

			modifier_ = m as EyesModifier;
			if (modifier_ == null)
				return;

			if (changed)
			{
				targets_.Clear();

				foreach (var t in modifier_.Targets)
					targets_.Add(new EyesModifierTargetUIContainer(t));
			}


			saccadeTime_.SetValue(
				modifier_.SaccadeTime, new FloatRange(0, 5));
			saccadeMin_.Parameter = modifier_.SaccadeMinParameter;
			saccadeMax_.Parameter = modifier_.SaccadeMaxParameter;
			minDistance_.Parameter = modifier_.MinDistanceParameter;

			AddAtomWidgets(m);
			widgets_.AddToUI(saccade_);
			widgets_.AddToUI(minDistance_);
			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			widgets_.AddToUI(addTarget_);

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

		private void AddTarget()
		{
			if (modifier_ == null)
				return;

			var t = modifier_.AddTarget();
			targets_.Add(new EyesModifierTargetUIContainer(t));

			Synergy.Instance.UI.NeedsReset("eyes target added");
		}
	}
}
