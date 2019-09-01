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
                Assert.IsTrue(IsMethodNameOnly(testcase.Attribute("name").Value));
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
                var methodWithoutArgs = RemoveParametersFromMethod(testcase.Attribute("name").Value);

                var classOnly = methodWithoutArgs.Substring(0, methodWithoutArgs.LastIndexOf('.'));

                var assemblyClass = testcase.Attribute("classname").Value;

                // For the class, the namespaces may not match the dll, so we can only check that the method name
                // ends with everything before the method in the method name
                Assert.IsTrue(assemblyClass.EndsWith(classOnly));
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
                var methodFullName = testcase.Attribute("name").Value;
                var stringStart = testcase.Attribute("classname").Value + ".";
                var methodOnly = methodFullName.Substring(stringStart.Length, methodFullName.Length - stringStart.Length);

                Assert.IsTrue(methodFullName.StartsWith(stringStart));
                Assert.IsTrue(IsMethodNameOnly(methodOnly));
            }
        }

        private static bool IsMethodNameOnly(string methodName)
        {
            string baseMethodName = RemoveParametersFromMethod(methodName);

            return baseMethodName.Contains('.') == false;
        }

        private static string RemoveParametersFromMethod(string methodName)
        {
            // The base method is everything before parameters. When there isn't an opening parenthesis
            // then the substring would be null.
            return methodName.Contains('(') ? methodName.Substring(0, methodName.IndexOf('(')) : methodName;
        }
    }
}
