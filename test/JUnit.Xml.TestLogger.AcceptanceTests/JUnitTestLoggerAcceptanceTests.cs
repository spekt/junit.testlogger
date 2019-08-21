// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JUnitTestLoggerAcceptanceTests
    {
        private readonly string resultsFile;
        private readonly XDocument resultsXml;

        public JUnitTestLoggerAcceptanceTests()
        {
            this.resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "test-results.xml");
            this.resultsXml = XDocument.Load(this.resultsFile);
        }

        [ClassInitialize]
        public static void SuiteInitialize(TestContext context)
        {
            DotnetTestFixture.Execute("test-results.xml");
        }

        [TestMethod]
        public void TestRunWithLoggerAndFilePathShouldCreateResultsFile()
        {
            Assert.IsTrue(File.Exists(this.resultsFile));
        }

        [TestMethod]
        public void TestResultFileShouldContainTestRunInformation()
        {
            var node = this.resultsXml.XPathSelectElement("/test-run");

            Assert.IsNotNull(node);
            Assert.AreEqual("48", node.Attribute(XName.Get("testcasecount")).Value);
            Assert.AreEqual("20", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);

            // Start time and End time should be valid dates
            Convert.ToDateTime(node.Attribute(XName.Get("start-time")).Value);
            Convert.ToDateTime(node.Attribute(XName.Get("end-time")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainAssemblyTestSuite()
        {
            var node = this.resultsXml.XPathSelectElement("/test-run/test-suite[@type='Assembly']");

            Assert.IsNotNull(node);
            Assert.AreEqual("48", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("20", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("JUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual(DotnetTestFixture.TestAssembly, node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainNamespaceTestSuiteForNetFull()
        {
            // Two namespaces in test asset are:
            // JUnit.Xml.TestLogger.NetFull.Tests and JUnit.Xml.TestLogger.Tests2
            var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='NetFull']");
            var node = this.resultsXml.XPathSelectElement(query);

            Assert.IsNotNull(node);
            Assert.AreEqual("27", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("13", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("4", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("3", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("JUnit.Xml.TestLogger.NetFull", node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainNamespaceTestSuiteForTests2()
        {
            // Two namespaces in test asset are:
            // JUnit.Xml.TestLogger.NetFull.Tests and JUnit.Xml.TestLogger.Tests2
            var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='Tests2']");
            var node = this.resultsXml.XPathSelectElement(query);

            Assert.IsNotNull(node);
            Assert.AreEqual("21", node.Attribute(XName.Get("total")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("passed")).Value);
            Assert.AreEqual("7", node.Attribute(XName.Get("failed")).Value);
            Assert.AreEqual("4", node.Attribute(XName.Get("inconclusive")).Value);
            Assert.AreEqual("3", node.Attribute(XName.Get("skipped")).Value);
            Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
            Assert.AreEqual("JUnit.Xml.TestLogger.Tests2", node.Attribute(XName.Get("fullname")).Value);
        }

        [TestMethod]
        [DataRow("JUnit.Xml.TestLogger.NetFull.Tests")]
        [DataRow("JUnit.Xml.TestLogger.Tests2")]
        public void TestResultFileShouldContainPartsOfNamespaceTestSuite(string testNamespace)
        {
            // Two namespaces in test asset are:
            // JUnit.Xml.TestLogger.NetFull.Tests and JUnit.Xml.TestLogger.Tests2
            var fullName = string.Empty;
            foreach (var part in testNamespace.Split("."))
            {
                var query = string.Format("/test-run//test-suite[@type='TestSuite' and @name='{0}']", part);
                var node = this.resultsXml.XPathSelectElement(query);
                fullName = fullName == string.Empty ? part : fullName + "." + part;

                Assert.IsNotNull(node);
                Assert.AreEqual("Failed", node.Attribute(XName.Get("result")).Value);
                Assert.AreEqual(fullName, node.Attribute(XName.Get("fullname")).Value);
            }
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCasePropertiesForTestWithPropertyAttributes()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.WithProperty']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(1, propertiesElement.Descendants().Count());

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);
            Assert.AreEqual("Property value", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldNotContainTestCasePropertiesForTestWithNoPropertyAttributes()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.NoProperty']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNull(propertiesElement, "properties element");
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseCategoryForTestWithCategory()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.WithCategory']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(1, propertiesElement.Descendants().Count());

            var propertyElement = propertiesElement.Element("property");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);
            Assert.AreEqual("Junit Test Category", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldNotContainTestCaseCategoryForTestWithMultipleCategory()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.MultipleCategories']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Category2']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);

            // Verify second category
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Category1']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Category", propertyElement.Attribute("name")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCaseCategoryAndPropertyForTestWithMultipleProperties()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.WithCategoryAndProperty']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@name='Category']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("JUnit Test Category", propertyElement.Attribute("value")?.Value);

            // Verify second property
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@name='Property name']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property value", propertyElement.Attribute("value")?.Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCasePropertyForTestWithMultipleProperties()
        {
            var testNamespace = "JUnit.Xml.TestLogger.NetFull.Tests";
            var query = string.Format("/test-run//test-case[@fullname='{0}.UnitTest1.WithProperties']", testNamespace);
            var testCaseElement = this.resultsXml.XPathSelectElement(query);
            Assert.IsNotNull(testCaseElement, "test-case element");

            var propertiesElement = testCaseElement.Element("properties");
            Assert.IsNotNull(propertiesElement, "properties element");
            Assert.AreEqual(2, propertiesElement.Descendants().Count());

            // Verify first category
            var propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Property value 1']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);

            // Verify second property
            propertyElement = propertiesElement.XPathSelectElement("descendant::property[@value='Property value 2']");
            Assert.IsNotNull(propertyElement, "property element");
            Assert.AreEqual("Property name", propertyElement.Attribute("name")?.Value);
        }
    }
}
