using Synergy;
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
			Assert.AreEqual(d.ActiveDuration, null);
			Assert.IsInstanceOfType(d.SingleDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.HalfwayDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.EndForwardsDuration, typeof(RandomDuration));
			Assert.IsInstanceOfType(d.EndBackwardsDuration, typeof(RandomDuration));
			Assert.IsFalse(d.Halfway);
			Assert.IsFalse(d.EndForwards);
			Assert.IsFalse(d.EndBackwards);
			Assert.AreEqual(d.ActiveType, Delay.None);
			Assert.IsFalse(d.StopAfter);
			Assert.IsFalse(d.ResetDurationAfter);
			Assert.IsTrue(d.SameDelay);
		}
	}
}
