using System.Collections.Generic;

namespace Synergy
{
	using MorphProgressionTypeStringList = FactoryStringList<
		MorphProgressionFactory, IMorphProgression>;


	class MorphModifierUI : AtomModifierUI
	{
		class SelectedMorphWidget
		{
			private readonly MorphModifierUI ui_;
			private readonly SelectedMorph sm_;
			private readonly Collapsible collapsible_;
			private readonly Checkbox enabled_;
			private readonly ConfirmableButton remove_;
			private readonly MovementUI movementUI_;

			public SelectedMorphWidget(
				MorphModifierUI ui, SelectedMorph sm, int flags = 0)
			{
				ui_ = ui;
				sm_ = sm;

				collapsible_ = new Collapsible(sm_.DisplayName, null, flags);
				enabled_ = new Checkbox("Enabled", EnabledChanged, flags);
				remove_ = new ConfirmableButton("Remove", Remove, flags);
				movementUI_ = new MovementUI(flags);

				enabled_.Parameter = sm_.EnabledParameter;
				movementUI_.SetValue(sm_.Movement, sm_.PreferredRange);

				collapsible_.Add(enabled_);
				collapsible_.Add(remove_);

				foreach (var w in movementUI_.GetWidgets())
					collapsible_.Add(w);
				RenameCollapsible();
			}

			public Collapsible Collapsible
			{
				get { return collapsible_; }
			}

			public SelectedMorph SelectedMorph
			{
				get { return sm_; }
			}

			private void EnabledChanged(bool b)
			{
				if (sm_ != null)
				{
					sm_.Enabled = b;
					RenameCollapsible();
				}
			}

			private void Remove()
			{
				ui_.RemoveMorph(sm_);
			}

			private void RenameCollapsible()
			{
				if (sm_ == null)
					return;

				if (sm_.Enabled)
					collapsible_.Text = sm_.DisplayName;
				else
					collapsible_.Text = "(X) " + sm_.DisplayName;
			}
		}


		public override string ModifierType
		{
			get { return MorphModifier.FactoryTypeName; }
		}

		private MorphModifier modifier_ = null;
		private readonly MorphProgressionTypeStringList progressionType_;
		private IMorphProgressionUI progressionUI_ = null;
		private readonly Collapsible selectedMorphsCollapsible_;
		private readonly Button toggleAll_;
		private readonly List<SelectedMorphWidget> selectedMorphs_ =
			new List<SelectedMorphWidget>();
		private readonly MorphCheckboxes morphCheckboxes_;

		public MorphModifierUI(MainUI ui)
			: base(ui, Utilities.AtomHasMorphs)
		{
			progressionType_ = new MorphProgressionTypeStringList(
				"Morph progression", ProgressionTypeChanged, Widget.Right);

			selectedMorphsCollapsible_ = new Collapsible(
				"Selected morphs", null, Widget.Right);

			toggleAll_ = new Button("Toggle All", ToggleAll, Widget.Right);

			morphCheckboxes_ = new MorphCheckboxes(
				"Morphs", MorphAdded, MorphRemoved, Widget.Right);
		}

		public override void AddToTopUI(IModifier m)
		{
			base.AddToTopUI(m);

			var changed = (m != modifier_);
			modifier_ = m as MorphModifier;

			if (modifier_ == null)
			{
				progressionUI_ = null;
				return;
			}

			if (changed)
			{
				selectedMorphsCollapsible_.Clear();
				selectedMorphsCollapsible_.Add(toggleAll_);

				foreach (var sm in modifier_.Morphs)
					AddSelectedMorphUI(sm);
			}

			if (progressionUI_ == null ||
				progressionUI_.ProgressionType != modifier_.Progression.GetFactoryTypeName())
			{
				progressionUI_ = CreateMorphProgressionUI(modifier_.Progression);
			}

			progressionType_.Value = modifier_.Progression;
			morphCheckboxes_.Atom = modifier_.Atom;

			var dazmorphs = new List<DAZMorph>();
			foreach (var sm in modifier_.Morphs)
				dazmorphs.Add(sm.Morph);
			morphCheckboxes_.Morphs = dazmorphs;

			AddAtomWidgets(m);

			widgets_.AddToUI(progressionType_);
			if (progressionUI_ != null)
				progressionUI_.AddToUI(modifier_.Progression);

			widgets_.AddToUI(new SmallSpacer(Widget.Right));

			widgets_.AddToUI(selectedMorphsCollapsible_);

			widgets_.AddToUI(new SmallSpacer(Widget.Right));
			widgets_.AddToUI(morphCheckboxes_);
		}

