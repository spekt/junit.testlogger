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

    /// <summary>
    /// Acceptance tests evaluate the most recent output of the build.ps1 script, NOT the most
    /// recent build performed by visual studio or dotnet.build
    ///
    /// These acceptance tests look at the specific structure and contents of the produced Xml.
    /// </summary>
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
        public void TestResultFileShouldContainTestSuitesInformation()
        {
            var node = this.resultsXml.XPathSelectElement("/testsuites");

            Assert.IsNotNull(node);
            Assert.AreEqual("JUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual("52", node.Attribute(XName.Get("tests")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failures")).Value);

            Convert.ToDouble(node.Attribute(XName.Get("time")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestSuiteInformation()
        {
            var node = this.resultsXml.XPathSelectElement("/testsuites/testsuite");

            Assert.IsNotNull(node);
            Assert.AreEqual("JUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual("executor://nunit3testexecutor/", node.Attribute(XName.Get("hostname")).Value);
            Assert.AreEqual("52", node.Attribute(XName.Get("tests")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failures")).Value);
            Assert.AreEqual("6", node.Attribute(XName.Get("skipped")).Value);

            // Errors is zero becasue we don't get errors as a test outcome from .net
            Assert.AreEqual("0", node.Attribute(XName.Get("errors")).Value);

            Convert.ToDouble(node.Attribute(XName.Get("time")).Value);
            Convert.ToDateTime(node.Attribute(XName.Get("timestamp")).Value);
        }

        [TestMethod]
        public void TestResultFileShouldContainTestCases()
        {
            var node = this.resultsXml.XPathSelectElements("/testsuites/testsuite").Descendants();
            var testcases = node.Where(x => x.Name.LocalName == "testcase").ToList();

            // Check all test cases
            Assert.IsNotNull(node);
            Assert.AreEqual(52, testcases.Count());
            Assert.IsTrue(testcases.All(x => double.TryParse(x.Attribute("time").Value, out _)));

            // Check failures
            Assert.AreEqual(14, testcases.Where(x => x.Descendants().Any()).Count());
            Assert.IsTrue(testcases.Where(x => x.Descendants().Any())
                                   .All(x => x.Descendants().Count() == 1));
            Assert.IsTrue(testcases.Where(x => x.Descendants().Any())
                                   .All(x => x.Descendants().First().Name.LocalName == "failure"));
            Assert.IsTrue(testcases.Where(x => x.Descendants().Any())
                                   .All(x => x.Descendants().First().Attribute("type").Value == "failure"));
        }

        [TestMethod]
        public void LoggedXmlValidatesAgainstXsdSchema()
        {
            var validator = new JunitXmlValidator();
            var result = validator.IsValid(File.ReadAllText(this.resultsFile));
            Assert.IsTrue(result);
        }
    }
}
