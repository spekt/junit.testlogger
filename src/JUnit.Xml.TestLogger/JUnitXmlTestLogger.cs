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
    using Newtonsoft.Json;

    [FriendlyName(FriendlyName)]
    [ExtensionUri(ExtensionUri)]
    public class JUnitXmlTestLogger : ITestLoggerWithParameters
    {
        /// <summary>
        /// Uri used to uniquely identify the logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/JUnitXmlLogger/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "junit";

        public const string LogFilePathKey = "LogFilePath";
        public const string LogFileName = "LogFileName";

        public const string ResultDirectoryKey = "TestRunDirectory";

        private const string ResultStatusPassed = "Passed";
        private const string ResultStatusFailed = "Failed";

        private const string DateFormat = "yyyy-MM-ddT HH:mm:ssZ";

        private const string AssemblyToken = "{assembly}";
        private const string FrameworkToken = "{framework}";

        private readonly object resultsGuard = new object();
        private string outputFilePath;

        private List<TestResultInfo> results;
        private DateTime localStartTime;

        public static IEnumerable<TestSuite> GroupTestSuites(IEnumerable<TestSuite> suites)
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
                                .Select(g => AggregateTestSuites(g, "TestSuite", g.Key.SubstringAfterDot(), g.Key))
                                .ToList();
            }

            return roots;
        }

        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (testResultsDirPath == null)
            {
                throw new ArgumentNullException(nameof(testResultsDirPath));
            }

            var outputPath = Path.Combine(testResultsDirPath, "TestResults.xml");
            this.InitializeImpl(events, outputPath);
        }

        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.TryGetValue(LogFileName, out string outputPathName) && parameters.TryGetValue(ResultDirectoryKey, out string outputFileDirectory))
            {
                outputPathName = Path.Combine(outputFileDirectory, outputPathName);
                this.InitializeImpl(events, outputPathName);
            }
            else if (parameters.TryGetValue(LogFilePathKey, out string outputPath))
            {
                this.InitializeImpl(events, outputPath);
            }
            else if (parameters.TryGetValue(DefaultLoggerParameterNames.TestRunDirectory, out string outputDir))
            {
                this.Initialize(events, outputDir);
            }
            else
            {
                throw new ArgumentException($"Expected {LogFilePathKey} or {DefaultLoggerParameterNames.TestRunDirectory} parameter", nameof(parameters));
            }
        }

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when a test starts.
        /// </summary>
        internal void TestRunStartHandler(object sender, TestRunStartEventArgs e)
        {
            if (this.outputFilePath.Contains(AssemblyToken))
            {
                string assemblyPath = e.TestRunCriteria.AdapterSourceMap["_none_"].First();
                string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                this.outputFilePath = this.outputFilePath.Replace(AssemblyToken, assemblyName);
            }

            if (this.outputFilePath.Contains(FrameworkToken))
            {
                XmlDocument runSettings = new XmlDocument();
                runSettings.LoadXml(e.TestRunCriteria.TestRunSettings);
                XmlNode x = runSettings.GetElementsByTagName("TargetFrameworkVersion")[0];
                string framework = x.InnerText;
                framework = framework.Replace(",Version=v", string.Empty).Replace(".", string.Empty);
                this.outputFilePath = this.outputFilePath.Replace(FrameworkToken, framework);
            }
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        internal void TestResultHandler(object sender, TestResultEventArgs e)
        {
            TestResult result = e.Result;

            if (TryParseName(result.TestCase.FullyQualifiedName, out var typeName, out var methodName, out _))
            {
                lock (this.resultsGuard)
                {
                    this.results.Add(new TestResultInfo(
                        result,
                        typeName,
                        methodName));
                }
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            Console.WriteLine("..");
            Console.WriteLine("Hello from the JUNIT logger");
            try
            {
                List<TestResultInfo> resultList;
                lock (this.resultsGuard)
                {
                    resultList = this.results;
                    this.results = new List<TestResultInfo>();
                }

                var doc = new XDocument(this.CreateTestSuitesElement(resultList));

                // Create directory if not exist
                var loggerFileDirPath = Path.GetDirectoryName(this.outputFilePath);
                if (!Directory.Exists(loggerFileDirPath))
                {
                    Directory.CreateDirectory(loggerFileDirPath);
                }

                using (var f = File.Create(this.outputFilePath))
                {
                    doc.Save(f);
                }

                var resultsFileMessage = string.Format(CultureInfo.CurrentCulture, "Results File: {0}", this.outputFilePath);
                Console.WriteLine(resultsFileMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in the logger");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.Source);
                throw;
            }
        }

        private static TestSuite AggregateTestSuites(
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

        private static IEnumerable<XElement> CreatePropertyElement(string name, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("message", nameof(name));
            }

            foreach (var value in values)
            {
                yield return new XElement(
                "property",
                new XAttribute("name", name),
                new XAttribute("value", value));
            }
        }

        private static bool TryParseName(
            string testCaseName,
            out string metadataTypeName,
            out string metadataMethodName,
            out string metadataMethodArguments)
        {
            // This is fragile. The FQN is constructed by a test adapter.
            // There is no enforcement that the FQN starts with metadata type name.
            string typeAndMethodName;
            var methodArgumentsStart = testCaseName.IndexOf('(');

            if (methodArgumentsStart == -1)
            {
                typeAndMethodName = testCaseName.Trim();
                metadataMethodArguments = string.Empty;
            }
            else
            {
                typeAndMethodName = testCaseName.Substring(0, methodArgumentsStart).Trim();
                metadataMethodArguments = testCaseName.Substring(methodArgumentsStart).Trim();

                if (metadataMethodArguments[metadataMethodArguments.Length - 1] != ')')
                {
                    metadataTypeName = null;
                    metadataMethodName = null;
                    metadataMethodArguments = null;
                    return false;
                }
            }

            var typeNameLength = typeAndMethodName.LastIndexOf('.');
            var methodNameStart = typeNameLength + 1;

            if (typeNameLength <= 0 || methodNameStart == typeAndMethodName.Length)
            {
                // No typeName is available
                metadataTypeName = null;
                metadataMethodName = null;
                metadataMethodArguments = null;
                return false;
            }

            metadataTypeName = typeAndMethodName.Substring(0, typeNameLength).Trim();
            metadataMethodName = typeAndMethodName.Substring(methodNameStart).Trim();
            return true;
        }

        private static string OutcomeToString(TestOutcome outcome)
        {
            switch (outcome)
            {
                case TestOutcome.Failed:
                    return ResultStatusFailed;

                case TestOutcome.Passed:
                    return "Passed";

                case TestOutcome.Skipped:
                    return "Skipped";

                default:
                    return "Inconclusive";
            }
        }

        private void InitializeImpl(TestLoggerEvents events, string outputPath)
        {
            events.TestRunMessage += this.TestMessageHandler;
            events.TestRunStart += this.TestRunStartHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.outputFilePath = Path.GetFullPath(outputPath);

            lock (this.resultsGuard)
            {
                this.results = new List<TestResultInfo>();
            }

            this.localStartTime = DateTime.UtcNow;
        }

        /*

    <?xml version="1.0" encoding="UTF-8"?>
    <testsuites id="20140612_170519" name="New_configuration (14/06/12 17:05:19)" tests="225" failures="1262" time="0.001">
        <testsuite name="rspec" tests="2" skipped="0" failures="0" errors="0" time="0.001691" timestamp="2018-07-30T10:02:37+00:00" hostname="runner-7661726c-project-14-concurrent-0">
            <properties>
              <property name="seed" value="8528"/>
            </properties>
            <testcase classname="spec.string_helper_spec" name="StringHelper#concatenate when a is git and b is lab returns summary" file="./spec/string_helper_spec.rb" time="0.000287"></testcase>
            <testcase time="0.000102" name="Test#subtract3 fails" file="./spec/test_spec.rb" classname="spec.test_spec">
                <failure type="RSpec::Expectations::ExpectationNotMetError" message="expected: falsey value got: true">Failure/Error: expect(true).to be_falsy expected: falsey value got: true ./spec/test_spec.rb:66:in `block (3 levels) in <top (required)>'</failure>
            </testcase>
        </testsuite>
     </testsuites>
         */

        private XElement CreateTestSuitesElement(List<TestResultInfo> results)
        {
            File.WriteAllText($@"C:\test\testoutput.json", JsonConvert.SerializeObject(results));

            // <testsuites id="20140612_170519" name="New_configuration (14/06/12 17:05:19)" tests="225" failures="1262" time="0.001">
            var assemblies = results.Select(x => x.AssemblyPath).Distinct().ToList();
            var testsuiteElements = assemblies
                .Select(a => this.CreateTestSuiteElement(results.Where(x => x.AssemblyPath == a).ToList()));

            var element = new XElement("testsuites", testsuiteElements);

            element.SetAttributeValue("name", "todo add name");

            element.SetAttributeValue("tests", results.Count);
            element.SetAttributeValue("failures", results.Where(x => x.Outcome == TestOutcome.Failed).Count());
            element.SetAttributeValue("time", results.Sum(x => x.Duration.TotalSeconds));

            return element;
        }

        private XElement CreateTestSuiteElement(List<TestResultInfo> results)
        {
            // <testsuite name="rspec" tests="2" skipped="0" failures="0" errors="0" time="0.001691" timestamp="2018-07-30T10:02:37+00:00" hostname="runner-7661726c-project-14-concurrent-0">
            var testCaseElements = results.Select(a => this.CreateTestCaseElement(a));

            var element = new XElement("testsuite", testCaseElements);

            element.SetAttributeValue("name", Path.GetFileName(results.First().AssemblyPath));

            element.SetAttributeValue("tests", results.Count);
            element.SetAttributeValue("skipped", results.Where(x => x.Outcome == TestOutcome.Skipped).Count());
            element.SetAttributeValue("failures", results.Where(x => x.Outcome == TestOutcome.Failed).Count());
            element.SetAttributeValue("errors", 0); // looks like this isn't supported by .net?
            element.SetAttributeValue("time", results.Sum(x => x.Duration.TotalSeconds));
            element.SetAttributeValue("timestamp", this.localStartTime.ToString(DateFormat, CultureInfo.InvariantCulture));
            element.SetAttributeValue("hostname", results.First().TestCase.ExecutorUri);

            return element;
        }

        private XElement CreateTestCaseElement(TestResultInfo result)
        {
            // <testcase classname="spec.string_helper_spec" name="StringHelper#concatenate when a is git and b is lab returns summary" file="./spec/string_helper_spec.rb" time="0.000287"></testcase>
            // <testcase time="0.000102" name="Test#subtract3 fails" file="./spec/test_spec.rb" classname="spec.test_spec">
            //       <failure type="RSpec::Expectations::ExpectationNotMetError" message="expected: falsey value got: true">Failure/Error: expect(true).to be_falsy expected: falsey value got: true ./spec/test_spec.rb:66:in `block (3 levels) in <top (required)>'</failure>
            // </testcase>
            var testcaseElement = new XElement("testcase");

            testcaseElement.SetAttributeValue("classname", result.TestCase
                .FullyQualifiedName
                .Substring(0, result.TestCase.FullyQualifiedName.IndexOf(result.TestCase.DisplayName) - 1));
            testcaseElement.SetAttributeValue("name", result.Name);
            testcaseElement.SetAttributeValue("file", result.TestCase.Source);
            testcaseElement.SetAttributeValue("time", result.Duration.TotalSeconds);

            if (result.Outcome == TestOutcome.Failed)
            {
                var failureElement = new XElement("failure", result.ErrorStackTrace);

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
