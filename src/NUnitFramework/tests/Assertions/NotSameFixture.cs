// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Framework.Assertions
{
    [TestFixture]
    public class NotSameFixture
    {
        private readonly string _s1 = "S1";
        private readonly string _s2 = "S2";

        [Test]
        public void NotSame()
        {
            Assert.AreNotSame(_s1, _s2);
        }

        [Test]
        public void NotSameFails()
        {
            var expectedMessage =
                "  Expected: not same as \"S1\"" + Environment.NewLine +
                "  But was:  \"S1\"" + Environment.NewLine;
            var ex = Assert.Throws<AssertionException>(() => Assert.AreNotSame( _s1, _s1 ));
            Assert.That(ex.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void ShouldNotCallToStringOnClassForPassingTests()
        {
            var actual = new ThrowsIfToStringIsCalled(1);
            var expected = new ThrowsIfToStringIsCalled(1);

            Assert.AreNotSame(expected, actual);
        }
    }
}
