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
		void Next();
		void Removed();
		void StepInserted(int at, Step s);
		void StepDeleted(int at);
		bool IsStepActive(Step s);
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
		public abstract void Next();
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

		protected List<Step> Steps
		{
			get { return manager_.Steps; }
		}

		public abstract J.Node ToJSON();
		public abstract bool FromJSON(J.Node n);
	}


	abstract class OrderedStepProgression : BasicStepProgression
	{
		private int current_ = -1;
		private bool forwards_ = true;


		protected abstract int GetStepIndex(int i);
		protected abstract void AddToOrder(int stepIndex);
		protected abstract void RemoveFromOrder(int stepIndex);
		protected abstract void Reorder();


		public override Step Current
		{
			get
			{
				if (current_ < 0 || current_ >= Steps.Count)
					return null;

				return GetStep(current_);
			}
		}

		protected Step GetStep(int orderIndex)
		{
			if (orderIndex < 0 || orderIndex >= Steps.Count)
				return null;

			var stepIndex = GetStepIndex(orderIndex);

			if (stepIndex < 0 || stepIndex >= Steps.Count)
				return null;

			return Steps[stepIndex];
		}

		private bool CurrentStepValid()
		{
			var s = GetStep(current_);
			if (s == null)
				return false;

			return s.Enabled;
		}

		public override void Tick(float deltaTime)
		{
			if (!CurrentStepValid())
				Next();

			if (current_ == -1)
				return;

			for (int i = 0; i < Steps.Count; ++i)
			{
				if (!Steps[i].Enabled)
					continue;

				if (i == GetStepIndex(current_))
				{
					if (!Steps[i].Tick(deltaTime, forwards_))
						Next();
				}
				else
				{
					Steps[i].TickPaused(deltaTime);
				}
			}
		}

		public override void Next()
		{
			if (Steps.Count == 0)
			{
				current_ = -1;
				forwards_ = true;
				return;
			}

			int dirCount = 0;

			for (; ; )
			{
				int dir = (forwards_ ? +1 : -1);

				current_ += dir;

				if (current_ < 0 || current_ >= Steps.Count)
				{
					forwards_ = !forwards_;
					++dirCount;

					if (dirCount >= 2)
					{
						// went back and forth, nothing's enabled
						break;
					}

					if (forwards_)
						Reorder();
				}
				else
				{
					if (GetStep(current_).Enabled)
						break;
				}
			}

			if (current_ >= 0 && current_ < Steps.Count)
			{
				if (forwards_)
					GetStep(current_).Resume();
			}
			else
			{
				current_ = -1;
			}
		}

		public override void StepInserted(int at, Step s)
		{
			base.StepInserted(at, s);
			AddToOrder(Steps.Count - 1);
		}

		public override void StepDeleted(int stepIndex)
		{
			base.StepDeleted(stepIndex);

			bool deletingCurrent = (current_ == stepIndex);

			if (deletingCurrent)
				Next();

			RemoveFromOrder(stepIndex);

			if (deletingCurrent)
			{
				if (Steps.Count == 0)
					current_ = -1;
				else
					--current_;
			}
		}

		public override bool IsStepActive(Step s)
		{
			for (int i = 0; i <= current_; ++i)
			{
				if (GetStep(i) == s)
					return true;
			}

			return false;
		}

		protected override void StepsChanged()
		{
			base.StepsChanged();
			Reorder();
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

	sealed class SequentialStepProgression : OrderedStepProgression
	{
		public static string FactoryTypeName { get; } = "sequential";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Sequential";
		public override string GetDisplayName() { return DisplayName; }

		private List<int> order_ = new List<int>();

		protected override int GetStepIndex(int i)
		{
			return order_[i];
		}

		protected override void AddToOrder(int stepIndex)
		{
			order_.Add(stepIndex);
		}

		protected override void RemoveFromOrder(int stepIndex)
		{
			order_.Remove(stepIndex);

			for (int o = 0; o < order_.Count; ++o)
			{
				if (order_[o] > stepIndex)
					--order_[o];
			}
		}

		protected override void Reorder()
		{
			order_.Clear();

			for (int i = 0; i < Steps.Count; ++i)
				order_.Add(i);
		}
	}

	sealed class RandomStepProgression : OrderedStepProgression
	{
		public static string FactoryTypeName { get; } = "random";
		public override string GetFactoryTypeName() { return FactoryTypeName; }

		public static string DisplayName { get; } = "Random";
		public override string GetDisplayName() { return DisplayName; }

		private ShuffledOrder order_ = new ShuffledOrder();

		protected override int GetStepIndex(int i)
		{
			return order_.Get(i);
		}

		protected override void AddToOrder(int stepIndex)
		{
			order_.Add(stepIndex);
		}

		protected override void RemoveFromOrder(int stepIndex)
		{
			order_.Remove(stepIndex);
		}

		protected override void Reorder()
		{
			order_.Shuffle(Steps.Count);
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

		public override void Next()
		{
			// no-op
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
