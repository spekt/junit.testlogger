// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Spekt.TestLogger;

    [FriendlyName(FriendlyName)]
    [ExtensionUri(ExtensionUri)]
    public class JUnitXmlTestLogger : TestLogger
    {
        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/JUnitXmlLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "junit";

        // Dicionary keys for command line arguments.
        public const string MethodFormatKey = "MethodFormat";
        public const string FailureBodyFormatKey = "FailureBodyFormat";

        public enum MethodFormat
        {
            /// <summary>
            /// The method format will be the method only (i.e. Class.Method())
            /// </summary>
            Default,

            /// <summary>
            /// The method format will include the class and method name (i.e. Class.Method())
            /// </summary>
            Class,

            /// <summary>
            /// The method format will include the namespace, class and method (i.e. Namespace.Class.Method())
            /// </summary>
            Full
        }

        public enum FailureBodyFormat
        {
            /// <summary>
            /// The failure body will incldue only the error stack trace.
            /// </summary>
            Default,

            /// <summary>
            /// The failure body will incldue the Expected/Actual messages.
            /// </summary>
            Verbose
        }

        public MethodFormat MethodFormatOption { get; private set; } = MethodFormat.Default;

        public FailureBodyFormat FailureBodyFormatOption { get; private set; } = FailureBodyFormat.Default;

        public IEnumerable<TestSuite> GroupTestSuites(IEnumerable<TestSuite> suites)
        {
            var groups = suites;
            var roots = new List<TestSuite>();
            while (groups.Any())
            {
                groups = groups.GroupBy(r =>
                {
                    var name = r.FullName.SubstringBeforeDot();
                    if (string.IsNullOrEmpty(name))
                    {
                        roots.Add(r);
                    }

                    return name;
                })
                .OrderBy(g => g.Key)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => this.AggregateTestSuites(g, "TestSuite", g.Key.SubstringAfterDot(), g.Key))
                .ToList();
            }

            return roots;
        }

        public override string GetExtensionUri() => ExtensionUri;

        public override string GetFriendlyName() => FriendlyName;

        /// <summary>
        /// Initialized called by dotnet test.
        /// </summary>
        /// <param name="parameters">Dictionary of key value pairs provided by the user, semicolon delimited (i.e. 'key1=val1;key2=val2').</param>
        public override void Initialize(Dictionary<string, string> parameters)
        {
            if (parameters.TryGetValue(MethodFormatKey, out string methodFormat))
            {
                if (string.Equals(methodFormat.Trim(), "Class", StringComparison.OrdinalIgnoreCase))
                {
                    this.MethodFormatOption = MethodFormat.Class;
                }
                else if (string.Equals(methodFormat.Trim(), "Full", StringComparison.OrdinalIgnoreCase))
                {
                    this.MethodFormatOption = MethodFormat.Full;
                }
                else if (string.Equals(methodFormat.Trim(), "Default", StringComparison.OrdinalIgnoreCase))
                {
                    this.MethodFormatOption = MethodFormat.Default;
                }
                else
                {
                    Console.WriteLine($"JunitXML Logger: The provided Method Format '{methodFormat}' is not a recognized option. Using default");
                }
            }

            if (parameters.TryGetValue(FailureBodyFormatKey, out string failureFormat))
            {
                if (string.Equals(failureFormat.Trim(), "Verbose", StringComparison.OrdinalIgnoreCase))
                {
                    this.FailureBodyFormatOption = FailureBodyFormat.Verbose;
                }
                else if (string.Equals(failureFormat.Trim(), "Default", StringComparison.OrdinalIgnoreCase))
                {
                    this.FailureBodyFormatOption = FailureBodyFormat.Default;
                }
                else
                {
                    Console.WriteLine($"JunitXML Logger: The provided Failure Body Format '{failureFormat}' is not a recognized option. Using default");
                }
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        /// <returns>Returns the log file as a string.</returns>
        public override string BuildLog(List<TestResultInfo> resultList)
        {
            var doc = new XDocument(this.CreateTestSuitesElement(resultList));
            return doc.ToString();
        }

        private TestSuite AggregateTestSuites(
           IEnumerable<TestSuite> suites,
           string testSuiteType,
           string name,
           string fullName)
        {
            var element = new XElement("test-suite");

            int total = 0;
            int passed = 0;
            int failed = 0;
            int skipped = 0;
            int inconclusive = 0;
            int error = 0;
            var time = TimeSpan.Zero;

            foreach (var result in suites)
            {
                total += result.Total;
                passed += result.Passed;
                failed += result.Failed;
                skipped += result.Skipped;
                inconclusive += result.Inconclusive;
                error += result.Error;
                time += result.Time;

                element.Add(result.Element);
            }

            element.SetAttributeValue("type", testSuiteType);
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("fullname", fullName);
            element.SetAttributeValue("total", total);
            element.SetAttributeValue("passed", passed);
            element.SetAttributeValue("failed", failed);
            element.SetAttributeValue("inconclusive", inconclusive);
            element.SetAttributeValue("skipped", skipped);

            var resultString = failed > 0 ? ResultStatusFailed : ResultStatusPassed;
            element.SetAttributeValue("result", resultString);
            element.SetAttributeValue("duration", time.TotalSeconds);

            return new TestSuite
            {
                Element = element,
                Name = name,
                FullName = fullName,
                Total = total,
                Passed = passed,
                Failed = failed,
                Inconclusive = inconclusive,
                Skipped = skipped,
                Error = error,
                Time = time
            };
        }

        private XElement CreateTestSuitesElement(List<TestResultInfo> results)
        {
            var assemblies = results.Select(x => x.AssemblyPath).Distinct().ToList();
            var testsuiteElements = assemblies
                .Select(a => this.CreateTestSuiteElement(results.Where(x => x.AssemblyPath == a).ToList()));

            var element = new XElement("testsuites", testsuiteElements);

            element.SetAttributeValue("name", Path.GetFileName(results.First().AssemblyPath));

            element.SetAttributeValue("tests", results.Count);
            element.SetAttributeValue("failures", results.Where(x => x.Outcome == TestOutcome.Failed).Count());
            element.SetAttributeValue("time", results.Sum(x => x.Duration.TotalSeconds));

            return element;
        }

        private XElement CreateTestSuiteElement(List<TestResultInfo> results)
        {
            var testCaseElements = results.Select(a => this.CreateTestCaseElement(a));

            var element = new XElement("testsuite", testCaseElements);

            element.SetAttributeValue("name", Path.GetFileName(results.First().AssemblyPath));

            element.SetAttributeValue("tests", results.Count);
            element.SetAttributeValue("skipped", results.Where(x => x.Outcome == TestOutcome.Skipped).Count());
            element.SetAttributeValue("failures", results.Where(x => x.Outcome == TestOutcome.Failed).Count());
            element.SetAttributeValue("errors", 0); // looks like this isn't supported by .net?
            element.SetAttributeValue("time", results.Sum(x => x.Duration.TotalSeconds));
            element.SetAttributeValue("timestamp", this.LocalStartTime.ToString(DateFormat, CultureInfo.InvariantCulture));
            element.SetAttributeValue("hostname", results.First().TestCase.ExecutorUri);

            return element;
        }

        private XElement CreateTestCaseElement(TestResultInfo result)
        {
            var testcaseElement = new XElement("testcase");

            var namespaceClass = result.Namespace + "." + result.Type;

            testcaseElement.SetAttributeValue("classname", namespaceClass);

            if (this.MethodFormatOption == MethodFormat.Full)
            {
                testcaseElement.SetAttributeValue("name", namespaceClass + "." + result.Name);
            }
            else if (this.MethodFormatOption == MethodFormat.Class)
            {
                testcaseElement.SetAttributeValue("name", result.Type + "." + result.Name);
            }
            else
            {
                testcaseElement.SetAttributeValue("name", result.Name);
            }

            testcaseElement.SetAttributeValue("file", result.TestCase.Source);
            testcaseElement.SetAttributeValue("time", result.Duration.TotalSeconds);

            if (result.Outcome == TestOutcome.Failed)
            {
                var failureBodySB = new StringBuilder();

                if (this.FailureBodyFormatOption == FailureBodyFormat.Verbose)
                {
                    failureBodySB.AppendLine(result.ErrorMessage);

                    // Stack trace included to mimic the normal test output
                    failureBodySB.AppendLine("Stack Trace:");
                }

                failureBodySB.AppendLine(result.ErrorStackTrace);

                var failureElement = new XElement("failure", failureBodySB.ToString());

                failureElement.SetAttributeValue("type", "failure"); // TODO are there failure types?
                failureElement.SetAttributeValue("message", result.ErrorMessage);

                testcaseElement.Add(failureElement);
            }

            return testcaseElement;
        }

        public class TestSuite
        {
            public XElement Element { get; set; }

            public string Name { get; set; }

            public string FullName { get; set; }

            public int Total { get; set; }

            public int Passed { get; set; }

            public int Failed { get; set; }

            public int Inconclusive { get; set; }

            public int Skipped { get; set; }

            public int Error { get; set; }

            public TimeSpan Time { get; set; }
        }
    }
}
