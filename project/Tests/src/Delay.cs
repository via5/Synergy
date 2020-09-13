using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Synergy.Tests
{
	[TestClass]
	public class DelayTests
	{
		[TestMethod]
		public void DefaultCtor()
		{
			var d = new Delay();
			Assert.AreEqual(null, d.ActiveDuration);
			Assert.IsInstanceOfType(d.SingleDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.HalfwayDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.EndForwardsDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.EndBackwardsDuration, typeof(RandomDuration));
			Assert.IsFalse(d.Halfway);
			Assert.IsFalse(d.EndForwards);
			Assert.IsFalse(d.EndBackwards);
			Assert.AreEqual(Delay.None, d.ActiveType);
			Assert.IsFalse(d.StopAfter);
			Assert.IsFalse(d.ResetDurationAfter);
			Assert.IsTrue(d.SameDelay);
		}
	}
}
