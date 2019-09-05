// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Acceptance tests evaluate the most recent output of the build.ps1 script, NOT the most
    /// recent build performed by visual studio or dotnet.build
    ///
    /// These acceptance tests look at the specific places output is expected to change because of the format option specified.
    /// Accordingly, these tests cannot protect against other changes occurring due to the formatting option.
    /// </summary>
    [TestClass]
    public class JUnitTestLoggerFormatOptionsAcceptanceTests
    {
        [TestMethod]
        public void FailureBodyFormat_Default_ShouldntStartWithMessage()
        {
            DotnetTestFixture.Execute("failure-default-test-results.xml;FailureBodyFormat=Default");
            string resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "failure-default-test-results.xml");
            XDocument resultsXml = XDocument.Load(resultsFile);

            var failures = resultsXml.XPathSelectElements("/testsuites/testsuite")
                .Descendants()
                .Where(x => x.Name.LocalName == "failure")
                .ToList();

            foreach (var failure in failures)
            {
                // Strip new line and carrige return. These may be inconsistent depending on environment settings
                var message = failure.Attribute("message").Value.Replace("\r", string.Empty).Replace("\n", string.Empty);
                var body = failure.Value.Replace("\r", string.Empty).Replace("\n", string.Empty);

                Assert.IsFalse(body.StartsWith(message));
            }
        }

        [TestMethod]
        public void FailureBodyFormat_Verbose_ShouldStartWithMessage()
        {
            DotnetTestFixture.Execute("failure-verbose-test-results.xml;FailureBodyFormat=Verbose");
            string resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "failure-verbose-test-results.xml");
            XDocument resultsXml = XDocument.Load(resultsFile);

            var failures = resultsXml.XPathSelectElements("/testsuites/testsuite")
                .Descendants()
                .Where(x => x.Name.LocalName == "failure")
                .ToList();

            foreach (var failure in failures)
            {
                // Strip new line and carrige return. These may be inconsistent depending on environment settings
                var message = failure.Attribute("message").Value.Replace("\r", string.Empty).Replace("\n", string.Empty);
                var body = failure.Value.Replace("\r", string.Empty).Replace("\n", string.Empty);

                Assert.IsTrue(body.StartsWith(message));
            }
        }

        [TestMethod]
        public void MethodFormat_Default_ShouldBeOnlyTheMethod()
        {
            DotnetTestFixture.Execute("method-default-test-results.xml;MethodFormat=Default");
            string resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "method-default-test-results.xml");
            XDocument resultsXml = XDocument.Load(resultsFile);

            var testcases = resultsXml.XPathSelectElements("/testsuites/testsuite")
                .Descendants()
                .Where(x => x.Name.LocalName == "testcase")
                .ToList();

            foreach (var testcase in testcases)
            {
                var parsedName = JUnitXmlTestLogger.ParseTestCaseName(testcase.Attribute("name").Value);

                // A method name only will not be parsable into two pieces
                Assert.AreEqual(parsedName.Item1, JUnitXmlTestLogger.TestCaseParserUnknownType);
            }
        }

        [TestMethod]
        public void MethodFormat_Class_ShouldIncludeClass()
        {
            DotnetTestFixture.Execute("method-class-test-results.xml;MethodFormat=Class");
            string resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "method-class-test-results.xml");
            XDocument resultsXml = XDocument.Load(resultsFile);

            var testcases = resultsXml.XPathSelectElements("/testsuites/testsuite")
                .Descendants()
                .Where(x => x.Name.LocalName == "testcase")
                .ToList();

            foreach (var testcase in testcases)
            {
                var parsedName = JUnitXmlTestLogger.ParseTestCaseName(testcase.Attribute("name").Value);

                // If the name is parsable into two pieces, then we have a two piece name
                // and consider that to be a passing result.
                Assert.AreNotEqual(parsedName.Item1, JUnitXmlTestLogger.TestCaseParserUnknownType);
            }
        }

        [TestMethod]
        public void MethodFormat_Full_ShouldIncludeNamespaceAndClass()
        {
            DotnetTestFixture.Execute("method-full-test-results.xml;MethodFormat=Full");
            string resultsFile = Path.Combine(DotnetTestFixture.RootDirectory, "method-full-test-results.xml");
            XDocument resultsXml = XDocument.Load(resultsFile);

            var testcases = resultsXml.XPathSelectElements("/testsuites/testsuite")
                .Descendants()
                .Where(x => x.Name.LocalName == "testcase")
                .ToList();

            foreach (var testcase in testcases)
            {
                var parsedName = JUnitXmlTestLogger.ParseTestCaseName(testcase.Attribute("name").Value);

                // We expect the full name would be the class name plus the parsed method
                var actualFullName = testcase.Attribute("classname").Value + "." + parsedName.Item2;

                // If the name is parsable into two pieces, then we have at least a two piece name
                Assert.AreNotEqual(parsedName.Item1, JUnitXmlTestLogger.TestCaseParserUnknownType);
                Assert.AreEqual(actualFullName, testcase.Attribute("name").Value);
            }
        }
    }
}
