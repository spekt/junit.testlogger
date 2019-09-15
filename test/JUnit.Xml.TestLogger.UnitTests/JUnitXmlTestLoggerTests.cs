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
    using Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TestSuite = Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger.JUnitXmlTestLogger.TestSuite;

    [TestClass]
    public class JUnitXmlTestLoggerTests
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

            var result = JUnitXmlTestLogger.GroupTestSuites(new[] { suite1, suite2 }).ToArray();

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

            var result = JUnitXmlTestLogger.GroupTestSuites(suites).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("c", result[0].Name);
            Assert.AreEqual(expectedXmlForC, result[0].Element.ToString(SaveOptions.DisableFormatting));
            Assert.AreEqual("a", result[1].Name);
            Assert.AreEqual(expectedXmlForA, result[1].Element.ToString(SaveOptions.DisableFormatting));
        }

        [DataTestMethod]
        [DataRow("a.b", "a", "b")]

        // Cover all expected cases of different parentesis locations, handling normal strings
        [DataRow("z.y.x.a.b(\"arg\",2)", "a", "b(\"arg\",2)")]
        [DataRow("a.b(\"arg\",2)", "a", "b(\"arg\",2)")]
        [DataRow("a(\"arg\",2).b", "a(\"arg\",2)", "b")]
        [DataRow("z.y.x.a(\"arg\",2).b", "a(\"arg\",2)", "b")]
        [DataRow("a(\"arg\",2).b(\"arg\",2)", "a(\"arg\",2)", "b(\"arg\",2)")]
        [DataRow("z.y.x.a(\"arg\",2).b(\"arg\",2)", "a(\"arg\",2)", "b(\"arg\",2)")]
        [DataRow("z.y.x.a(\"arg.())(\",2).b", "a(\"arg.())(\",2)", "b")]

        // Examples with period in non string
        [DataRow("a.b(0.5f)", "a", "b(0.5f)")]

        // Cover select cases with characters in strings that could cause issues
        [DataRow("z.y.x.a.b(\"arg\",\"\\\"\")", "a", "b(\"arg\",\"\\\"\")")]
        [DataRow("z.y.x.a.b(\"arg\",\")(\")", "a", "b(\"arg\",\")(\")")]

        // Tests with longer type and method names
        [DataRow("z.y.x.ape.bar(\"ar.g\",\"\\\"\")", "ape", "bar(\"ar.g\",\"\\\"\")")]
        [DataRow("z.y.x.ape.bar(\"ar.g\",\")(\")", "ape", "bar(\"ar.g\",\")(\")")]
        public void ParseTestCaseName_ParsesAllParsableInputs_WithoutConsoleOutput(string testCaseName, string expectedType, string expectedMethod)
        {
            var expected = new Tuple<string, string>(expectedType, expectedMethod);

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                Assert.AreEqual(expected, JUnitXmlTestLogger.ParseTestCaseName(testCaseName));
                Assert.AreEqual(0, sw.ToString().Length);
            }
        }

        [DataTestMethod]
        [DataRow("x.()x()")]
        [DataRow("x")]
        [DataRow("z.y.X(x")]
        [DataRow("z.y.x(")]
        [DataRow("z.y.X\"x")]
        [DataRow("z.y.x\"")]
        [DataRow("z.y.X\\x")]
        [DataRow("z.y.x\\")]
        [DataRow("z.y.X\\)")]
        [DataRow("z.y.x))")]
        [DataRow("z.y.x()x")]
        [DataRow("z.y.x.")]
        [DataRow("z.y.x.)")]
        [DataRow("z.y.x.\"\")")]
        public void ParseTestCaseName_FailsGracefullyOnNonParsableInputs_WithConsoleOutput(string testCaseName)
        {
            var expected = new Tuple<string, string>(JUnitXmlTestLogger.TestCaseParserUnknownType, testCaseName);
            var expectedConsole = string.Format(JUnitXmlTestLogger.TestCaseParserErrorTemplate, testCaseName);

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);

                Assert.AreEqual(expected, JUnitXmlTestLogger.ParseTestCaseName(testCaseName));

                // Remove the trailing new line before comparing.
                Assert.AreEqual(expectedConsole, sw.ToString().Replace(sw.NewLine, string.Empty));
            }
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
