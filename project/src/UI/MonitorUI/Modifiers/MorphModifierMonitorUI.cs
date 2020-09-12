using System.Collections.Generic;

namespace Synergy
{
	class MorphModifierMonitor : BasicModifierMonitor
	{
		public override string ModifierType
		{
			get { return MorphModifier.FactoryTypeName; }
		}

		class MorphMonitor
		{
			public SelectedMorph sm;
			public Collapsible collapsible;
			public MovementMonitorWidgets movement;
			public FloatSlider timeRemaining;
			public FloatSlider progress;
			public DurationMonitorWidgets duration;
			public DelayMonitor delay;
			public Checkbox stopping;
			public Checkbox finished;
			public FloatSlider customTargetTime;
			public FloatSlider customTargetMovePerTick;
			public FloatSlider customTargetValue;

			public MorphMonitor()
			{
			}

			public void Update(MorphModifier m, int morphIndex)
			{
				movement.Update(sm.Movement);

				if (m.Progression is OrderedMorphProgression)
				{
					var omp = m.Progression as OrderedMorphProgression;
					timeRemaining.Value = omp.GetTimeRemainingForMorph(morphIndex);
					progress.Value = omp.GetProgressForMorph(morphIndex);
				}

				// todo
				var natpro = m.Progression as NaturalMorphProgression;

				if (natpro != null)
				{
					var mi = natpro.GetMorphInfoFor(sm);
					if (mi != null)
					{
						duration.Update();
						delay.Update();

						stopping.Value = mi.stopping;
						finished.Value = mi.finished;

						if (mi.target == null)
						{
							customTargetTime.Value = -1;
							customTargetMovePerTick.Value = -1;
							customTargetValue.Value = -1;
						}
						else
						{
							customTargetTime.Value = mi.target.time;
							customTargetMovePerTick.Value = mi.target.movePerTick;
							customTargetValue.Value = mi.target.value;
						}

						return;
					}
				}
			}
		}

		private MorphModifier modifier_ = null;
		private Checkbox natProStop_ = null;
		private readonly OverlapMonitorUI overlap_;
		private readonly List<MorphMonitor> morphs_ = new List<MorphMonitor>();

		public MorphModifierMonitor()
		{
			overlap_ = new OverlapMonitorUI(Widget.Right);
		}

		public override void AddToUI(IModifier m)
		{
			base.AddToUI(m);

			var changed = (modifier_ != m);

			modifier_ = m as MorphModifier;
			if (modifier_ == null)
				return;

			// todo
			var natpro = modifier_.Progression as NaturalMorphProgression;
			if (natpro == null)
				natProStop_ = null;
			else
				natProStop_ = new Checkbox("Natural progression stop", null, Widget.Right);

			if (modifier_.Progression is OrderedMorphProgression)
			{
				foreach (var w in overlap_.GetWidgets())
					widgets_.AddToUI(w);
			}

			if (natProStop_ != null)
				widgets_.AddToUI(natProStop_);

			if (modifier_.Morphs.Count == 0)
			{
				widgets_.AddToUI(new Header("No morphs", Widget.Right));
			}
			else if (changed)
			{
				morphs_.Clear();

				for (int i=0; i<modifier_.Morphs.Count; ++i)
					morphs_.Add(CreateMorphMonitor(i));
			}

			foreach (var mm in morphs_)
				widgets_.AddToUI(mm.collapsible);
		}

		public override void Update()
		{
			base.Update();

			if (modifier_ == null)
				return;

			// todo
			var natpro = modifier_.Progression as NaturalMorphProgression;
			if (natpro != null)
				natProStop_.Value = natpro.Stopping;

			overlap_.Update(
				(modifier_.Progression as OrderedMorphProgression)
					?.Overlapper);

			int morphIndex = 0;
			foreach (var mm in morphs_)
			{
				if (mm.sm.Enabled)
				{
					mm.Update(modifier_, morphIndex);
					++morphIndex;
				}
			}
		}

		private MorphMonitor CreateMorphMonitor(int morphIndex)
		{
			SelectedMorph sm = modifier_.Morphs[morphIndex];
			var mm = new MorphMonitor();

			mm.sm = sm;

			mm.collapsible = new Collapsible(sm.DisplayName, null, Widget.Right);
			mm.movement = new MovementMonitorWidgets(Widget.Right);

			mm.collapsible.Add(mm.movement.GetWidgets());

			// todo
			var natpro = modifier_.Progression as NaturalMorphProgression;
			if (natpro != null)
			{
				var mi = natpro.GetMorphInfoFor(sm);
				if (mi != null)
				{
					mm.duration = new DurationMonitorWidgets("Duration", Widget.Right);
					mm.delay = new DelayMonitor(Widget.Right);
					mm.stopping = new Checkbox("Stopping", null, Widget.Right);
					mm.finished = new Checkbox("Finished", null, Widget.Right);
					mm.customTargetTime = new FloatSlider(
						"Custom target time", null, Widget.Right);
					mm.customTargetMovePerTick = new FloatSlider(
						"Custom target move per tick", null, Widget.Right);
					mm.customTargetValue = new FloatSlider(
						"Custom target value", null, Widget.Right);

					mm.collapsible.Add(mm.duration.GetWidgets(mi.duration));
					mm.collapsible.Add(mm.delay.GetWidgets(mi.delay));
					mm.collapsible.Add(mm.stopping);
					mm.collapsible.Add(mm.finished);
					mm.collapsible.Add(mm.customTargetTime);
					mm.collapsible.Add(mm.customTargetMovePerTick);
					mm.collapsible.Add(mm.customTargetValue);
				}
			}

			if (modifier_.Progression is OrderedMorphProgression)
			{
				var omp = modifier_.Progression as OrderedMorphProgression;

				mm.timeRemaining = new FloatSlider(
					"Time remaining", null, Widget.Right | Widget.Disabled);

				mm.progress = new FloatSlider(
					"Progress", null, Widget.Right | Widget.Disabled);

				mm.collapsible.Add(mm.timeRemaining);
				mm.collapsible.Add(mm.progress);
			}

			return mm;
		}
	}
}