		public override void RemoveFromUI()
		{
			widgets_.RemoveFromUI();

			if (progressionUI_ != null)
				progressionUI_.RemoveFromUI();
		}

		public void RemoveMorph(SelectedMorph sm)
		{
			if (modifier_ == null)
				return;

			modifier_.RemoveMorph(sm.Morph);

			for (int i = 0; i < selectedMorphs_.Count; ++i)
			{
				if (selectedMorphs_[i].SelectedMorph == sm)
				{
					selectedMorphsCollapsible_.Remove(
						selectedMorphs_[i].Collapsible);

					selectedMorphs_.RemoveAt(i);

					if (selectedMorphsCollapsible_.Expanded)
						ui_.NeedsReset("selected morph removed");

					return;
				}
			}

			Synergy.LogError(
				"can't remove morph " + sm.Morph.displayName + ", " +
				"not in list");
		}

		private IMorphProgressionUI CreateMorphProgressionUI(
			IMorphProgression p)
		{
			if (p is NaturalMorphProgression)
				return new NaturalMorphProgressionUI(Widget.Right);
			else if (p is SequentialMorphProgression)
				return new SequentialMorphProgressionUI(Widget.Right);
			else if (p is RandomMorphProgression)
				return new RandomMorphProgressionUI(Widget.Right);
			else
				return null;
		}

		protected override void AtomChanged(Atom atom)
		{
			base.AtomChanged(atom);

			morphCheckboxes_.ChangeAtomKeepMorphs(atom);
			ui_.NeedsReset("atom changed on morph modifier");
		}

		private void ProgressionTypeChanged(IMorphProgression p)
		{
			if (p != null && modifier_ != null)
			{
				modifier_.Progression = p;
				ui_.NeedsReset("morph progression type changed");
			}
		}

		private void AddSelectedMorphUI(SelectedMorph sm)
		{
			var smw = new SelectedMorphWidget(this, sm, Widget.Right);

			selectedMorphs_.Add(smw);
			selectedMorphsCollapsible_.Add(smw.Collapsible);
		}

		private void ToggleAll()
		{
			if (selectedMorphs_.Count == 0)
				return;

			bool b = !selectedMorphs_[0].Collapsible.Expanded;

			foreach (var sm in selectedMorphs_)
				sm.Collapsible.SetExpanded(b);
		}

		private void MorphAdded(DAZMorph m)
		{
			if (modifier_ == null)
				return;

			var sm = modifier_.AddMorph(m);
			AddSelectedMorphUI(sm);

			if (selectedMorphsCollapsible_.Expanded)
				ui_.NeedsReset("selected morph added");
		}

		private void MorphRemoved(DAZMorph m)
		{
			if (modifier_ == null)
				return;

			for (int i = 0; i < selectedMorphs_.Count; ++i)
			{
				if (selectedMorphs_[i].SelectedMorph.Morph == m)
				{
					RemoveMorph(selectedMorphs_[i].SelectedMorph);
					return;
				}
			}

			Synergy.LogError(
				"can't remove morph " + m.displayName + ", not in list");
		}

		private void DisableAll()
		{
			morphCheckboxes_.DisableAll();
		}
	}


	interface IMorphProgressionUI
	{
		string ProgressionType { get; }
		void AddToUI(IMorphProgression p);
		void RemoveFromUI();
	}

	abstract class BasicMorphProgressionUI : IMorphProgressionUI
	{
		public abstract string ProgressionType { get; }

		protected readonly int flags_;
		protected readonly WidgetList widgets_ = new WidgetList();

		protected BasicMorphProgressionUI(int flags)
		{
			flags_ = flags;
		}

		public virtual void AddToUI(IMorphProgression m)
		{
			// no-op
		}

		public virtual void RemoveFromUI()
		{
			widgets_.RemoveFromUI();
		}
	}


