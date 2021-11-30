// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
    public class CoberturaParserTests
    {
        // Simple XPath-based parser for comparison
        // Returns unresolved file names and list of source directories.
        internal static (CoverageResult result, List<string> sourceDirectories) SimpleCoberturaParser(string reportFilePath)
        {
            DateTime reportTimestamp = File.GetLastWriteTimeUtc(reportFilePath);
            XPathDocument document = new(reportFilePath);
            Regex regexCoverage = new(@"\((\d+)/(\d+)\)", RegexOptions.CultureInvariant);

            CoverageResult result = new(reportFilePath);
            List<string> sourceDirectories = new();

            XPathNavigator navi = document.CreateNavigator();
            XPathNavigator? node = navi.SelectSingleNode("/coverage");
            if (node == null || node.Select("@*").Count < 2
                || node.GetAttribute("profilerVersion", "").Length != 0
                || node.GetAttribute("clover", "").Length != 0
                || node.GetAttribute("generated", "").Length != 0)
            {
                ThrowXmlException(node, "This is not a valid Cobertura report.");
            }

            XPathNodeIterator sources = navi.Select("/coverage/sources/source");
            foreach (XPathNavigator source in sources)
            {
                if (source.Value.Length > 0)
                {
                    sourceDirectories.Add(source.Value);
                }
            }

            string xpathClasses = "/coverage/packages/package/classes/class";
            if (navi.Select(xpathClasses).Count == 0)
            {
                xpathClasses = "/coverage/packages/class";
                // Intentionally not checking Count again (allow empty)
            }
            foreach (XPathNavigator @class in navi.Select(xpathClasses))
            {
                string fileName = @class.GetAttribute("filename", "");
                if (fileName.Length == 0)
                {
                    ThrowXmlException(@class, "Invalid or missing attribute 'filename'.");
                }
                SourceFileInfo sourceFileInfo = new(fileName, reportTimestamp);

                foreach (XPathNavigator line in @class.Select("lines/line"))
                {
                    LineInfo lineInfo = default;
                    if (!int.TryParse(line.GetAttribute("number", ""), out int lineNumber) || lineNumber < 1)
                    {
                        ThrowXmlException(line, "Invalid or missing attribute 'number'.");
                    }
                    string hitsAttr = line.GetAttribute("hits", "");
                    long hits = 0;
                    if (hitsAttr.Length != 0 && (!long.TryParse(hitsAttr, out hits) || hits < 0))
                    {
                        ThrowXmlException(line, "Invalid attribute 'hits'.");
                    }
                    lineInfo.Hits = (int)Math.Min(hits, int.MaxValue);
                    string coverage = line.GetAttribute("condition-coverage", "");
                    if (coverage.Length != 0)
                    {
                        Match m = regexCoverage.Match(coverage);
                        if (m.Success)
                        {
                            lineInfo.CoveredBranches = ushort.Parse(m.Groups[1].Value);
                            lineInfo.TotalBranches = ushort.Parse(m.Groups[2].Value);
                        }
                    }
                    sourceFileInfo.AddOrMergeLine(lineNumber, lineInfo);
                }

                result.AddOrMergeSourceFile(sourceFileInfo);
            }

            return (result, sourceDirectories);

            [DoesNotReturn]
            static void ThrowXmlException(XPathNavigator? node = null, string? message = null)
            {
                int lineNumber = 0, linePosition = 0;
                if (node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
                {
                    lineNumber = lineInfo.LineNumber;
                    linePosition = lineInfo.LinePosition;
                }
                throw new XmlException(message, null, lineNumber, linePosition);
            }
        }

        [Theory]
        [InlineData("coverlet small.xml")]
        [InlineData("coverlet big.xml")]
        [InlineData("fcc merged.xml")]
        public void Parse_WithTestFiles_WithoutFileResolver_Succeeds(string reportName)
        {
            string reportFilePath = Path.Combine(ProgramTests.GetTestDataDirectory(), reportName);
            (CoverageResult expected, List<string> sourceDirectories) = SimpleCoberturaParser(reportFilePath);
            // Supress file resolver
            CoverageResult result = CoberturaParser.Parse(reportFilePath, (f, d) => f);
            Assert.Equal(expected.SourceFiles.Count, result.SourceFiles.Count);
            foreach(SourceFileInfo expectedSource in expected.SourceFiles.Values)
            {
                SourceFileInfo resultSource = result.SourceFiles[expectedSource.SourceFilePath];
                Assert.Equal(expectedSource.LastLineNumber, resultSource.LastLineNumber);
                for (int i = 1; i <= expectedSource.LastLineNumber; i++)
                {
                    Assert.Equal(expectedSource.Line(i), resultSource.Line(i));
                }
            }
        }
    }
}
