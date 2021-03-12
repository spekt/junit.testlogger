// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extension.Junit.Xml.TestLogger
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
    using Spekt.TestLogger.Core;
    using Spekt.TestLogger.Utilities;

    public class JunitXmlSerializer : ITestResultSerializer
    {
        // Dicionary keys for command line arguments.
        public const string TargetFrameworkKey = "TargetFramework";
        public const string LogFilePathKey = "LogFilePath";
        public const string LogFileNameKey = "LogFileName";
        public const string ResultDirectoryKey = "TestRunDirectory";
        public const string MethodFormatKey = "MethodFormat";
        public const string FailureBodyFormatKey = "FailureBodyFormat";
        public const string FileEncodingKey = "FileEncoding";

        private const string ResultStatusPassed = "Passed";
        private const string ResultStatusFailed = "Failed";

        // Tokens to allow user to manipulate output file or directory names.
        private const string AssemblyToken = "{assembly}";
        private const string FrameworkToken = "{framework}";

        private readonly object resultsGuard = new object();
        private string outputFilePath;

        /// <summary>
        /// Recieves information messages, along with StdOut from test methods.
        /// </summary>
        private StringBuilder stdOut = new StringBuilder();

        /// <summary>
        /// Recieves both warning and error messages.
        /// </summary>
        private StringBuilder stdErr = new StringBuilder();

        private List<TestResultInfo> results;
        private DateTime utcStartTime;

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

        public enum FileEncoding
        {
            /// <summary>
            /// UTF8
            /// </summary>
            UTF8,

            /// <summary>
            /// UTF8 Bom
            /// </summary>
            UTF8Bom
        }

        public MethodFormat MethodFormatOption { get; private set; } = MethodFormat.Default;

        public FailureBodyFormat FailureBodyFormatOption { get; private set; } = FailureBodyFormat.Default;

        public FileEncoding FileEncodingOption { get; private set; } = FileEncoding.UTF8;

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

        public string Serialize(LoggerConfiguration loggerConfiguration, TestRunConfiguration runConfiguration, List<TestResultInfo> results)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initialized called by dotnet test.
        /// </summary>
        /// <param name="events">Test logger event.</param>
        /// <param name="testResultsDirPath">
        /// A single string is assumed to be the test result directory argument.
        /// </param>
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

        /// <summary>
        /// Initialized called by dotnet test.
        /// </summary>
        /// <param name="events">Test logger event.</param>
        /// <param name="parameters">
        /// Dictionary of key value pairs provided by the user, semicolon delimited (i.e. 'key1=val1;key2=val2').
        /// </param>
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

            // Assist users with message when they entered invalid CLI options
            var knownKeys = new List<string>() { TargetFrameworkKey, ResultDirectoryKey, LogFilePathKey, LogFileNameKey, MethodFormatKey, FailureBodyFormatKey };
            parameters.Where(x => knownKeys.Contains(x.Key) == false).ToList()
                .ForEach(x => Console.WriteLine($"JunitXML Logger: The provided configuration item '{x.Key}' is not valid and will be ignored. Note, names are case sensitive."));

            if (parameters.TryGetValue(LogFileNameKey, out string outputPathName) && parameters.TryGetValue(ResultDirectoryKey, out string outputFileDirectory))
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
                throw new ArgumentException($"JunitXML Logger: Expected {LogFilePathKey} or {DefaultLoggerParameterNames.TestRunDirectory} parameter", nameof(parameters));
            }

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

            if (parameters.TryGetValue(FileEncodingKey, out string fileEncoding))
            {
                if (string.Equals(fileEncoding.Trim(), "UTF8Bom", StringComparison.OrdinalIgnoreCase))
                {
                    this.FileEncodingOption = FileEncoding.UTF8Bom;
                }
                else if (string.Equals(fileEncoding.Trim(), "UTF8", StringComparison.OrdinalIgnoreCase))
                {
                    this.FileEncodingOption = FileEncoding.UTF8;
                }
                else
                {
                    Console.WriteLine($"JunitXML Logger: The provided File Encoding '{fileEncoding}' is not a recognized option. Using default");
                }
            }
        }

        /// <summary>
        /// Called when a test message is received. These messages are coming from the test
        /// framework, and don't contain standard output produced by test code.
        /// </summary>
        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            if (e.Level == TestMessageLevel.Informational)
            {
                this.stdOut.AppendLine(e.Message);
            }
            else
            {
                this.stdErr.AppendLine(e.Message);
            }
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

            if (e.Result.Messages.Count > 0)
            {
                this.stdOut.AppendLine();
                this.stdOut.AppendLine(result.TestCase.FullyQualifiedName);
                this.stdOut.AppendLine(Indent(result.Messages));
            }

            var parsedName = TestCaseNameParser.Parse(result.TestCase.FullyQualifiedName);
            lock (this.resultsGuard)
            {
                this.results.Add(new TestResultInfo(
                    result,
                    parsedName.NamespaceName,
                    parsedName.TypeName,
                    parsedName.MethodName));
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
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

                var settings = new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(this.FileEncodingOption == FileEncoding.UTF8Bom),
                    Indent = true,
                };

                using (var f = File.Create(this.outputFilePath))
                {
                    using (var w = XmlWriter.Create(f, settings))
                    {
                        doc.Save(w);
                    }
                }

                var resultsFileMessage = string.Format(CultureInfo.CurrentCulture, "JunitXML Logger - Results File: {0}", this.outputFilePath);
                Console.WriteLine(Environment.NewLine + resultsFileMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("JunitXML Logger: Threw an unhandeled exception. ");
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

        /// <summary>
        /// Produces a consistently indented output, taking into account that incoming messages
        /// often have new lines within a message.
        /// </summary>
        private static string Indent(IReadOnlyCollection<TestResultMessage> messages)
        {
            var indent = "    ";

            // Splitting on any line feed or carrage return because a message may include new lines
            // that are inconsistent with the Environment.NewLine. We then remove all blank lines so
            // it shouldn't cause an issue that this generates extra line breaks.
            return
                indent +
                string.Join(
                    $"{Environment.NewLine}{indent}",
                    messages.SelectMany(m =>
                        m.Text.Split(new string[] { "\r", "\n" }, StringSplitOptions.None)
                              .Where(x => !string.IsNullOrWhiteSpace(x))
                              .Select(x => x.Trim())));
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

            this.utcStartTime = DateTime.UtcNow;
        }

        private XElement CreateTestSuitesElement(List<TestResultInfo> results)
        {
            var assemblies = results.Select(x => x.AssemblyPath).Distinct().ToList();
            var testsuiteElements = assemblies
                .Select(a => this.CreateTestSuiteElement(results.Where(x => x.AssemblyPath == a).ToList()));

            var element = new XElement("testsuites", testsuiteElements);

            return element;
        }

        private XElement CreateTestSuiteElement(List<TestResultInfo> results)
        {
            var testCaseElements = results.Select(a => this.CreateTestCaseElement(a));

            // Adding required properties, system-out, and system-err elements in the correct
            // positions as required by the xsd. In system-out collapse consequtive newlines to a
            // single newline.
            var element = new XElement(
                "testsuite",
                new XElement("properties"),
                testCaseElements,
                new XElement("system-out", this.stdOut.ToString()),
                new XElement("system-err", this.stdErr.ToString()));

            element.SetAttributeValue("name", Path.GetFileName(results.First().AssemblyPath));

            element.SetAttributeValue("tests", results.Count);
            element.SetAttributeValue("skipped", results.Where(x => x.Outcome == TestOutcome.Skipped).Count());
            element.SetAttributeValue("failures", results.Where(x => x.Outcome == TestOutcome.Failed).Count());
            element.SetAttributeValue("errors", 0); // looks like this isn't supported by .net?
            element.SetAttributeValue("time", results.Sum(x => x.Duration.TotalSeconds));
            element.SetAttributeValue("timestamp", this.utcStartTime.ToString("s"));
            element.SetAttributeValue("hostname", Environment.MachineName);
            element.SetAttributeValue("id", 0); // we never output multiple, so this is always zero.
            element.SetAttributeValue("package", Path.GetFileName(results.First().AssemblyPath));

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

            // Ensure time value is never zero because gitlab treats 0 like its null. 0.1 micro
            // seconds should be low enough it won't interfere with anyone monitoring test duration.
            testcaseElement.SetAttributeValue(
                "time",
                Math.Max(0.0000001f, result.Duration.TotalSeconds).ToString("0.0000000"));

            if (result.Outcome == TestOutcome.Failed)
            {
                var failureBodySB = new StringBuilder();

                if (this.FailureBodyFormatOption == FailureBodyFormat.Verbose)
                {
                    failureBodySB.AppendLine(result.ErrorMessage);

                    // Stack trace label included to mimic the normal test output
                    failureBodySB.AppendLine("Stack Trace:");
                }

                failureBodySB.AppendLine(result.ErrorStackTrace);

                if (this.FailureBodyFormatOption == FailureBodyFormat.Verbose &&
                    result.Messages.Count > 0)
                {
                    failureBodySB.AppendLine("Standard Output:");

                    failureBodySB.AppendLine(Indent(result.Messages));
                }

                var failureElement = new XElement("failure", failureBodySB.ToString().Trim());

                failureElement.SetAttributeValue("type", "failure"); // TODO are there failure types?
                failureElement.SetAttributeValue("message", result.ErrorMessage);

                testcaseElement.Add(failureElement);
            }
            else if (result.Outcome == TestOutcome.Skipped)
            {
                var skippedElement = new XElement("skipped");

                testcaseElement.Add(skippedElement);
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