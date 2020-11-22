using System.Collections.Generic;
using System;
using System.Linq;

namespace Synergy
{
	interface IStepProgression : IFactoryObject
	{
		Manager ParentManager { get; set; }
		Step Current { get; }
		void Tick(float deltaTime);
		void Removed();
		void StepInserted(int at, Step s);
		void StepDeleted(int at);
		bool IsStepRunning(Step s);
		bool IsStepActive(Step s);
		void ForceRun(Step s);
	}

	sealed class StepProgressionFactory : BasicFactory<IStepProgression>
	{
		public override List<IStepProgression> GetAllObjects()
		{
			return new List<IStepProgression>()
			{
				new SequentialStepProgression(),
				new RandomStepProgression(),
				new ConcurrentStepProgression()
			};
		}
	}

	abstract class BasicStepProgression : IStepProgression
	{
		public abstract string GetFactoryTypeName();
		public abstract string GetDisplayName();

		protected Manager manager_ = null;

		public Manager ParentManager
		{
			get
			{
				return manager_;
			}

			set
			{
				manager_ = value;

				if (manager_ != null)
					StepsChanged();
			}
		}


		public abstract Step Current { get; }
		public abstract void Tick(float deltaTime);

		public abstract bool IsStepRunning(Step s);
		public abstract bool IsStepActive(Step s);

		public virtual void Removed()
		{
			ParentManager = null;
		}

		public virtual void StepInserted(int at, Step s)
		{
			s.Reset();
		}

		public virtual void StepDeleted(int at)
		{
			// no-op
		}

		protected virtual void StepsChanged()
		{
			foreach (var s in manager_.Steps)
				s.Reset();
		}

		public virtual void ForceRun(Step s)
		{
			// no-op
		}

		protected List<Step> Steps
		{
			get { return manager_.Steps; }
		}

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
	}


	abstract class OrderedStepProgression : BasicStepProgression
	{
		private Overlapper o_ = new Overlapper("step");

		public OrderedStepProgression()
		{
			o_.CanRun += i => Steps[i].Enabled;
			o_.Resume += (i) => { Steps[i].Resume(); return true;  };
			o_.CanRunBackwards += i => Steps[i].HalfMove;
			o_.Reset += (i) => { Steps[i].Reset(); return true; };

			o_.Ticker += (int i, float deltaTime, bool stepForwards, bool paused) =>
			{
				if (paused)
				{
					Steps[i].TickPaused(deltaTime);
					return true;
				}
				else
				{
					return Steps[i].Tick(deltaTime, stepForwards);
				}
			};

			o_.TimeRemaining += i => Steps[i].TimeRemainingInDirection;
			o_.Regenerate += (old) => Regenerate(old, Steps.Count);
			o_.ItemCount += () => Steps.Count;
			o_.GetOverlapTime += () => Synergy.Instance.Options.OverlapTime;
		}

		public override Step Current
		{
			get
			{
				var ri = o_.CurrentIndex;

				if (ri < 0 || ri >= Steps.Count)
					return null;
				else
					return Steps[ri];
			}
		}

		public Overlapper Overlapper
		{
			get { return o_; }
		}

		public override void ForceRun(Step s)
		{
			o_.ForceRun(Steps.IndexOf(s));
			s.Reset();
		}

		public override void Tick(float deltaTime)
		{
			float OverlapTime = Synergy.Instance.Options.OverlapTime;

			o_.Tick(deltaTime);
		}

		public override void StepInserted(int at, Step s)
		{
			base.StepInserted(at, s);
			o_.ItemInserted(at);
		}

		public override void StepDeleted(int stepIndex)
		{
			base.StepDeleted(stepIndex);
			o_.ItemDeleted(stepIndex);
		}

		public override bool IsStepRunning(Step s)
		{
			return o_.IsRunning(Steps.IndexOf(s));
		}

		public override bool IsStepActive(Step s)
		{
			return o_.IsActive(Steps.IndexOf(s));
		}

		protected override void StepsChanged()
		{
			base.StepsChanged();
			o_.ItemsChanged();
		}

		protected abstract List<int> Regenerate(List<int> old, int count);

		public override J.Node ToJSON()
		{
			return new J.Object();
		}

		public override bool FromJSON(J.Node n)
		{
			// no-op
			return true;
		}
	}

	sealed class SequentialStepProgression : OrderedStepProgression
	{
		public static string FactoryTypeName { get; } = "sequential";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Sequential";
		public override string GetDisplayName() { return DisplayName; }

		protected override List<int> Regenerate(List<int> old, int count)
		{
			var list = new List<int>();

			for (int i = 0; i < count; ++i)
				list.Add(i);

			return list;
		}
	}

	sealed class RandomStepProgression : OrderedStepProgression
	{
		public static string FactoryTypeName { get; } = "random";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random";
		public override string GetDisplayName() { return DisplayName; }

		protected override List<int> Regenerate(List<int> old, int count)
		{
			return ShuffledOrder.Shuffle(old, count);
		}
	}

	sealed class ConcurrentStepProgression : BasicStepProgression
	{
		public static string FactoryTypeName { get; } = "concurrent";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Concurrent";
		public override string GetDisplayName() { return DisplayName; }

		public override Step Current
		{
			get { return null; }
		}

		public override void Tick(float deltaTime)
		{
			foreach (var s in Steps)
			{
				if (s.Enabled)
				{
					if (!s.Tick(deltaTime, true))
						s.Resume();
				}
			}
		}

		public override void StepInserted(int at, Step s)
		{
			base.StepInserted(at, s);
			s.Resume();
		}

		protected override void StepsChanged()
		{
			base.StepsChanged();

			foreach (var s in Steps)
				s.Resume();
		}

		public override bool IsStepRunning(Step s)
		{
			return true;
		}

		public override bool IsStepActive(Step s)
		{
			return true;
		}

		public override J.Node ToJSON()
		{
			return new J.Object();
		}

		public override bool FromJSON(J.Node n)
		{
			// no-op
			return true;
		}
	}
}
