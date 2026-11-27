using NUnit.Framework;

namespace XunJiYiDa.Levels.Prologue.XR.Tests
{
    public class BookOrderRuleTests
    {
        [Test]
        public void CollectsOnlyInOrder()
        {
            var rule = new BookOrderRule("red", "blue", "yellow");
            Assert.AreEqual("red", rule.NextId);
            Assert.IsFalse(rule.TryCollect("blue"));
            Assert.AreEqual(0, rule.Collected);

            Assert.IsTrue(rule.TryCollect("red"));
            Assert.IsTrue(rule.TryCollect("blue"));
            Assert.IsFalse(rule.TryCollect("blue"));
            Assert.IsTrue(rule.TryCollect("yellow"));

            Assert.IsTrue(rule.IsComplete);
            Assert.IsNull(rule.NextId);
            Assert.IsFalse(rule.TryCollect("red"));
        }

        [Test]
        public void Reset_StartsOver()
        {
            var rule = new BookOrderRule("red", "blue");
            rule.TryCollect("red");
            rule.Reset();
            Assert.AreEqual(0, rule.Collected);
            Assert.AreEqual("red", rule.NextId);
        }

        [Test]
        public void EmptyOrder_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new BookOrderRule());
        }
    }
}
