// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.UnitTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestPlatform.Extension.Junit.Xml.TestLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestSuite = Microsoft.VisualStudio.TestPlatform.Extension.Junit.Xml.TestLogger.JunitXmlSerializer.TestSuite;

    [TestClass]
    public class JUnitXmlTestSerializerTests
    {
        private const string DummyTestResultsDirectory = "/tmp/testresults";

        [TestMethod]
        public void InitializeShouldThrowIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new JUnitXmlTestLogger().Initialize(null, DummyTestResultsDirectory));
        }

        [TestMethod]
        public void CreateTestSuiteShouldReturnEmptyGroupsIfTestSuitesAreExclusive()
        {
            var suite1 = CreateTestSuite("a.b");
            var suite2 = CreateTestSuite("c.d");

            var result = JunitXmlSerializer.GroupTestSuites(new[] { suite1, suite2 }).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("a", result[0].Name);
            Assert.AreEqual("c", result[1].Name);
        }

        [TestMethod]
        public void CreateTestSuiteShouldGroupTestSuitesByName()
        {
            var suites = new[] { CreateTestSuite("a.b.c"), CreateTestSuite("a.b.e"), CreateTestSuite("c.d") };
            var expectedXmlForA = @"<test-suite type=""TestSuite"" name=""a"" fullname=""a"" total=""10"" passed=""2"" failed=""2"" inconclusive=""2"" skipped=""2"" result=""Failed"" duration=""0""><test-suite type=""TestSuite"" name=""b"" fullname=""a.b"" total=""10"" passed=""2"" failed=""2"" inconclusive=""2"" skipped=""2"" result=""Failed"" duration=""0""><test-suite /><test-suite /></test-suite></test-suite>";
            var expectedXmlForC = @"<test-suite type=""TestSuite"" name=""c"" fullname=""c"" total=""5"" passed=""1"" failed=""1"" inconclusive=""1"" skipped=""1"" result=""Failed"" duration=""0""><test-suite /></test-suite>";

            var result = JunitXmlSerializer.GroupTestSuites(suites).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("c", result[0].Name);
            Assert.AreEqual(expectedXmlForC, result[0].Element.ToString(SaveOptions.DisableFormatting));
            Assert.AreEqual("a", result[1].Name);
            Assert.AreEqual(expectedXmlForA, result[1].Element.ToString(SaveOptions.DisableFormatting));
        }

        private static TestSuite CreateTestSuite(string name)
        {
            return new TestSuite
            {
                Element = new XElement("test-suite"),
                Name = "n",
                FullName = name,
                Total = 5,
                Passed = 1,
                Failed = 1,
                Inconclusive = 1,
                Skipped = 1,
                Error = 1
            };
        }
    }
}
