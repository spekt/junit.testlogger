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

            var tuptuo = new List<char>();

            try
            {
                var eman = fullyQualifedName.ToCharArray().Reverse().ToArray();

                for (int i = 0; i < eman.Count(); i++)
                {
                    var thisChar = eman[i];
                    if (step == NameParseStep.FindNamespace)
                    {
                        // When we are doing namespace, we always accumulate the char.
                        tuptuo.Add(thisChar);
                    }
                    else if (state == NameParseState.Default)
                    {
                        if (thisChar == '(' || thisChar == '"' || thisChar == '\\')
                        {
                            throw new Exception("Found invalid characters");
                        }
                        else if (thisChar == ')')
                        {
                            if (tuptuo.Count > 0)
                            {
                                throw new Exception("The closing parenthesis we detected wouldn't be the last character in the output string. This isn't acceptable because we aren't in a string");
                            }

                            state = NameParseState.Parenthesis;
                            tuptuo.Add(thisChar);
                        }
                        else if (thisChar == '.')
                        {
                            // Found the end of this element.
                            if (step == NameParseStep.FindMethod)
                            {
                                if (tuptuo.Count == 0)
                                {
                                    throw new Exception("This shouldn't be an empty string");
                                }

                                tuptuo.Reverse();
                                metadataMethodName = string.Join(string.Empty, tuptuo);

                                // Prep the next step
                                tuptuo = new List<char>();
                                step = NameParseStep.FindType;
                            }
                            else if (step == NameParseStep.FindType)
                            {
                                if (tuptuo.Count == 0)
                                {
                                    throw new Exception("This shouldn't be an empty string");
                                }

                                tuptuo.Reverse();
                                metadataTypeName = string.Join(string.Empty, tuptuo);

                                // Get The Namespace Next
                                step = NameParseStep.FindNamespace;
                                tuptuo = new List<char>();
                            }
                        }
                        else
                        {
                            // Part of the name to add
                            tuptuo.Add(thisChar);
                        }
                    }
                    else if (state == NameParseState.Parenthesis)
                    {
                        if (thisChar == ')' || thisChar == '\\')
                        {
                            throw new Exception("Found invalid characters");
                        }
                        else if (thisChar == '(')
                        {
                            // If we found the beginning of the parenthesis block, we are back in default state
                            state = NameParseState.Default;
                            tuptuo.Add(thisChar);
                        }
                        else if (thisChar == '"')
                        {
                            // This must come at the end of a string, when escape characters aren't an issue, so we are
                            // 'entering' string state, because of the reverse parsing.
                            state = NameParseState.String;
                            tuptuo.Add(thisChar);
                        }
                        else
                        {
                            tuptuo.Add(thisChar);
                        }
                    }
                    else
                    {
                        // We are in String State.
                        if (thisChar == '"')
                        {
                            if (eman.ElementAtOrDefault(i + 1) == '\\')
                            {
                                // The quote was escaped, so its atually a quote mark in a string
                                tuptuo.Add(thisChar);
                            }
                            else
                            {
                                state = NameParseState.Parenthesis;
                                tuptuo.Add(thisChar);
                            }
                        }
                        else
                        {
                            tuptuo.Add(thisChar);
                        }
                    }
                }

                // We are done. If we are finding type, set that variable.
                // Otherwise, ther was some issue, so leave the type blank.
                if (step == NameParseStep.FindNamespace)
                {
                    tuptuo.Reverse();
                    metadataNamespaceName = string.Join(string.Empty, tuptuo);
                }
                else if (step == NameParseStep.FindType)
                {
                    tuptuo.Reverse();
                    metadataTypeName = string.Join(string.Empty, tuptuo);
                }
            }
            catch (Exception)
            {
                // On exception, whipe out the type name
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
