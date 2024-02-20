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
        }

        [TestMethod]
        public void TestResultFileShouldContainTestSuiteInformation()
        {
            var node = this.resultsXml.XPathSelectElement("/testsuites/testsuite");

            Assert.IsNotNull(node);
            Assert.AreEqual("JUnit.Xml.TestLogger.NetCore.Tests.dll", node.Attribute(XName.Get("name")).Value);
            Assert.AreEqual(Environment.MachineName, node.Attribute(XName.Get("hostname")).Value);

            Assert.AreEqual("52", node.Attribute(XName.Get("tests")).Value);
            Assert.AreEqual("14", node.Attribute(XName.Get("failures")).Value);
            Assert.AreEqual("8", node.Attribute(XName.Get("skipped")).Value);

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
            var failures = testcases
                .Where(x => x.Descendants().Any(y => y.Name.LocalName == "failure"))
                .ToList();

            Assert.AreEqual(14, failures.Count());
            Assert.IsTrue(failures.All(x => x.Descendants().Any(element => element.Attribute("type").Value == "failure")));

            // Check failures
            var skips = testcases
                .Where(x => x.Descendants().Any(y => y.Name.LocalName == "skipped"))
                .ToList();

            Assert.AreEqual(8, skips.Count());
        }

        [TestMethod]
        public void TestCasesShouldContainStandardOut()
        {
            var node = this.resultsXml.XPathSelectElements("/testsuites/testsuite").Descendants();
            var testcases = node.Where(x => x.Name.LocalName == "testcase").ToList();

            var testCasesWithSystemOut = testcases
                .Where(x => x.Descendants().Any(y => y.Name.LocalName == "system-out"))
                .ToList();

            Assert.AreEqual(2, testCasesWithSystemOut.Count());
        }

        [TestMethod]
        public void TestResultFileShouldContainStandardOut()
        {
            var node = this.resultsXml.XPathSelectElement("/testsuites/testsuite/system-out");

            Assert.IsTrue(node.Value.Contains("{2010CAE3-7BC0-4841-A5A3-7D5F947BB9FB}"));
            Assert.IsTrue(node.Value.Contains("{998AC9EC-7429-42CD-AD55-72037E7AF3D8}"));
            Assert.IsTrue(node.Value.Contains("{EEEE1DA6-6296-4486-BDA5-A50A19672F0F}"));
            Assert.IsTrue(node.Value.Contains("{C33FF4B5-75E1-4882-B968-DF9608BFE7C2}"));
        }

        [TestMethod]
        public void TestResultFileShouldContainErrorOut()
        {
            var node = this.resultsXml.XPathSelectElement("/testsuites/testsuite/system-err");

            Assert.IsTrue(node.Value.Contains("{D46DFA10-EEDD-49E5-804D-FE43051331A7}"));
            Assert.IsTrue(node.Value.Contains("{33F5FD22-6F40-499D-98E4-481D87FAEAA1}"));
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
