﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Framework.Internal.Filters
{
    public class IdFilterTests : TestFilterTests
    {
        private TestFilter _filter;

        [SetUp]
        public void CreateFilter()
        {
            _filter = new IdFilter(DummyFixtureSuite.Id);
        }

        [Test]
        public void IsNotEmpty()
        {
            Assert.That(_filter.IsEmpty, Is.False);
        }

        [Test]
        public void MatchTest()
        {
            Assert.That(_filter.Match(DummyFixtureSuite));
            Assert.That(_filter.Match(AnotherFixtureSuite), Is.False);
        }

        [Test]
        public void PassTest()
        {
            Assert.That(_filter.Pass(TopLevelSuite));
            Assert.That(_filter.Pass(DummyFixtureSuite));
            Assert.That(_filter.Pass(DummyFixtureSuite.Tests[0]));

            Assert.That(_filter.Pass(AnotherFixtureSuite), Is.False);
        }

        [Test]
        public void ExplicitMatchTest()
        {
            Assert.That(_filter.IsExplicitMatch(TopLevelSuite));
            Assert.That(_filter.IsExplicitMatch(DummyFixtureSuite));
            Assert.That(_filter.IsExplicitMatch(DummyFixtureSuite.Tests[0]), Is.False);

            Assert.That(_filter.IsExplicitMatch(AnotherFixtureSuite), Is.False);
        }
    }
}
