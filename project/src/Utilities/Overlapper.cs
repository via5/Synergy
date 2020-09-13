using System.Collections.Generic;

namespace Synergy
{
	class Overlapper
	{
		public delegate bool RealIndexCallback(int i);
		public delegate bool TickCallback(
			int realIndex, float deltaTime, bool forwards, bool paused);
		public delegate float TimeRemainingCallback(int realIndex);
		public delegate List<int> Generator(List<int> old);
		public delegate int CountCallback();
		public delegate float TimeCallback();

		public event RealIndexCallback CanRun;
		public event RealIndexCallback Resume;
		public event RealIndexCallback CanRunBackwards;
		public event RealIndexCallback Reset;
		public event TickCallback Ticker;
		public event TimeRemainingCallback TimeRemaining;
		public event Generator Regenerate;
		public event CountCallback ItemCount;
		public event TimeCallback GetOverlapTime;

		public class TickInfo
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

			public override string ToString()
			{
				return
					"i=" + orderIndex.ToString() + " " +
					(forwards ? "fw" : "bw") + " " +
					(order1 ? "o1" : "o2") + " " +
					(mustWait ? "wait" : "");
			}
		}


		private readonly string name_;
		private List<int> order1_ = new List<int>();
		private List<int> order2_ = new List<int>();
		private TickInfo active_ = new TickInfo();
		private TickInfo overlap_ = new TickInfo();


		public Overlapper(string name)
		{
			name_ = name;
		}

		public int CurrentIndex
		{
			get
			{
				return GetRealIndex(active_.orderIndex, true);
			}
		}

		public float TimeRemainingForCurrent
		{
			get
			{
				var ri = CurrentIndex;
				if (ri == -1)
					return -1;
				else
					return TimeRemaining(ri);
			}
		}

		public TickInfo ActiveTick
		{
			get { return new TickInfo(active_); }
		}

		public TickInfo OverlapTick
		{
			get { return new TickInfo(overlap_); }
		}

		public float OverlapTime
		{
			get { return GetOverlapTime(); }
		}

		public int GetRealIndex(int orderIndex, bool order1)
		{
			if (orderIndex == -1)
				return -1;

			var list = (order1 ? order1_ : order2_);

			if (orderIndex < 0 || orderIndex >= list.Count)
			{
				LogErrorST(
					$"GetRealIndex: orderIndex {orderIndex} out of range " +
					 "in " + (order1 ? "order1" : "order2") + ", " +
					$"count={list.Count}");

				return -1;
			}

			var realIndex = list[orderIndex];

			if (realIndex < 0 || realIndex >= ItemCount())
			{
				LogErrorST(
					$"GetRealIndex: realIndex {realIndex} out of range, " +
					$"count={ItemCount()} in " + (order1 ? "order1" : "order2"));

				return -1;
			}

			return realIndex;
		}

		public void ForceRun(int realIndex)
		{
			order1_ = Regenerate(null);
			order2_ = new List<int>();

			active_ = new TickInfo();
			overlap_ = new TickInfo();

			for (int i = 0; i < order1_.Count; ++i)
			{
				var ri = GetRealIndex(i, true);
				Reset(ri);

				if (ri == realIndex)
					active_.orderIndex = i;
			}

			if (active_.orderIndex == -1)
			{
				LogError($"item {realIndex} not found");
				return;
			}

			Resume(realIndex);
		}

		public void Tick(float deltaTime)
		{
			if (order1_.Count == 0)
			{
				Log("Tick: generating order1");
				order1_ = Regenerate(order1_);
			}

			if (order1_.Count == 0)
			{
				Log("Tick: no elements");
				return;
			}

			if (order2_.Count == 0)
			{
				Log("Tick: generating order2");
				order2_ = Regenerate(order1_);
			}

			if (active_.orderIndex == -1)
			{
				Log(
					"active is -1, starting from 0, " +
					$"order count={order1_.Count}");

				active_.forwards = true;
				active_.order1 = true;
				NextActive();
			}


			for (int orderIndex = 0; orderIndex < order1_.Count; ++orderIndex)
			{
				var realIndex = GetRealIndex(orderIndex, true);

				if (realIndex == -1)
				{
					Log(
						$"Tick: in loop, GetRealIndex failed, " +
						$"i={orderIndex} count={order1_.Count}");

					continue;
				}

				if (orderIndex == active_.orderIndex)
				{
					if (CanRun(realIndex) && Ticker(realIndex, deltaTime, active_.forwards, false))
					{
						// still going

						if (overlap_.orderIndex == -1 &&
							GetOverlapTime() > 0 &&
							TimeRemaining(realIndex) < GetOverlapTime())
						{
							Log(
								$"Tick: remaining {TimeRemaining(realIndex)}, " +
								"starting overlap");

							// element ending, start overlap
							NextOverlap();
						}
					}
					else
					{
						Log("tick finished, next");

						// next
						NextActive();
					}
				}
				else
				{
					if (overlap_.order1 && orderIndex != overlap_.orderIndex)
					{
						// this element is not active nor the overlap
						Ticker(realIndex, deltaTime, false, true);
					}
				}
			}

			if (overlap_.orderIndex != -1 && !overlap_.mustWait)
			{
				var realIndex = GetRealIndex(overlap_.orderIndex, overlap_.order1);

				if (realIndex == -1)
				{
					Log(
						$"element not found for overlap " +
						$"index={overlap_.orderIndex} in " +
						(overlap_.order1 ? "order1" : "order2"));
				}
				else
				{
					if (!CanRun(realIndex) || !Ticker(realIndex, deltaTime, overlap_.forwards, false))
					{
						Log($"overlap: element tick finished, getting next");
						NextOverlap();
					}
				}
			}
		}

