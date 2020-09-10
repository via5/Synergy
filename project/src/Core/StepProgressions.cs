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
		private List<int> order1_ = new List<int>();
		private List<int> order2_ = new List<int>();

		class TickInfo
		{
			public int orderIndex;
			public bool forwards;
			public bool order1;
			public bool mustWait;

			public TickInfo()
			{
				orderIndex = -1;
				forwards = false;
				order1 = true;
				mustWait = false;
			}

			public TickInfo(TickInfo o)
			{
				orderIndex = o.orderIndex;
				forwards = o.forwards;
				order1 = o.order1;
				mustWait = o.mustWait;
			}
		}

		private TickInfo active_ = new TickInfo();
		private TickInfo overlap_ = new TickInfo();

		public override Step Current
		{
			get
			{
				if (active_.orderIndex == -1)
					return null;
				else
					return GetStep(active_.orderIndex, true);
			}
		}

		protected Step GetStep(int orderIndex, bool order1)
		{
			var list = (order1 ? order1_ : order2_);

			if (orderIndex < 0 || orderIndex >= list.Count)
			{
				Synergy.LogError(
					$"GetStep: orderIndex {orderIndex} out of range " +
					 "in " + (order1 ? "order1" : "order2") + ", " +
					$"count={list.Count}");

				return null;
			}

			var stepIndex = list[orderIndex];

			if (stepIndex < 0 || stepIndex >= Steps.Count)
			{
				Synergy.LogError(
					$"GetStep: stepIndex {stepIndex} out of range, " +
					$"steps={Steps.Count} in " + (order1 ? "order1" : "order2"));

				return null;
			}

			return Steps[stepIndex];
		}


		public override void Tick(float deltaTime)
		{
			float OverlapTime = Synergy.Instance.Options.OverlapTime;


			if (order1_.Count == 0)
			{
				Synergy.LogOverlap("Tick: generating order1");
				order1_ = Regenerate(order1_, Steps.Count);
			}

			if (order1_.Count == 0)
			{
				Synergy.LogOverlap("Tick: no steps");
				return;
			}

			if (order2_.Count == 0)
			{
				Synergy.LogOverlap("Tick: generating order2");
				order2_ = Regenerate(order1_, Steps.Count);
			}

			if (active_.orderIndex == -1)
			{
				Synergy.LogOverlap(
					"active is -1, starting from 0, " +
					$"order count={order1_.Count}");

				active_.forwards = true;
				active_.order1 = true;
				NextActive();
			}


			for (int i=0; i<order1_.Count; ++i)
			{
				var s = GetStep(i, true);

				if (s == null)
				{
					Synergy.LogOverlap(
						$"Tick: in loop, GetStep failed, i={i} count={order1_.Count}");

					continue;
				}

				if (i == active_.orderIndex)
				{
					if (s.Enabled && s.Tick(deltaTime, active_.forwards))
					{
						// still going

						if (overlap_.orderIndex == -1 &&
							OverlapTime > 0 &&
						    s.TimeRemainingInDirection < OverlapTime)
						{
							Synergy.LogOverlap(
								$"Tick: remaining {s.TimeRemainingInDirection}, " +
								"starting overlap");

							// step ending, start overlap
							NextOverlap();
						}
					}
					else
					{
						Synergy.LogOverlap("step tick finished, next");

						// next
						NextActive();
					}
				}
				else
				{
					if (overlap_.order1 && i != overlap_.orderIndex)
					{
						// this step is not active nor the overlap
						s.TickPaused(deltaTime);
					}
				}
			}

			if (overlap_.orderIndex != -1 && !overlap_.mustWait)
			{
				var s = GetStep(overlap_.orderIndex, overlap_.order1);

				if (s == null)
				{
					Synergy.LogOverlap(
						$"step not found for overlap " +
						$"index={overlap_.orderIndex} in " +
						(overlap_.order1 ? "order1" : "order2"));
				}
				else
				{
					if (!s.Enabled || !s.Tick(deltaTime, overlap_.forwards))
					{
						Synergy.LogOverlap(
							$"overlap: step tick finished, getting next");

						// next
						NextOverlap();
					}
				}
			}
		}

		private void NextActive()
		{
			Synergy.LogOverlap("\nin NextActive");

			if (order1_.Count == 0)
			{
				Synergy.LogOverlap("NextActive: no steps");
				active_.orderIndex = -1;
				return;
			}

			if (overlap_.orderIndex == -1)
			{
				Synergy.LogOverlap(
					$"NextActive: initial fwd={active_.forwards} i={active_.orderIndex}");

				int initial = active_.orderIndex;
				if (initial < 0)
					initial = 0;

				bool reversedDir = false;

				for (; ; )
				{
					if (active_.orderIndex == initial && reversedDir)
					{
						Synergy.LogOverlap("NextActive: looped around, no valid step, bailing out");
						active_.orderIndex = -1;
						break;
					}

					if (active_.forwards)
					{
						++active_.orderIndex;
						Synergy.LogOverlap($"NextActive: checking {active_.orderIndex}");

						if (active_.orderIndex >= order1_.Count)
						{
							Synergy.LogOverlap(
								$"NextActive: {active_.orderIndex} past end, reversing");

							active_.forwards = false;
							active_.orderIndex = order1_.Count - 1;
							reversedDir = true;
						}

						Synergy.LogOverlap(
							$"NextActive: i now {active_.orderIndex}");
					}
					else
					{
						if (active_.orderIndex == 0)
						{
							Synergy.LogOverlap($"NextActive: at beginning, going forwards");

							active_.forwards = true;
							order1_ = new List<int>(order2_);
							order2_ = Regenerate(order2_, Steps.Count);
							reversedDir = true;
						}
						else
						{
							--active_.orderIndex;

							Synergy.LogOverlap(
								$"NextActive: i now {active_.orderIndex}");
						}
					}

					var s = GetStep(active_.orderIndex, true);
					if (s == null)
					{
						Synergy.LogOverlap("NextActive: GetStep says it's empty, continuing");
					}
					else if (!s.Enabled)
					{
						Synergy.LogOverlap("NextActive: but it's disabled, continuing");
					}
					else
					{
						Synergy.LogOverlap("NextActive: looks good, taking it");
						break;
					}
				}
			}
			else
			{
				Synergy.LogOverlap("NextActive: overlap already active, taking over");

				// already an overlap, take it over
				active_ = new TickInfo(overlap_);
				active_.mustWait = false;

				Synergy.LogOverlap(
					$"NextActive: i={active_.orderIndex}");

				if (!active_.order1)
				{
					Synergy.LogOverlap($"NextActive: was in order2, swapping");
					order1_ = new List<int>(order2_);
					order2_ = Regenerate(order2_, Steps.Count);
					active_.order1 = true;
				}

				overlap_ = new TickInfo();

				return;
			}


			var ns = GetStep(active_.orderIndex, true);
			if (ns != null)
			{
				overlap_.mustWait = false;

				Synergy.LogOverlap(
					$"NextActive: resuming {active_.orderIndex}");

				ns.Resume();
			}
		}

		private void NextOverlap()
		{
			Synergy.LogOverlap("\nin NextOverlap");

			if (overlap_.mustWait)
			{
				Synergy.LogOverlap("\nNextOverlap: must wait");
				return;
			}


			if (overlap_.orderIndex == -1)
			{
				Synergy.LogOverlap(
					"NextOverlap: no current overlap, starting from active " +
					$"i={active_.orderIndex} fwd={active_.forwards}");

				overlap_ = new TickInfo(active_);
			}
			else
			{
				if (GetStep(active_.orderIndex, true).HalfMove)
				{
					Synergy.LogOverlap(
						"NextOverlap: current overlap finished but active is " +
						"half move, must wait");

					overlap_.mustWait = true;
					return;
				}
			}

			if (overlap_.orderIndex == -1)
			{
				Synergy.LogOverlap("NextOverlap: no current active, bailing out");
				return;
			}


			bool order1 = true;
			bool reversedDir = false;


			for (; ; )
			{
				if (overlap_.forwards)
				{
					Synergy.LogOverlap("NextOverlap: checking forwards");

					++overlap_.orderIndex;
					Synergy.LogOverlap($"NextOverlap: i now {overlap_.orderIndex}");

					if (overlap_.orderIndex >= (order1 ? order1_ : order2_).Count)
					{
						Synergy.LogOverlap("NextOverlap: past end, reversing");
						overlap_.orderIndex = order1_.Count;
						overlap_.forwards = false;
						reversedDir = true;
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Synergy.LogOverlap(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (fw)");

						overlap_.mustWait = true;
						break;
					}
					else
					{
						var s = GetStep(overlap_.orderIndex, order1);

						if (s == null)
						{
							Synergy.LogOverlap("NextOverlap: GetStep says it's empty, continuing");
						}
						else if (!s.Enabled)
						{
							Synergy.LogOverlap("NextOverlap: but it's disabled, continuing");
						}
						else
						{
							Synergy.LogOverlap("NextOverlap: looks good, taking it");
							break;
						}
					}
				}
				else
				{
					Synergy.LogOverlap("NextOverlap: checking backwards");

					if (overlap_.orderIndex == 0)
					{
						Synergy.LogOverlap("NextOverlap: past end, reversing");
						overlap_.forwards = true;
						overlap_.orderIndex = -1;

						if (reversedDir)
						{
							Synergy.LogOverlap(
								"NextOverlap: already reversed dir before, " +
								"switching to order2");

							order1 = false;
						}
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Synergy.LogOverlap(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (bw)");

						overlap_.mustWait = true;
					}
					else
					{
						--overlap_.orderIndex;

						var s = GetStep(overlap_.orderIndex, order1);

						if (s == null)
						{
							Synergy.LogOverlap(
								$"NextOverlap: no step for {overlap_.orderIndex}, " +
								"continuing");
						}
						else if (!s.Enabled)
						{
							Synergy.LogOverlap(
								$"NextOverlap: step {overlap_.orderIndex} disabled, " +
								"continuing");
						}
						else if (s.HalfMove)
						{
							if (overlap_.orderIndex == active_.orderIndex)
							{
								Synergy.LogOverlap(
									$"NextOverlap: step {overlap_.orderIndex} enabled " +
									"but half move and is same as active; must wait");

								overlap_.mustWait = true;
							}
							else
							{
								Synergy.LogOverlap(
									$"NextOverlap: step {overlap_.orderIndex} enabled " +
									"and half move, taking it");
							}

							break;
						}

						Synergy.LogOverlap(
							$"NextOverlap: step {overlap_.orderIndex} enabled " +
							"but not half move, so doesn't need ticking; continuing");
					}
				}
			}


			if (overlap_.mustWait)
				return;

			var ns = GetStep(overlap_.orderIndex, order1);
			if (ns != null)
			{
				Synergy.LogOverlap(
					$"NextOverlap: resuming {overlap_.orderIndex}");

				ns.Resume();
			}
		}

		public override void StepInserted(int at, Step s)
		{
			base.StepInserted(at, s);

			if (at >= 0 && at < order1_.Count)
			{
				Synergy.LogOverlap("step inserted");
				order1_.Insert(at, Steps.Count - 1);
				order2_.Insert(at, Steps.Count - 1);
			}
		}

		public override void StepDeleted(int stepIndex)
		{
			base.StepDeleted(stepIndex);

			Synergy.LogError($"step deleted {stepIndex}");

			if (active_.orderIndex != -1 && order1_[active_.orderIndex] == stepIndex)
			{
				Synergy.LogError("this is the active step, regenerating");
				StepsChanged();
			}
			else
			{
				Synergy.LogError("not the active step");
				RemoveFromOrder(stepIndex, true);
				RemoveFromOrder(stepIndex, false);
			}
		}

		private void RemoveFromOrder(int stepIndex, bool order1)
		{
			var list = (order1 ? order1_ : order2_);

			var orderIndex = list.IndexOf(stepIndex);

			if (orderIndex == -1)
			{
				Synergy.LogError(
					$"StepDeleted: step index {stepIndex} not found in " +
						"order list");

				return;
			}

			if (order1)
			{
				if (orderIndex < active_.orderIndex)
				{
					Synergy.LogError(
						$"is before current in the order, so " +
						$"active was {active_.orderIndex}, now {active_.orderIndex - 1}");

					--active_.orderIndex;
				}
			}

			list.RemoveAt(orderIndex);

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] >= stepIndex)
					--list[i];
			}

			Synergy.LogError(
				$"removed from list, now has {list.Count} elements");
		}

		public override bool IsStepActive(Step s)
		{
			for (int i=0; i<=active_.orderIndex; ++i)
			{
				if (GetStep(i, true) == s)
					return true;
			}

			return false;
		}

		protected override void StepsChanged()
		{
			base.StepsChanged();

			Synergy.LogOverlap("steps changed, regenerating");

			active_ = new TickInfo();
			overlap_ = new TickInfo();

			order1_ = new List<int>();
			order2_ = new List<int>();
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
