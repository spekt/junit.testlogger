// Copyright (c) Spekt Contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extension.JUnit.Xml.TestLogger
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class TestCaseNameParser
    {
        public const string TestCaseParserUnknownNamsepace = "UnknownNamespace";
        public const string TestCaseParserUnknownType = "UnknownType";
        public const string TestCaseParserErrorTemplate = JUnitXmlTestLogger.FriendlyName + "Xml Logger: Unable to parse the test name '{0}' into a namespace type and method. " +
            "Using Namespace='{1}', Type='{2}' and Method='{3}'";

        private enum NameParseStep
        {
            FindMethod,
            FindType,
            FindNamespace,
            Done
        }

        private enum NameParseState
        {
            Default,
            Parenthesis,
            String
        }

        /// <summary>
        /// This method attempts to parse out a Namespace, Type and Method name from a given string. When a clearly
        /// invalid output is encountered, a message is written to the console.
        /// </summary>
        /// <remarks>
        /// This is fragile, because the fully qualified name is constructed by a test adapter and there is
        /// no enforcement that the FQN starts with the namespace, or is of the expected format.
        /// Because the possible input space is very large and this parser is relativly simple
        /// there are some invalid strings, such as "#.#.#" will 'successfully' parse.
        /// </remarks>
        /// <param name="fullyQualifedName">
        /// String like 'namespace.type.method', where type and or method may be followed by parenthesis containing
        /// parameter values.
        /// </param>
        /// <returns>
        /// An instance of ParsedName containing the parsed results. A result is always returned, even in the case when
        /// the input could not be full parsed.
        /// </returns>
        public static ParsedName Parse(string fullyQualifedName)
        {
            var metadataNamespaceName = string.Empty;
            var metadataTypeName = string.Empty;
            var metadataMethodName = string.Empty;

            var step = NameParseStep.FindMethod;
            var state = NameParseState.Default;
            var parenthesisCount = 0;

            var output = new List<char>();

            try
            {
                for (int i = fullyQualifedName.Length - 1; i >= 0; i--)
                {
                    var thisChar = fullyQualifedName[i];
                    if (step == NameParseStep.FindNamespace)
                    {
                        // When we are doing namespace, we always accumulate the char.
                        output.Insert(0, thisChar);
                    }
                    else if (state == NameParseState.Default)
                    {
                        if (thisChar == '(')
                        {
                            parenthesisCount--;
                        }

                        if (thisChar == '"' || thisChar == '\\')
                        {
                            throw new Exception("Found invalid characters");
                        }
                        else if (thisChar == ')')
                        {
                            if ((output.Count > 0) && (parenthesisCount == 0))
                            {
                                throw new Exception("The closing parenthesis we detected wouldn't be the last character in the output string. This isn't acceptable because we aren't in a string");
                            }

                            state = NameParseState.Parenthesis;
                            output.Insert(0, thisChar);
                        }
                        else if (thisChar == '.')
                        {
                            // Found the end of this element.
                            if (step == NameParseStep.FindMethod)
                            {
                                if (output.Count == 0)
                                {
                                    throw new Exception("This shouldn't be an empty string");
                                }

                                metadataMethodName = string.Join(string.Empty, output);

                                // Prep the next step
                                output = new List<char>();
                                step = NameParseStep.FindType;
                            }
                            else if (step == NameParseStep.FindType)
                            {
                                if (output.Count == 0)
                                {
                                    throw new Exception("This shouldn't be an empty string");
                                }

                                metadataTypeName = string.Join(string.Empty, output);

                                // Get The Namespace Next
                                step = NameParseStep.FindNamespace;
                                output = new List<char>();
                            }
                        }
                        else
                        {
                            // Part of the name to add
                            output.Insert(0, thisChar);
                        }
                    }
                    else if (state == NameParseState.Parenthesis)
                    {
                        if (thisChar == ')')
                        {
                            parenthesisCount++;
                        }

                        if (thisChar == '(')
                        {
                            // If we found the beginning of the parenthesis block, we are back in default state
                            state = NameParseState.Default;
                        }
                        else if (thisChar == '"')
                        {
                            // This must come at the end of a string, when escape characters aren't an issue, so we are
                            // 'entering' string state, because of the reverse parsing.
                            state = NameParseState.String;
                        }

                        output.Insert(0, thisChar);
                    }

                    // state == NameParseState.String
                    else
                    {
                        if (thisChar == '"' && fullyQualifedName.ElementAtOrDefault(i - 1) != '\\')
                        {
                            // If this is a quote that has not been escaped, switch the state. If it had
                            // been escaped, we would still be in a string.
                            state = NameParseState.Parenthesis;
                        }

                        output.Insert(0, thisChar);
                    }
                }

                if (parenthesisCount != 0)
                {
                    throw new Exception($"Unbalanced count of parentheses found ({parenthesisCount})");
                }

                // We are done. If we are finding type, set that variable.
                // Otherwise, ther was some issue, so leave the type blank.
                if (step == NameParseStep.FindNamespace)
                {
                    metadataNamespaceName = string.Join(string.Empty, output);
                }
                else if (step == NameParseStep.FindType)
                {
                    metadataTypeName = string.Join(string.Empty, output);
                }
            }
            catch (Exception)
            {
                // On exception, wipe out the type name
                metadataTypeName = string.Empty;
                metadataNamespaceName = string.Empty;
            }
            finally
            {
                // If for any reason we don't have a Type Name or Namespace then
                // we fall back on our safe option and notify the user
                if (string.IsNullOrWhiteSpace(metadataNamespaceName) && string.IsNullOrWhiteSpace(metadataTypeName))
                {
                    metadataNamespaceName = TestCaseParserUnknownNamsepace;
                    metadataTypeName = TestCaseParserUnknownType;
                    metadataMethodName = fullyQualifedName;
                    Console.WriteLine(string.Format(TestCaseParserErrorTemplate, fullyQualifedName, metadataNamespaceName, metadataTypeName, metadataMethodName));
                }
                else if (string.IsNullOrWhiteSpace(metadataNamespaceName))
                {
                    metadataNamespaceName = TestCaseParserUnknownNamsepace;
                    Console.WriteLine(string.Format(TestCaseParserErrorTemplate, fullyQualifedName, metadataNamespaceName, metadataTypeName, metadataMethodName));
                }
            }

            return new ParsedName(metadataNamespaceName, metadataTypeName, metadataMethodName);
        }

        public class ParsedName
        {
            public ParsedName(string namespaceName, string typeName, string methodName)
            {
                this.NamespaceName = namespaceName;
                this.TypeName = typeName;
                this.MethodName = methodName;
            }

            public string NamespaceName { get; }

            public string TypeName { get; }

            public string MethodName { get; }
        }
    }
}