		private void NextActive()
		{
			Log("\nin NextActive");

			if (order1_.Count == 0)
			{
				Log("NextActive: no elements");
				active_.orderIndex = -1;
				return;
			}

			if (overlap_.orderIndex == -1)
			{
				Log(
					$"NextActive: initial fwd={active_.forwards} i={active_.orderIndex}");

				int initial = active_.orderIndex;
				if (initial < 0)
					initial = 0;

				bool reversedDir = false;

				for (; ; )
				{
					if (active_.orderIndex == initial && reversedDir)
					{
						Log(
							"NextActive: looped around, no valid " +
							"element, bailing out");

						active_.orderIndex = -1;
						break;
					}

					if (active_.forwards)
					{
						++active_.orderIndex;
						Log($"NextActive: checking {active_.orderIndex}");

						if (active_.orderIndex >= order1_.Count)
						{
							Log(
								$"NextActive: {active_.orderIndex} past end, reversing");

							active_.forwards = false;
							active_.orderIndex = order1_.Count - 1;
							reversedDir = true;
						}

						Log(
							$"NextActive: i now {active_.orderIndex}");
					}
					else
					{
						if (active_.orderIndex == 0)
						{
							Log($"NextActive: at beginning, going forwards");

							active_.forwards = true;
							order1_ = new List<int>(order2_);
							order2_ = Regenerate(order2_);
							reversedDir = true;
						}
						else
						{
							--active_.orderIndex;

							Log(
								$"NextActive: i now {active_.orderIndex}");
						}
					}

					var realIndex = GetRealIndex(active_.orderIndex, true);

					if (realIndex == -1)
					{
						Log("NextActive: GetRealIndex says it's -1, continuing");
					}
					else if (!CanRun(realIndex))
					{
						Log("NextActive: but it's disabled, continuing");
					}
					else
					{
						Log("NextActive: looks good, taking it");
						break;
					}
				}
			}
			else
			{
				Log("NextActive: overlap already active, taking over");

				// already an overlap, take it over
				active_ = new TickInfo(overlap_);
				active_.mustWait = false;

				Log(
					$"NextActive: i={active_.orderIndex}");

				if (!active_.order1)
				{
					Log($"NextActive: was in order2, swapping");
					order1_ = new List<int>(order2_);
					order2_ = Regenerate(order2_);
					active_.order1 = true;
				}

				overlap_ = new TickInfo();

				return;
			}


			var newRealIndex = GetRealIndex(active_.orderIndex, true);
			if (newRealIndex != -1)
			{
				overlap_.mustWait = false;

				Log(
					$"NextActive: resuming {active_.orderIndex}");

				Resume(newRealIndex);
			}
		}