	class NaturalMorphProgressionUI : BasicMorphProgressionUI
	{
		public override string ProgressionType
		{
			get { return NaturalMorphProgression.FactoryTypeName; }
		}

		private NaturalMorphProgression progression_ = null;

		private readonly Collapsible durationCollapsible_;
		private readonly DurationWidgets durationWidgets_;

		private readonly Collapsible delayCollapsible_;
		private readonly DelayWidgets delayWidgets_;


		public NaturalMorphProgressionUI(int flags)
			: base(flags)
		{
			durationCollapsible_ = new Collapsible(
				"Natural duration", null, flags_);

			durationWidgets_ = new DurationWidgets(
				"Natural", DurationTypeChanged, flags_);

			delayCollapsible_ = new Collapsible("Natural delay", null, flags_);
			delayWidgets_ = new DelayWidgets(flags_);
			delayWidgets_.SupportsHalfMove = false;
		}

		public override void AddToUI(IMorphProgression p)
		{
			base.AddToUI(p);

			progression_ = p as NaturalMorphProgression;
			if (progression_ == null)
				return;

			durationWidgets_.SetValue(progression_.Duration);
			delayWidgets_.SetValue(progression_.Delay);

			durationCollapsible_.Clear();
			durationCollapsible_.Add(durationWidgets_.GetWidgets());

			delayCollapsible_.Clear();
			delayCollapsible_.Add(delayWidgets_.GetWidgets());

			widgets_.AddToUI(durationCollapsible_);
			widgets_.AddToUI(delayCollapsible_);
		}

		private void DurationTypeChanged(IDuration d)
		{
			if (progression_ == null)
				return;

			progression_.Duration = d;
			Synergy.Instance.UI.NeedsReset("morph natpro duration changed");
		}
	}


	abstract class OrderedMorphProgressionUI : BasicMorphProgressionUI
	{
		private OrderedMorphProgression progression_ = null;
		private readonly Checkbox overrideOverlapTime_;
		private readonly FloatSlider overlapTime_;
		private readonly Checkbox holdHalfway_;


		public OrderedMorphProgressionUI(int flags)
			: base(flags)
		{
			holdHalfway_ = new Checkbox(
				"Hold halfway", HoldHalfwayChanged, flags);

			overrideOverlapTime_ = new Checkbox(
				"Override global overlap time",
				OverrideOverlapTimeChanged, flags);

			overlapTime_ = new FloatSlider(
				"Overlap time", OverlapTimeChanged, flags);
		}

		public override void AddToUI(IMorphProgression p)
		{
			base.AddToUI(p);

			progression_ = p as OrderedMorphProgression;
			if (progression_ == null)
				return;

			holdHalfway_.Parameter = progression_.HoldHalfwayParameter;

			overrideOverlapTime_.Value = progression_.OverrideOverlapTime;
			overlapTime_.Value = progression_.OverlapTime;
			overlapTime_.Enabled = progression_.OverrideOverlapTime;

			widgets_.AddToUI(holdHalfway_);
			//widgets_.AddToUI(overrideOverlapTime_);
			//widgets_.AddToUI(overlapTime_);
		}

		private void HoldHalfwayChanged(bool b)
		{
			if (progression_ == null)
				return;

			progression_.HoldHalfway = b;
		}

		private void OverrideOverlapTimeChanged(bool b)
		{
			if (progression_ == null)
				return;

			if (b)
				progression_.OverlapTime = Synergy.Instance.Options.OverlapTime;
			else
				progression_.OverlapTime = -1;

			overlapTime_.Enabled = b;
		}

		private void OverlapTimeChanged(float f)
		{
			if (progression_ == null)
				return;

			progression_.OverlapTime = f;
		}
	}


	class SequentialMorphProgressionUI : OrderedMorphProgressionUI
	{
		public override string ProgressionType
		{
			get { return SequentialMorphProgression.FactoryTypeName; }
		}

		public SequentialMorphProgressionUI(int flags)
			: base(flags)
		{
		}
	}


	class RandomMorphProgressionUI : OrderedMorphProgressionUI
	{
		public override string ProgressionType
		{
			get { return RandomMorphProgression.FactoryTypeName; }
		}

		public RandomMorphProgressionUI(int flags)
			: base(flags)
		{
		}
	}
}
