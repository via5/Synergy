using System.Collections.Generic;

namespace Synergy
{
	sealed class Manager : IJsonable
	{
		private List<Step> steps_ = new List<Step>();

		private readonly ExplicitHolder<IStepProgression> progression_ =
			new ExplicitHolder<IStepProgression>();


		public Manager()
		{
			StepProgression = new SequentialStepProgression();
		}

		public List<Step> Steps
		{
			get
			{
				return steps_;
			}
		}

		public IStepProgression StepProgression
		{
			get
			{
				return progression_.HeldValue;
			}

			set
			{
				progression_.HeldValue?.Removed();
				progression_.Set(value);

				if (progression_.HeldValue != null)
					progression_.HeldValue.ParentManager = this;
			}
		}

		public void Clear()
		{
			while (steps_.Count != 0)
				DeleteStep(steps_[0]);

			StepProgression = new SequentialStepProgression();
		}

		public Step CurrentStep
		{
			get
			{
				return StepProgression?.Current;
			}
		}

		public void AddStep(Step s = null)
		{
			InsertStep(steps_.Count, s);
		}

		public void InsertStep(int at, Step s = null)
		{
			if (s == null)
				s = new Step();

			steps_.Insert(at, s);
			StepProgression?.StepInserted(at, s);
			s.Added();
		}

		public void DeleteStep(Step s)
		{
			var i = steps_.IndexOf(s);
			steps_.Remove(s);
			StepProgression?.StepDeleted(i);

			s.Removed();
		}

		public Step GetStep(int i)
		{
			if (i < 0 || i >= steps_.Count)
				return null;

			return steps_[i];
		}

		public int IndexOfStep(Step s)
		{
			return steps_.IndexOf(s);
		}

		public bool IsOnlyEnabledStep(Step s)
		{
			foreach (var ss in steps_)
			{
				if (ss.Enabled && ss != s)
					return false;
			}

			return true;
		}

		public void ResetAllSteps()
		{
			foreach (var s in steps_)
				s.Reset();
		}

		public void Tick(float deltaTime)
		{
			StepProgression?.Tick(deltaTime);
		}

		public void Set()
		{
			var current = CurrentStep;

			foreach (var s in steps_)
			{
				bool paused = false;

				if (current != null && current != s)
					paused = true;

				s.Set(paused);
			}
		}

		public void LoadPreset(string path, int flags)
		{
			J.Node.SaveType = SaveTypes.Preset;

			var node = J.Node.Wrap(SuperController.singleton.LoadJSON(path));

			if (Bits.IsSet(flags, Utilities.FullPreset))
			{
				if (Bits.IsSet(flags, Utilities.PresetReplace))
				{
					FromJSON(node);
				}
				else if (Bits.IsSet(flags, Utilities.PresetAppend))
				{
					var m = new Manager();
					m.FromJSON(node);

					foreach (var s in m.Steps)
						AddStep(s);
				}
			}
			else if (Bits.IsSet(flags, Utilities.StepPreset))
			{
				if (Bits.IsSet(flags, Utilities.PresetReplace))
				{
					var s = Synergy.Instance.UI.CurrentStep;
					if (s == null)
					{
						Synergy.LogError("no current step");
						return;
					}

					s.FromJSON(node);
					s.Reset();
				}
				else if (Bits.IsSet(flags, Utilities.PresetAppend))
				{
					var s = new Step();
					s.FromJSON(node);
					AddStep(s);
					s.Reset();
				}
				else if (Bits.IsSet(flags, Utilities.PresetMerge))
				{
					var s = Synergy.Instance.UI.CurrentStep;
					if (s == null)
					{
						Synergy.LogError("no current step");
						return;
					}

					var newStep = new Step();
					newStep.FromJSON(node);

					foreach (var m in newStep.Modifiers)
						s.AddModifier(new ModifierContainer(m.Modifier));

					s.Reset();
				}
			}
			else if (Bits.IsSet(flags, Utilities.ModifierPreset))
			{
				if (Bits.IsSet(flags, Utilities.PresetReplace))
				{
					var m = Synergy.Instance.UI.CurrentModifier;
					if (m == null)
					{
						Synergy.LogError("no current modifier");
						return;
					}

					m.FromJSON(node);
				}
				else if (Bits.IsSet(flags, Utilities.PresetAppend))
				{
					var s = Synergy.Instance.UI.CurrentStep;
					if (s == null)
					{
						Synergy.LogError("no current step");
						return;
					}

					var m = new ModifierContainer();
					m.FromJSON(node);

					s.AddModifier(m);
				}
			}

			J.Node.SaveType = SaveTypes.None;
		}

		public void SavePreset(string path, int flags)
		{
			J.Node.SaveType = SaveTypes.Preset;

			if (Bits.IsSet(flags, Utilities.FullPreset))
			{
				var o = ToJSON() as J.Object;
				o.Add("version", Version.String);
				o.Save(path);
			}
			else if (Bits.IsSet(flags, Utilities.StepPreset))
			{
				var s = Synergy.Instance.UI.CurrentStep;
				if (s == null)
					return;

				var o = s.ToJSON() as J.Object;
				o.Add("version", Version.String);
				o.Save(path);
			}
			else if (Bits.IsSet(flags, Utilities.ModifierPreset))
			{
				var m = Synergy.Instance.UI.CurrentModifier;
				if (m == null)
					return;

				var o = m.ToJSON() as J.Object;
				o.Add("version", Version.String);
				o.Save(path);
			}

			J.Node.SaveType = SaveTypes.None;
		}

		public J.Node ToJSON()
		{
			var o = new J.Object();

			o.Add("steps", steps_);
			o.Add("progression", StepProgression);

			return o;
		}

		public bool FromJSON(J.Node n)
		{
			Clear();

			var o = n.AsObject("Manager");
			if (o == null)
				return false;

			o.Opt("steps", ref steps_);

			foreach (var s in steps_)
				s.Added();

			IStepProgression sp = null;
			o.Opt<StepProgressionFactory, IStepProgression>(
				"progression", ref sp);
			StepProgression = sp;

			return true;
		}
	}
}