		private void NextOverlap()
		{
			Log("\nin NextOverlap");

			if (overlap_.mustWait)
			{
				Log("\nNextOverlap: must wait");
				return;
			}


			if (overlap_.orderIndex == -1)
			{
				Log(
					"NextOverlap: no current overlap, starting from active " +
					$"i={active_.orderIndex} fwd={active_.forwards}");

				overlap_ = new TickInfo(active_);
			}
			else
			{
				var realIndex = GetRealIndex(active_.orderIndex, true);

				if (CanRunBackwards(realIndex))
				{
					Log(
						"NextOverlap: current overlap finished but active is " +
						"half move, must wait");

					overlap_.mustWait = true;
					return;
				}
			}

			if (overlap_.orderIndex == -1)
			{
				Log("NextOverlap: no current active, bailing out");
				return;
			}


			bool order1 = true;
			bool reversedDir = false;


			for (; ; )
			{
				if (overlap_.forwards)
				{
					Log("NextOverlap: checking forwards");

					++overlap_.orderIndex;
					Log($"NextOverlap: i now {overlap_.orderIndex}");

					if (overlap_.orderIndex >= (order1 ? order1_ : order2_).Count)
					{
						Log("NextOverlap: past end, reversing");
						overlap_.orderIndex = order1_.Count;
						overlap_.forwards = false;
						reversedDir = true;
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Log(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (fw)");

						overlap_.mustWait = true;
						break;
					}
					else
					{
						var realIndex = GetRealIndex(overlap_.orderIndex, order1);

						if (realIndex == -1)
						{
							Log("NextOverlap: GetRealIndex says it's -1, continuing");
						}
						else if (!CanRun(realIndex))
						{
							Log("NextOverlap: but it's disabled, continuing");
						}
						else
						{
							Log("NextOverlap: looks good, taking it");
							break;
						}
					}
				}
				else
				{
					Log("NextOverlap: checking backwards");

					if (overlap_.orderIndex == 0)
					{
						Log("NextOverlap: past end, reversing");
						overlap_.forwards = true;
						overlap_.orderIndex = -1;

						if (reversedDir)
						{
							Log(
								"NextOverlap: already reversed dir before, " +
								"switching to order2");

							order1 = false;
						}
					}
					else if (overlap_.orderIndex == active_.orderIndex && !order1)
					{
						Log(
							"NextOverlap: went around, reached active " +
							$"i={active_.orderIndex}, must wait (bw)");

						overlap_.mustWait = true;
						break;
					}
					else
					{
						--overlap_.orderIndex;

						var realIndex = GetRealIndex(overlap_.orderIndex, order1);

						if (realIndex == -1)
						{
							Log(
								$"NextOverlap: no element for " +
								$"{overlap_.orderIndex}, continuing");
						}
						else if (!CanRun(realIndex))
						{
							Log(
								$"NextOverlap: index {overlap_.orderIndex} " +
								$"disabled, continuing");
						}
						else if (CanRunBackwards(realIndex))
						{
							if (overlap_.orderIndex == active_.orderIndex)
							{
								Log(
									$"NextOverlap: index " +
									$"{overlap_.orderIndex} enabled but " +
									$"half move and is same as active; must " +
									$"wait");

								overlap_.mustWait = true;
							}
							else
							{
								Log(
									$"NextOverlap: index " +
									$"{overlap_.orderIndex} enabled and " +
									$"half move, taking it");
							}

							break;
						}

						Log(
							$"NextOverlap: index {overlap_.orderIndex} " +
							$"enabled but not half move, so doesn't need " +
							$"ticking; continuing");
					}
				}
			}


			if (overlap_.mustWait)
				return;

			var newRealIndex = GetRealIndex(overlap_.orderIndex, order1);
			if (newRealIndex != -1)
			{
				Log(
					$"NextOverlap: resuming {overlap_.orderIndex}");

				Resume(newRealIndex);
			}
		}

		public void ItemInserted(int at)
		{
			if (at >= 0 && at < order1_.Count)
			{
				Log("item inserted");
				order1_.Insert(at, ItemCount() - 1);
				order2_.Insert(at, ItemCount() - 1);
			}
		}

		public void ItemDeleted(int realIndex)
		{
			Log($"item deleted {realIndex}");

			if (active_.orderIndex != -1 && order1_[active_.orderIndex] == realIndex)
			{
				Log("this is the active element, regenerating");
				ItemsChanged();
			}
			else
			{
				Log("not the active element");
				RemoveFromOrder(realIndex, true);
				RemoveFromOrder(realIndex, false);
			}
		}

		private void RemoveFromOrder(int realIndex, bool order1)
		{
			var list = (order1 ? order1_ : order2_);

			var orderIndex = list.IndexOf(realIndex);

			if (orderIndex == -1)
			{
				LogError(
					$"RemoveFromOrder: index {realIndex} not found in " +
					$"order list");

				return;
			}

			if (order1)
			{
				if (orderIndex < active_.orderIndex)
				{
					Log(
						$"is before current in the order, so " +
						$"active was {active_.orderIndex}, now {active_.orderIndex - 1}");

					--active_.orderIndex;
				}
			}

			if (order1 == overlap_.order1)
			{
				if (orderIndex < overlap_.orderIndex)
				{
					Log(
						$"is before current overlap in the order, so " +
						$"overlap was {overlap_.orderIndex}, now {overlap_.orderIndex - 1}");

					--overlap_.orderIndex;
				}
			}


			list.RemoveAt(orderIndex);

			for (int i = 0; i < list.Count; ++i)
			{
				if (list[i] >= realIndex)
					--list[i];
			}

			Log($"removed from list, now has {list.Count} elements");
		}

		public bool IsRunning(int realIndex)
		{
			if (active_.orderIndex != -1)
			{
				if (GetRealIndex(active_.orderIndex, true) == realIndex)
					return true;
			}

			if (overlap_.orderIndex != -1)
			{
				if (GetRealIndex(overlap_.orderIndex, overlap_.order1) == realIndex)
					return true;
			}

			return false;
		}

		public bool IsActive(int realIndex)
		{
			for (int i = 0; i <= active_.orderIndex; ++i)
			{
				if (GetRealIndex(i, true) == realIndex)
					return true;
			}

			return false;
		}

		public void ItemsChanged()
		{
			Log("items changed, regenerating");

			active_ = new TickInfo();
			overlap_ = new TickInfo();

			order1_ = new List<int>();
			order2_ = new List<int>();
		}

		private void LogError(string s)
		{
			Synergy.LogError($"[{name_}] {s}");
		}

		private void LogErrorST(string s)
		{
			Synergy.LogErrorST($"[{name_}] {s}");
		}

		private void Log(string s)
		{
			Synergy.LogOverlap($"[{name_}] {s}");
		}
	}
}
