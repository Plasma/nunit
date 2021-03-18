// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.TestData.RepeatingTests;
using NUnit.TestUtilities;

namespace NUnit.Framework.Attributes
{
    [TestFixture]
    public class RetryAttributeTests
    {
        [TestCase(typeof(RetrySucceedsOnFirstTryFixture), "Passed", 1)]
        [TestCase(typeof(RetrySucceedsOnSecondTryFixture), "Passed", 2)]
        [TestCase(typeof(RetrySucceedsOnThirdTryFixture), "Passed", 3)]
        [TestCase(typeof(RetryFailsEveryTimeFixture), "Failed(Child)", 3)]
        [TestCase(typeof(RetryWithIgnoreAttributeFixture), "Skipped:Ignored(Child)", 0)]
        [TestCase(typeof(RetryIgnoredOnFirstTryFixture), "Skipped:Ignored(Child)", 1)]
        [TestCase(typeof(RetryIgnoredOnSecondTryFixture), "Skipped:Ignored(Child)", 2)]
        [TestCase(typeof(RetryIgnoredOnThirdTryFixture), "Skipped:Ignored(Child)", 3)]
        [TestCase(typeof(RetryErrorOnFirstTryFixture), "Failed(Child)", 1)]
        [TestCase(typeof(RetryErrorOnSecondTryFixture), "Failed(Child)", 2)]
        [TestCase(typeof(RetryErrorOnThirdTryFixture), "Failed(Child)", 3)]
        [TestCase(typeof(RetryTestCaseFixture), "Failed(Child)", 3)]
        public void RetryWorksAsExpectedOnFixturesWithSetupAndTeardown(Type fixtureType, string outcome, int nTries)
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(fixtureType);
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            Assert.That(result.ResultState.ToString(), Is.EqualTo(outcome));
            Assert.AreEqual(1, fixture.FixtureSetupCount);
            Assert.AreEqual(1, fixture.FixtureTeardownCount);
            Assert.AreEqual(nTries, fixture.SetupCount);
            Assert.AreEqual(nTries, fixture.TeardownCount);
            Assert.AreEqual(nTries, fixture.Count);
        }

        [TestCase(typeof(RetrySucceedsOnFirstTryFixture), "Passed")]
        [TestCase(typeof(RetrySucceedsOnSecondTryFixture), "Failed", "Passed")]
        [TestCase(typeof(RetrySucceedsOnThirdTryFixture), "Failed", "Failed", "Passed")]
        [TestCase(typeof(RetryFailsEveryTimeFixture), "Failed", "Failed", "Failed")]
        [TestCase(typeof(RetryIgnoredOnFirstTryFixture), "Skipped:Ignored")]
        [TestCase(typeof(RetryIgnoredOnSecondTryFixture), "Failed", "Skipped:Ignored")]
        [TestCase(typeof(RetryIgnoredOnThirdTryFixture), "Failed", "Failed", "Skipped:Ignored")]
        [TestCase(typeof(RetryErrorOnFirstTryFixture), "Failed:Error")]
        [TestCase(typeof(RetryErrorOnSecondTryFixture), "Failed", "Failed:Error")]
        [TestCase(typeof(RetryErrorOnThirdTryFixture), "Failed", "Failed", "Failed:Error")]
        public void RetryExposesEachResultInTearDown(Type fixtureType, params string[] results)
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(fixtureType);
            ITestResult result = TestBuilder.RunTestFixture(fixture);

            Assert.AreEqual(results.Length, fixture.TearDownResults.Count);
            for (int i = 0; i < results.Length; i++)
                Assert.That(fixture.TearDownResults[i], Is.EqualTo(results[i]), $"Teardown {i} received incorrect result");
        }

        [TestCase(nameof(RetryWithoutSetUpOrTearDownFixture.SucceedsOnThirdTry), "Passed", 3)]
        [TestCase(nameof(RetryWithoutSetUpOrTearDownFixture.FailsEveryTime), "Failed", 3)]
        [TestCase(nameof(RetryWithoutSetUpOrTearDownFixture.ErrorsOnFirstTry), "Failed:Error", 1)]
        public void RetryWorksAsExpectedOnFixturesWithoutSetupOrTeardown(string methodName, string outcome, int nTries)
        {
            var fixture = (RetryWithoutSetUpOrTearDownFixture)Reflect.Construct(typeof(RetryWithoutSetUpOrTearDownFixture));
            ITestResult result = TestBuilder.RunTestCase(fixture, methodName);

            Assert.That(result.ResultState.ToString(), Is.EqualTo(outcome));
            Assert.AreEqual(nTries, fixture.Count);
        }


        [Test]
        public void CategoryWorksWithRetry()
        {
            TestSuite suite = TestBuilder.MakeFixture(typeof(RetryTestWithCategoryFixture));
            Test test = suite.Tests[0] as Test;
            System.Collections.IList categories = test.Properties["Category"];
            Assert.IsNotNull(categories);
            Assert.AreEqual(1, categories.Count);
            Assert.AreEqual("SAMPLE", categories[0]);
        }

        [Test]
        public void RetryUpdatesCurrentRepeatCountPropertyOnAlwaysFailingTest()
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(typeof(RetryTestVerifyAttempt));
            ITestResult result = TestBuilder.RunTestCase(fixture, "NeverPasses");

            Assert.AreEqual(fixture.TearDownResults.Count, fixture.Count + 1, "expected the CurrentRepeatCount property to be one less than the number of executions");
            Assert.AreEqual(result.FailCount, 1, "expected that the test failed all retries");
        }

        [Test]
        public void RetryUpdatesCurrentRepeatCountPropertyOnEachAttempt()
        {
            RepeatingTestsFixtureBase fixture = (RepeatingTestsFixtureBase)Reflect.Construct(typeof(RetryTestVerifyAttempt));
            ITestResult result = TestBuilder.RunTestCase(fixture, "PassesOnLastRetry");

            Assert.AreEqual(fixture.TearDownResults.Count, fixture.Count + 1, "expected the CurrentRepeatCount property to be one less than the number of executions");
            Assert.AreEqual(result.FailCount, 0, "expected that the test passed final retry");
        }
    }
}
