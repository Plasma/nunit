// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework.Constraints;

namespace NUnit.Framework.Constraints
{
    [TestFixture]
    public class NumericsTests
    {
        private Tolerance tenPercent, zeroTolerance, absoluteTolerance;

        [SetUp]
        public void SetUp()
        {
            absoluteTolerance = new Tolerance(0.1);
            tenPercent = new Tolerance(10.0).Percent;
            zeroTolerance = Tolerance.Exact;
        }

        [TestCase(123456789)]
        [TestCase(123456789U)]
        [TestCase(123456789L)]
        [TestCase(123456789UL)]
        [TestCase(1234.5678f)]
        [TestCase(1234.5678)]
        [Test]
        public void CanMatchWithoutToleranceMode(object value)
        {
            Assert.IsTrue(Numerics.AreEqual(value, value, ref zeroTolerance));
        }

        // Separate test case because you can't use decimal in an attribute (24.1.3)
        [Test]
        public void CanMatchDecimalWithoutToleranceMode()
        {
            Assert.IsTrue(Numerics.AreEqual(123m, 123m, ref zeroTolerance));
        }

        [TestCase((int)9500)]
        [TestCase((int)10000)]
        [TestCase((int)10500)]
        [TestCase((uint)9500)]
        [TestCase((uint)10000)]
        [TestCase((uint)10500)]
        [TestCase((long)9500)]
        [TestCase((long)10000)]
        [TestCase((long)10500)]
        [TestCase((ulong)9500)]
        [TestCase((ulong)10000)]
        [TestCase((ulong)10500)]
        [Test]
        public void CanMatchIntegralsWithPercentage(object value)
        {
            Assert.IsTrue(Numerics.AreEqual(10000, value, ref tenPercent));
        }

        [Test]
        public void CanMatchDecimalWithPercentage()
        {
            Assert.IsTrue(Numerics.AreEqual(10000m, 9500m, ref tenPercent));
            Assert.IsTrue(Numerics.AreEqual(10000m, 10000m, ref tenPercent));
            Assert.IsTrue(Numerics.AreEqual(10000m, 10500m, ref tenPercent));
        }

        [Test]
        public void CanCalculateAbsoluteDifference()
        {
            Assert.That(Numerics.Difference(10000m, 9500m, absoluteTolerance.Mode), Is.EqualTo(500m));
            Assert.That(Convert.ToDouble(Numerics.Difference(0.1, 0.05, absoluteTolerance.Mode)), Is.EqualTo(0.05).Within(0.00001));
            Assert.That(Convert.ToDouble(Numerics.Difference(0.1, 0.15, absoluteTolerance.Mode)), Is.EqualTo(-0.05).Within(0.00001));
        }

        [Test]
        public void CanCalculatePercentDifference()
        {
            Assert.That(Numerics.Difference(10000m, 8500m, tenPercent.Mode), Is.EqualTo(15));
            Assert.That(Numerics.Difference(10000m, 11500m, tenPercent.Mode), Is.EqualTo(-15));
        }

        [Test]
        public void DifferenceForNonNumericTypesReturnsNaN()
        {
            Assert.That(Numerics.Difference(new object(), new object(), tenPercent.Mode), Is.EqualTo(double.NaN));
        }

        [TestCase((int)8500)]
        [TestCase((int)11500)]
        [TestCase((uint)8500)]
        [TestCase((uint)11500)]
        [TestCase((long)8500)]
        [TestCase((long)11500)]
        [TestCase((ulong)8500)]
        [TestCase((ulong)11500)]
        public void FailsOnIntegralsOutsideOfPercentage(object value)
        {
            Assert.Throws<AssertionException>(() => Assert.IsTrue(Numerics.AreEqual(10000, value, ref tenPercent)));
        }

        [Test]
        public void FailsOnDecimalBelowPercentage()
        {
            Assert.Throws<AssertionException>(() => Assert.IsTrue(Numerics.AreEqual(10000m, 8500m, ref tenPercent)));
        }

        [Test]
        public void FailsOnDecimalAbovePercentage()
        {
            Assert.Throws<AssertionException>(() => Assert.IsTrue(Numerics.AreEqual(10000m, 11500m, ref tenPercent)));
        }
    }
}
