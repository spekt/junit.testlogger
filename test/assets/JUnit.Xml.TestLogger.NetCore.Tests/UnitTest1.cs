using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace JUnit.Xml.TestLogger.NetFull.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        [Description("Passing test description")]
        public async Task PassTest11()
        {
            Console.WriteLine("{2010CAE3-7BC0-4841-A5A3-7D5F947BB9FB}");
            Console.WriteLine("{998AC9EC-7429-42CD-AD55-72037E7AF3D8}");
            await Task.Delay(TimeSpan.FromMilliseconds(400));
        }

        [Test]
        public void FailTest11()
        {
            Console.WriteLine("{EEEE1DA6-6296-4486-BDA5-A50A19672F0F}");
            Console.WriteLine("{C33FF4B5-75E1-4882-B968-DF9608BFE7C2}");
            Console.Error.WriteLine("{D46DFA10-EEDD-49E5-804D-FE43051331A7}");
            Console.Error.WriteLine("{33F5FD22-6F40-499D-98E4-481D87FAEAA1}");
         
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

        [Test]
        [Property("Property name", "Property value")]
        public void WithProperty()
        {
        }

        [Test]
        public void NoProperty()
        {
        }

        [Test]
        [Category("Junit Test Category")]
        public void WithCategory()
        {
        }

        [Test]
        [Category("Category2")]
        [Category("Category1")]
        public void MultipleCategories()
        {
        }

        [Test]
        [Category("JUnit Test Category")]
        [Property("Property name", "Property value")]
        public void WithCategoryAndProperty()
        {
        }

        [Test]
        [Property("Property name", "Property value 1")]
        [Property("Property name", "Property value 2")]
        public void WithProperties()
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