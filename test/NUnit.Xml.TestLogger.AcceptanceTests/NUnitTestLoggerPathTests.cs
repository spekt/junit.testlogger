// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NUnitTestLoggerPathTests
    {
        public NUnitTestLoggerPathTests()
        {
        }

        [ClassInitialize]
        public static void SuiteInitialize(TestContext context)
        {
            DotnetTestFixture.RootDirectory = Path.GetFullPath(
                Path.Combine(
                    Environment.CurrentDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "assets",
                    "NUnit.Xml.TestLogger.NetMulti.Tests"));
            DotnetTestFixture.TestAssemblyName = "NUnit.Xml.TestLogger.NetMulti.Tests.dll";
            DotnetTestFixture.Execute("{assembly}.{framework}.test-results.xml");
        }

        [TestMethod]
        public void TestRunWithLoggerAndFilePathShouldCreateResultsFile()
        {
            string[] expectedResultsFiles = new string[]
            {
                Path.Combine(DotnetTestFixture.RootDirectory, "NUnit.Xml.TestLogger.NetMulti.Tests.NETFramework46.test-results.xml"),
                Path.Combine(DotnetTestFixture.RootDirectory, "NUnit.Xml.TestLogger.NetMulti.Tests.NETCoreApp20.test-results.xml")
            };
            foreach (string resultsFile in expectedResultsFiles)
            {
                Assert.IsTrue(File.Exists(resultsFile), $"{resultsFile} does not exist.");
            }
        }
    }
}
