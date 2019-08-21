// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JUnit.Xml.TestLogger.AcceptanceTests
{
    using System;
    using System.Diagnostics;
    using System.IO;

    public class DotnetTestFixture
    {
        public static string RootDirectory { get; set; } = Path.GetFullPath(
                    Path.Combine(
                        Environment.CurrentDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "assets",
                        "JUnit.Xml.TestLogger.NetCore.Tests"));

        public static string TestAssemblyName { get; set; } = "JUnit.Xml.TestLogger.NetCore.Tests.dll";

        public static string TestAssembly
        {
            get
            {
#if DEBUG
                var config = "Debug";
#else
                var config = "Release";
#endif
                return Path.Combine(RootDirectory, "bin", config, "netcoreapp2.0", TestAssemblyName);
            }
        }

        public static void Execute(string resultsFile)
        {
            var testProject = RootDirectory;
            var testLogger = $"--logger:\"junit;LogFilePath={resultsFile}\"";

            // Delete stale results file
            var testLogFile = Path.Combine(testProject, resultsFile);

            // Strip out tokens
            var sanitizedResultFile = System.Text.RegularExpressions.Regex.Replace(resultsFile, @"{.*}\.*", string.Empty);
            foreach (string fileName in Directory.GetFiles(testProject))
            {
                if (fileName.Contains("test-results.xml"))
                {
                    File.Delete(fileName);
                }
            }

            // Log the contents of test output directory. Useful to verify if the logger is copied
            Console.WriteLine("------------");
            Console.WriteLine("Contents of test output directory:");
            foreach (var f in Directory.GetFiles(Path.Combine(testProject, "bin/Debug/netcoreapp2.0")))
            {
                Console.WriteLine("  " + f);
            }

            Console.WriteLine();

            // Run dotnet test with logger
            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "dotnet";
                p.StartInfo.Arguments = $"test --no-build {testLogger} {testProject}";
                p.Start();

                Console.WriteLine("dotnet arguments: " + p.StartInfo.Arguments);

                // To avoid deadlocks, always read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                Console.WriteLine("dotnet output: " + output);
                Console.WriteLine("------------");
            }
        }

        public static void Execute(string resultsFileName, string filePath)
        {
            var testProject = RootDirectory;
            var testLogger = $"--logger \"junit;LogFileName={resultsFileName}\" --results-directory \"{filePath}\"";

            // Log the contents of test output directory. Useful to verify if the logger is copied
            Console.WriteLine("------------");
            Console.WriteLine("Contents of test output directory:");
            foreach (var f in Directory.GetFiles(Path.Combine(testProject, "bin/Debug/netcoreapp2.0")))
            {
                Console.WriteLine("  " + f);
            }

            Console.WriteLine();

            // Run dotnet test with logger
            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "dotnet";
                p.StartInfo.Arguments = $"test --no-build {testLogger} {testProject}";
                p.Start();

                Console.WriteLine("dotnet arguments: " + p.StartInfo.Arguments);

                // To avoid deadlocks, always read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                Console.WriteLine("dotnet output: " + output);
                Console.WriteLine("------------");
            }
        }
    }
}
