using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Synergy.Tests
{
	public class TestRng : IRandomProvider
	{
		private Random r_ = new Random();

		public float RandomFloat(float min, float max)
		{
			return (float)(r_.NextDouble() * (max - min) + min);
		}

		public int RandomInt(int min, int max)
		{
			return r_.Next(min, max);
		}
	}


	[TestClass]
	public class Init
	{
		[AssemblyInitialize()]
		public static void MyTestInitialize(TestContext testContext)
		{
			Utilities.RandomProvider = new TestRng();
		}
	}


	public class Test
	{
		public const float D = 0.001f;

		public float C(float v, float min, float max)
		{
			if (v < min)
				return min;
			else if (v > max)
				return max;
			else
				return v;
		}

	}


	[TestClass]
	public class RandomDurationTests : Test
	{
		[TestMethod]
		public void DefaultCtor()
		{
			var d = new RandomDuration();
			Assert.AreEqual(0, d.FirstHalfProgress);
			Assert.AreEqual(0, d.SecondHalfProgress);
			Assert.IsTrue(d.InFirstHalf);
			Assert.IsFalse(d.FirstHalfFinished);
			Assert.AreEqual(0, d.TotalProgress);
			Assert.IsTrue(d.InFirstHalfTotal);
			Assert.IsFalse(d.Finished);
			Assert.AreEqual(1, d.TimeRemaining);
			Assert.AreEqual(0.5, d.TimeRemainingInHalf);
			Assert.AreEqual(1, d.Current);
		}

		private void TickFirstHalfTest(RandomDuration d, float progress)
		{
			Assert.AreEqual(C(progress * 2, 0, 1), d.FirstHalfProgress, D);
			Assert.AreEqual(0, d.SecondHalfProgress);
			Assert.IsTrue(d.InFirstHalf);
			Assert.IsFalse(d.FirstHalfFinished);
			Assert.AreEqual(progress * 2, d.TotalProgress, D);
			Assert.IsTrue(d.InFirstHalfTotal);
			Assert.IsFalse(d.Finished);
			Assert.AreEqual(1.0f - progress, d.TimeRemaining, D);
			Assert.AreEqual(0.5f - progress, d.TimeRemainingInHalf, D);

			// constant
			Assert.AreEqual(1, d.Current);
		}

		private void TickSecondHalfTest(RandomDuration d, float progress)
		{
			Assert.AreEqual(1, d.FirstHalfProgress);
			Assert.AreEqual((progress - 0.5f) * 2, d.SecondHalfProgress, D);
			Assert.IsFalse(d.InFirstHalf);
			Assert.IsTrue(d.FirstHalfFinished);
			Assert.AreEqual((progress - 0.5f) * 2, d.TotalProgress, D);
			Assert.IsFalse(d.InFirstHalfTotal);
			Assert.IsFalse(d.Finished);
			Assert.AreEqual(1.0f - progress, d.TimeRemaining, D);
			Assert.AreEqual(1.0f - progress, d.TimeRemainingInHalf, D);

			// constant
			Assert.AreEqual(1, d.Current);
		}

		private void TickFinishedTest(RandomDuration d)
		{
			Assert.AreEqual(1, d.FirstHalfProgress);
			Assert.AreEqual(1, d.SecondHalfProgress);
			Assert.IsFalse(d.InFirstHalf);
			Assert.IsTrue(d.FirstHalfFinished);
			Assert.AreEqual(1, d.TotalProgress);
			Assert.IsFalse(d.InFirstHalfTotal);
			Assert.IsTrue(d.Finished);
			Assert.AreEqual(0, d.TimeRemaining);
			Assert.AreEqual(0, d.TimeRemainingInHalf);

			// constant
			Assert.AreEqual(1, d.Current);
		}


		[TestMethod]
		public void Tick1()
		{
			var d = new RandomDuration(1);
			TickFirstHalfTest(d, 0);

			d.Tick(0.1f);
			TickFirstHalfTest(d, 0.1f);

			d.Tick(0.1f);
			TickFirstHalfTest(d, 0.2f);

			d.Tick(0.1f);
			TickFirstHalfTest(d, 0.3f);

			d.Tick(0.1f);
			TickFirstHalfTest(d, 0.4f);

			d.Tick(0.1f);
			TickFirstHalfTest(d, 0.5f);


			d.Tick(0.1f);
			TickSecondHalfTest(d, 0.6f);

			d.Tick(0.1f);
			TickSecondHalfTest(d, 0.7f);

			d.Tick(0.1f);
			TickSecondHalfTest(d, 0.8f);

			d.Tick(0.1f);
			TickSecondHalfTest(d, 0.9f);

			d.Tick(0.1f);
			TickFinishedTest(d);
		}

		[TestMethod]
		public void Tick2()
		{
			var d = new RandomDuration(1);
			TickFirstHalfTest(d, 0);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.2f);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.4f);


			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.6f);

			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.8f);


			d.Tick(0.3f);
			TickFinishedTest(d);
		}

		[TestMethod]
		public void TickAndReset()
		{
			var d = new RandomDuration(1);
			TickFirstHalfTest(d, 0);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.2f);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.4f);


			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.6f);

			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.8f);


			d.Tick(0.3f);
			TickFinishedTest(d);


			d.Reset();


			TickFirstHalfTest(d, 0);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.2f);

			d.Tick(0.2f);
			TickFirstHalfTest(d, 0.4f);


			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.6f);

			d.Tick(0.2f);
			TickSecondHalfTest(d, 0.8f);


			d.Tick(0.3f);
			TickFinishedTest(d);
		}
	}


	[TestClass]
	public class RampDurationTests : Test
	{
		[TestMethod]
		public void DefaultCtor()
		{
			var d = new RampDuration();
			Assert.AreEqual(0, d.FirstHalfProgress);
			Assert.AreEqual(0, d.SecondHalfProgress);
			Assert.IsTrue(d.InFirstHalf);
			Assert.IsFalse(d.FirstHalfFinished);
			Assert.AreEqual(0, d.TotalProgress);
			Assert.IsTrue(d.InFirstHalfTotal);
			Assert.IsFalse(d.Finished);
			Assert.AreEqual(2, d.TimeRemaining);
			Assert.AreEqual(1, d.TimeRemainingInHalf);
			Assert.AreEqual(1, d.Current);
			Assert.AreEqual(1, d.TimeUp);
			Assert.AreEqual(1, d.TimeDown);
			Assert.AreEqual(new FloatRange(1, 1), d.Range);
			Assert.AreEqual(1, d.Minimum);
			Assert.AreEqual(1, d.Maximum);
			Assert.AreEqual(0, d.Hold);
			Assert.IsTrue(d.RampUp);
			Assert.IsTrue(d.RampDown);
			Assert.IsInstanceOfType(d.Easing, typeof(LinearEasing));
			Assert.AreEqual(0, d.Elapsed);
			Assert.AreEqual(0, d.TotalElapsed);
			Assert.AreEqual(0, d.Progress);
			Assert.AreEqual(1, d.HoldingProgress);
			Assert.AreEqual(0, d.HoldingElapsed);
		}

		private void TickUpTest(RampDuration d, float progress, float current)
		{
			Assert.AreEqual(progress, d.FirstHalfProgress);
			Assert.AreEqual(0, d.SecondHalfProgress);
			Assert.IsTrue(d.InFirstHalf);
			Assert.IsFalse(d.FirstHalfFinished);
			Assert.AreEqual(progress, d.TotalProgress);
			Assert.IsTrue(d.InFirstHalfTotal);
			Assert.IsFalse(d.Finished);
			Assert.AreEqual(2 + 1 - progress, d.TimeRemaining);
			Assert.AreEqual(1 - progress + 1, d.TimeRemainingInHalf);
			Assert.AreEqual(current, d.Current);
			Assert.AreEqual(progress, d.Elapsed);
			Assert.AreEqual(progress, d.TotalElapsed);
			Assert.AreEqual(progress, d.Progress);
			Assert.AreEqual(0, d.HoldingProgress);
			Assert.AreEqual(0, d.HoldingElapsed);

			// constant
			Assert.AreEqual(1, d.TimeUp);
			Assert.AreEqual(1, d.TimeDown);
			Assert.AreEqual(new FloatRange(1.2f, 0.5f), d.Range);
			Assert.AreEqual(1.2f, d.Minimum);
			Assert.AreEqual(0.5f, d.Maximum);
			Assert.AreEqual(1, d.Hold);
			Assert.IsTrue(d.RampUp);
			Assert.IsTrue(d.RampDown);
			Assert.IsInstanceOfType(d.Easing, typeof(LinearEasing));
		}

		[TestMethod]
		public void Tick()
		{
			var d = new RampDuration(1.2f, 0.5f, 1, 1);
			TickUpTest(d, 0, 1.2f);

			d.Tick(0.1f);
			TickUpTest(d, 0.05f, 1.2f);
		}
	}
}
