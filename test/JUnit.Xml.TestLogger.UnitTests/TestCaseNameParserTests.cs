// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.UnitTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseNameParserTests
    {
        [DataTestMethod]
        [DataRow("z.a.b", "z", "a", "b")]

        // Cover all expected cases of different parentesis locations, handling normal strings
        [DataRow("z.y.x.a.b(\"arg\",2)", "z.y.x", "a", "b(\"arg\",2)")]
        [DataRow("z.y.x.a(\"arg\",2).b", "z.y.x", "a(\"arg\",2)", "b")]
        [DataRow("z.y.x.a(\"arg\",2).b(\"arg\",2)", "z.y.x", "a(\"arg\",2)", "b(\"arg\",2)")]
        [DataRow("z.y.x.a(\"arg.())(\",2).b", "z.y.x", "a(\"arg.())(\",2)", "b")]

        // Cover select cases with characters in strings that could cause issues
        [DataRow("z.y.x.a.b(\"arg\",\"\\\"\")", "z.y.x", "a", "b(\"arg\",\"\\\"\")")]
        [DataRow("z.y.x.a.b(\"arg\",\")(\")", "z.y.x", "a", "b(\"arg\",\")(\")")]

        // Tests with longer type and method names
        [DataRow("z.y.x.ape.bar(\"ar.g\",\"\\\"\")", "z.y.x", "ape", "bar(\"ar.g\",\"\\\"\")")]
        [DataRow("z.y.x.ape.bar(\"ar.g\",\")(\")", "z.y.x", "ape", "bar(\"ar.g\",\")(\")")]
        public void Parse_ParsesAllParsableInputs_WithoutConsoleOutput(string testCaseName, string expectedNamespace, string expectedType, string expectedMethod)
        {
            var expected = new Tuple<string, string>(expectedType, expectedMethod);

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                var actual = TestCaseNameParser.Parse(testCaseName);

                Assert.AreEqual(expectedNamespace, actual.NamespaceName);
                Assert.AreEqual(expectedType, actual.TypeName);
                Assert.AreEqual(expectedMethod, actual.MethodName);
                Assert.AreEqual(0, sw.ToString().Length);
            }
        }

        [DataTestMethod]
        [DataRow("a.b", TestCaseNameParser.TestCaseParserUnknownNamsepace, "a", "b")]

        // Cover all expected cases of different parentesis locations, handling normal strings
        [DataRow("a.b(\"arg\",2)", TestCaseNameParser.TestCaseParserUnknownNamsepace, "a", "b(\"arg\",2)")]
        [DataRow("a(\"arg\",2).b", TestCaseNameParser.TestCaseParserUnknownNamsepace, "a(\"arg\",2)", "b")]
        [DataRow("a(\"arg\",2).b(\"arg\",2)", TestCaseNameParser.TestCaseParserUnknownNamsepace, "a(\"arg\",2)", "b(\"arg\",2)")]

        // Examples with period in non string
        [DataRow("a.b(0.5f)", TestCaseNameParser.TestCaseParserUnknownNamsepace, "a", "b(0.5f)")]
        public void Parse_ParsesAllParsableInputsWithoutNamespace_WithoutConsoleOutput(string testCaseName, string expectedNamespace, string expectedType, string expectedMethod)
        {
            var expectedConsole = string.Format(
                    TestCaseNameParser.TestCaseParserErrorTemplate,
                    testCaseName,
                    expectedNamespace,
                    expectedType,
                    expectedMethod);

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                var actual = TestCaseNameParser.Parse(testCaseName);

                Assert.AreEqual(expectedNamespace, actual.NamespaceName);
                Assert.AreEqual(expectedType, actual.TypeName);
                Assert.AreEqual(expectedMethod, actual.MethodName);

                // Remove the trailing new line before comparing.
                Assert.AreEqual(expectedConsole, sw.ToString().Replace(sw.NewLine, string.Empty));
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
        public void Parse_FailsGracefullyOnNonParsableInputs_WithConsoleOutput(string testCaseName)
        {
            var expectedConsole = string.Format(
                TestCaseNameParser.TestCaseParserErrorTemplate,
                testCaseName,
                TestCaseNameParser.TestCaseParserUnknownNamsepace,
                TestCaseNameParser.TestCaseParserUnknownType,
                testCaseName);

            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                var actual = TestCaseNameParser.Parse(testCaseName);

                Assert.AreEqual(TestCaseNameParser.TestCaseParserUnknownNamsepace, actual.NamespaceName);
                Assert.AreEqual(TestCaseNameParser.TestCaseParserUnknownType, actual.TypeName);
                Assert.AreEqual(testCaseName, actual.MethodName);

                // Remove the trailing new line before comparing.
                Assert.AreEqual(expectedConsole, sw.ToString().Replace(sw.NewLine, string.Empty));
            }
        }
    }
}
