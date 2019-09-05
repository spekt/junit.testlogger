using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace JUnit.Xml.TestLogger.Tests2
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        [Description("Passing test description")]
        public async Task PassTest11()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400));
        }

        [Test]
        public void FailTest11()
        {
            Assert.False(true);
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive("test inconclusive");
        }

        [Test]
        [Ignore("ignore reason")]
        public void Ignored()
        {
        }
    }

    public class UnitTest2
    {
        [Test]
        [Category("passing category")]
        public void PassTest21()
        {
            Assert.That(2, Is.EqualTo(2));
        }

        [Test]
        [Category("failing category")]
        public void FailTest22()
        {
            Assert.False(true);
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive();
        }

        [Test]
        [Ignore("ignore reason")]
        public void IgnoredTest()
        {
        }

        [Test]
        public void WarningTest()
        {
            Assert.Warn("Warning");
        }

        [Test]
        [Explicit]
        public void ExplicitTest()
        {
        }
    }

    [TestFixture]
    public class SuccessFixture
    {
        [Test]
        public void SuccessTest()
        {
        }
    }

    [TestFixture]
    public class SuccessAndInconclusiveFixture
    {
        [Test]
        public void SuccessTest()
        {
        }

        [Test]
        public void InconclusiveTest()
        {
            Assert.Inconclusive();
        }
    }

    [TestFixture]
    public class FailingOneTimeSetUp
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void TestA()
        {
        }
    }

    [TestFixture]
    public class FailingTestSetup
    {
        [SetUp]
        public void SetUp()
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void TestB()
        {
        }
    }

    [TestFixture]
    public class FailingTearDown
    {
        [TearDown]
        public void TearDown()
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void TestC()
        {
        }
    }

    [TestFixture]
    public class FailingOneTimeTearDown
    {
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void TestD()
        {
        }
    }

    [TestFixture]
    [TestFixtureSource("FixtureArgs")]
    public class ParametrizedFixture
    {
        public ParametrizedFixture(string word, int num)
        {
        }

        [Test]
        public void TestE()
        {
        }

        static object[] FixtureArgs =
        {
            new object[] {"Question", 1},
            new object[] {"Answer", 42}
        };
    }

    [TestFixture]
    public class ParametrizedTestCases
    {
        [Test]
        public void TestData([Values(1, 2)] int x, [Values("A", "B")] string s)
        {
            Assert.That(x, Is.Not.EqualTo(2), "failing for second case");
            Assert.That(s, Is.Not.Null);
        }
    }
}
