// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
    public partial class CoberturaParserTests
    {
        // Mock FilePathResolver
        private static string LeaveFilePathAsIs(string fileName, IReadOnlyList<string> _) => fileName;

        // Assert XmlException has proper message and line number/position.
        private static void AssertXmlException(XmlException exception, string messageContains,
                                               CoberturaReportBuilder report)
        {
            Assert.NotEqual(0, exception.LineNumber);
            Assert.NotEqual(0, exception.LinePosition);
            Assert.NotNull(report.FirstInvalidElement);
            Assert.NotNull(report.ReportFilePath);
            Assert.Contains(messageContains, exception.Message, StringComparison.OrdinalIgnoreCase);
            string fileLine;
            using (StreamReader reader = new(report.ReportFilePath!))
            {
                for (int i = 1; i < exception.LineNumber && reader.ReadLine() != null; i++) { /* do nothing */ }
                fileLine = (reader.ReadLine() ?? string.Empty).TrimEnd();
            }
            int pos = exception.LinePosition - fileLine.Length + (fileLine = fileLine.TrimStart()).Length;
            string[] elementLines = report.FirstInvalidElement!
                .ToString()
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            string elementLine;
            int expectedPos;
            if (fileLine.StartsWith("</", StringComparison.Ordinal))
            {
                // COMPAT net31 doesn't have StringSplitOptions.TrimEntries
                elementLine = elementLines[^1].Trim();
                expectedPos = 3;
            }
            else
            {
                elementLine = elementLines[0].Trim();
                expectedPos = 2;
            }

            Assert.Equal(elementLine, fileLine);
            Assert.Equal(expectedPos, pos);
        }

        private static void AssertCoverageResult(CoberturaReportBuilder report, CoberturaParser parser,
                                                 CoverageResult actualResult)
        {
            Assert.Equal(report.NormalizedSourceDirectories, parser.SourceDirectories);
            AssertCoverageResult(report.CoverageResult, actualResult);
        }

        private static void AssertCoverageResult(CoverageResult expectedResult, CoverageResult actualResult)
        {
            // Note: Although Xunit.Assert.Equal() supports Dictionary<>, it doesn't honor the Dictionary's
            //       comparer, e.g. variations of case-insensitive keys cause the assert to fail.
            Assert.Equal(expectedResult.LatestOnly, actualResult.LatestOnly);
            Assert.Equal(expectedResult.ReportFilePaths.Count, actualResult.ReportFilePaths.Count);
            foreach (string path in expectedResult.ReportFilePaths)
            {
                Assert.Contains(path, actualResult.ReportFilePaths, StringComparer.OrdinalIgnoreCase);
            }
            Assert.Equal(expectedResult.SourceFiles.Count, actualResult.SourceFiles.Count);
            foreach (KeyValuePair<string, SourceFileInfo> kvp in expectedResult.SourceFiles)
            {
                SourceFileInfo other =
                    Assert.Contains(kvp.Key, (IDictionary<string, SourceFileInfo>)actualResult.SourceFiles);
                AssertEqualSourceFileInfo(kvp.Value, other);
            }

            static void AssertEqualSourceFileInfo(SourceFileInfo expected, SourceFileInfo actual)
            {
                Assert.Equal(expected.SourceFilePath, actual.SourceFilePath, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expected.ReportTimestamp, actual.ReportTimestamp);
                Assert.Equal(expected.LastLineNumber, actual.LastLineNumber);
                for (int i = 1; i <= expected.LastLineNumber; i++)
                {
                    ref readonly LineInfo leftLine = ref expected.Line(i);
                    ref readonly LineInfo rightLine = ref actual.Line(i);
                    Assert.Equal((i, leftLine.IsLine), (i, rightLine.IsLine));
                    if (leftLine.IsLine)
                    {
                        Assert.Equal((i, leftLine), (i, rightLine));
                    }
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void ctor_InvalidReportFilePath_Throws(string reportFilePath)
        {
            ArgumentException ex = Assert.ThrowsAny<ArgumentException>(
                () => _ = new CoverageResult(reportFilePath));
            Assert.Equal("reportFilePath", ex.ParamName);
        }

        [Fact]
        public void ctor_NonExistingReportFilePath_Throws()
        {
            string reportFilePath = Path.Combine(Path.GetTempPath(), "nonexisting.file");
            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(
                () => _ = new CoberturaParser(reportFilePath));
        }

        [Fact]
        public void ctor_ExistingReportFilePath_Succeeds()
        {
            using TempFile tempFile = new();
            using CoberturaParser parser = new(tempFile.FullPath);
            Assert.Equal(tempFile.FullPath, parser.ReportFilePath);
            Assert.Equal(File.GetLastWriteTimeUtc(tempFile.FullPath), parser.ReportTimestamp);
            Assert.Null(parser.FilePathResolver);
        }

        [Fact]
        public void FilePathResolver_Roundtrip()
        {
            using TempFile tempFile = new();
            using CoberturaParser parser = new(tempFile.FullPath);
            Assert.Null(parser.FilePathResolver);
            parser.FilePathResolver = LeaveFilePathAsIs;
            Assert.Equal(LeaveFilePathAsIs, parser.FilePathResolver);
            parser.FilePathResolver = null;
            Assert.Null(parser.FilePathResolver);
        }

        [Fact]
        public void Dispose_MultipleCalls_Succeeds()
        {
            using TempFile tempFile = new();
            CoberturaParser parser = new(tempFile.FullPath);
            Assert.NotEqual(ReadState.Closed, parser.ReadState);
            for (int i = 0; i < 2; i++)
            {
                parser.Dispose();
                Assert.Equal(ReadState.Closed, parser.ReadState);
                ((IDisposable)parser).Dispose();
                Assert.Equal(ReadState.Closed, parser.ReadState);
            }
        }

        [Fact]
        public void Parse_MultipleCalls_Throws()
        {
            using TempFile tempFile = new();
            new CoberturaReportBuilder(tempFile.FullPath).AddMinimalDefaults().Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            Assert.NotEqual(ReadState.Closed, parser.ReadState);
            _ = parser.Parse();
            Assert.Equal(ReadState.Closed, parser.ReadState);
            ObjectDisposedException ex = Assert.Throws<ObjectDisposedException>(() => _ = parser.Parse());
            Assert.Equal(nameof(CoberturaParser), ex.ObjectName);
        }

        [Fact]
        public void Parse_InvalidXml_Throws()
        {
            using TempFile tempFile = new();
            tempFile.WriteAllText(@"<coverage version=""1.9"" timestamp=""1628548975"">
  <sources />
  <packages>
    </package>
  </packages>
</coverage>
");
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            Assert.Equal(4, ex.LineNumber);
            Assert.Equal(7, ex.LinePosition);
        }

        [Theory]
        [InlineData("something", null, null, null)]
        [InlineData("something", "http:/foo/bar", "http:/buzz/bazz", "internal_foo")]
        [InlineData("something", null, "http:/buzz/bazz", null)]
        [InlineData("coverage", null, "http://cobertura.sourceforge.net/xml/coverage-04.dtd", null)]
        public void Parse_IgnoresDTD(string name, string? publicId, string? systemId, string? internalSubset)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new(tempFile.FullPath)
            {
                XDocumentType = new XDocumentType(name, publicId, systemId, internalSubset!)
            };
            report.AddMinimalDefaults().Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Theory]
        [InlineData("something")]
        [InlineData("coverage")]
        [InlineData("coverage", "version", "1.2.3")]
        [InlineData("coverage", "generated", "1628548975", "clover", "1.2.3")]
        [InlineData("coverage", "generated", "1628548975", "attr2", "filler")]
        [InlineData("coverage", "clover", "1.2.3", "attr2", "filler")]
        [InlineData("coverage", "profilerVersion", "1.2.3", "driverVersion", "1.2.3")]
        public void Parse_NonCoberturaRoot_Throws(params string[] rootParams)
        {
            List<(string, string)> attributes = new();
            for (int i = 1; i + 1 < rootParams.Length; i += 2)
            {
                attributes.Add((rootParams[i], rootParams[i + 1]));
            }
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddRoot(rootParams[0], attributes.ToArray())
                .AddMinimalDefaults()
                .Save();
            report.FirstInvalidElement = report.Root; // Builder doesn't validate custom root.
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "not a valid Cobertura report", report);
        }

        [Fact]
        public void Parse_CoberturaRoot_Succeeds()
        {
            using TempFile tempFile = new();
            new CoberturaReportBuilder(tempFile.FullPath).AddRoot().Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            Assert.Equal(tempFile.FullPath, Assert.Single(result.ReportFilePaths));
            Assert.Empty(result.SourceFiles);
            Assert.False(result.LatestOnly, "LatestOnly");
        }

        [Fact]
        public void Parse_EmptySources_Succeeds()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddMinimalDefaults()
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            Assert.Empty(parser.SourceDirectories);
            AssertCoverageResult(report, parser, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("  \n  ")]
        public void Parse_InvalidSource_Throws(string sourceDirectory)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources((sourceDirectory, false))
                .AddMinimalDefaults()
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Invalid element 'source'", report);
        }

        [Fact]
        public void Parse_NormalizesSources()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources(
                    (@"c:\", true),
                    ("c:", true),
                    (@"c:\somedir", true),
                    (@"c:\somedir\", true),
                    ("/_/somedir", false),
                    ("/_3/somedir", false),
                    ("somedir", false),
                    ("http://foo.com/bar", false),
                    ("https://foo.com/bar", false))
                .AddMinimalDefaults()
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Parse_ClassParents_MayVary(bool classBelowPackages)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new(tempFile.FullPath)
            {
                ClassBelowPackages = classBelowPackages
            };
            report.AddMinimalDefaults().Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Fact]
        public void Parse_MultiplePackages_Succeeds()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath).AddSources();
            int fileId = 0;
            for (int i = 0; i < 3; i++)
            {
                report.AddPackage();
                for (int j = 0; j < 5; j++)
                {
                    report.AddClass($"file{fileId++}");
                    for (int k = 1; k <= 10; k++)
                    {
                        report.AddLine(k, 10);
                    }
                }
            }
            report.Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Fact]
        public void Parse_MissingFileName_Throws()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass(null)
                .AddMinimalDefaults()
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Invalid or missing attribute 'filename'", report);
        }

        [Fact]
        public void Parse_NormalizesFileNames()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass(@"dir1/file1.cs").AddLine(1, 0)
                .AddClass(@"dir1\file1.cs").AddLine(2, 0)
                .AddClass(@"c:\dir2/file2.cs").AddLine(1, 0)
                .AddClass(@"/_/dir3/file3.cs", normalize: false).AddLine(1, 0)
                .AddClass(@"/_3/file4.cs", normalize: false).AddLine(1, 0)
                .AddClass(@"http://foo.com/bar/file5.cs", normalize: false).AddLine(1, 0)
                .AddClass(@"https://foo.com/bar/file5.cs", normalize: false).AddLine(1, 0)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Fact]
        public void Parse_MergesNormalizedFileNames()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass(@"dir1/file1.cs").AddLine(1, 0)
                .AddClass(@"dir1\file1.cs").AddLine(2, 0)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            // Note: This is already part of the Parse_NormalizesFileNames() test,
            //       but singled out to emphasize the merging.
            Assert.Single(result.SourceFiles);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Parse_ResolvesFileNames(int numberOfSources)
        {
            using TempDirectory tempDirectory = new();
            string reportFilePath = Path.Combine(tempDirectory.FullPath, "coverage.xml");
            CoberturaReportBuilder report = new(reportFilePath);
            int fileId = 0;
            string fileName = Path.Combine(tempDirectory.FullPath, $"file{fileId++}.cs");
            string resolvedName = fileName;
            tempDirectory.WriteFile(resolvedName, "This is " + resolvedName);
            report.AddClass(fileName, resolvedName).AddLine(1, 0);
            for (int i = 0; i < numberOfSources; i++)
            {
                string sourcedir = $"sourcedir{i}";
                report.AddSources((Path.Combine(tempDirectory.FullPath, sourcedir), normalize: true));
                for (int k = 0; k < 2; k++)
                {
                    fileName = Path.Combine("project", $"file{fileId++}.cs");
                    resolvedName = Path.Combine(tempDirectory.FullPath, sourcedir, fileName);
                    tempDirectory.WriteFile(resolvedName, "This is " + resolvedName);
                    report.AddClass(fileName, resolvedName).AddLine(1, 0);
                }
            }
            report.Save();
            using CoberturaParser parser = new(reportFilePath);
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
        }

        [Fact]
        public void Parse_MissingLines_Throws()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Subtree 'lines' not found", report);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("one")]
        public void Parse_InvalidOrMissingLineNumber_Throws(string? number)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine(number, "0")
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Invalid or missing attribute 'number'", report);
        }

        [Fact]
        public void Parse_LineNumberLargerThanMaxLineNumber_Throws()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine(SourceFileInfo.MaxLineNumber + 1, 0)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Line attribute 'number' is larger than MaxLineNumber", report);
        }

        [Fact]
        public void Parse_ValidLineNumber_Succeeds()
        {
            int[] numbers = new[] { 1, 27, SourceFileInfo.MaxLineNumber, 2, 6 };
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1");
            for (int i = 0; i < numbers.Length; i++)
            {
                report.AddLine(numbers[i], 0);
            }
            report.Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            SourceFileInfo info = Assert.Single(result.SourceFiles).Value;
            Assert.Equal(SourceFileInfo.MaxLineNumber, info.LastLineNumber);
            for (int i = 1; i < info.LastLineNumber; i++)
            {
                Assert.Equal(Array.IndexOf(numbers, i) >= 0, info.Line(i).IsLine);
            }
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("one")]
        public void Parse_InvalidHits_Throws(string hits)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", hits)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Invalid attribute 'hits'", report);
        }

        [Fact]
        public void Parse_MissingHits_EqualsZero()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", hits: null)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            LineInfo line = result.SourceFiles["file1"].Line(1);
            Assert.True(line.IsLine);
            Assert.Equal(0, line.Hits);
        }

        [Theory]
        [InlineData((long)int.MaxValue + 1)]
        [InlineData((long)uint.MaxValue + 1)]
        public void Parse_VeryLargeHits_EqualsMaxIntValue(long hits)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", hits.ToString())
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            LineInfo line = result.SourceFiles["file1"].Line(1);
            Assert.True(line.IsLine);
            Assert.Equal(int.MaxValue, line.Hits);
        }

        [Fact]
        public void Parse_ValidHits_Succeeds()
        {
            int[] hits = new[] { 0, 1, 2, 32, 457 };
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1");
            for (int i = 0; i < hits.Length; i++)
            {
                report.AddLine(i + 1, hits[i]);
            }
            report.Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            SourceFileInfo info = Assert.Single(result.SourceFiles).Value;
            for (int i = 1; i <= hits.Length; i++)
            {
                Assert.True(info.Line(i).IsLine);
                Assert.Equal(hits[i - 1], info.Line(i).Hits);
            }
        }

        [Theory]
        [InlineData("75%")]
        [InlineData("75% (3;4)")]
        [InlineData("75% (3/4")]
        [InlineData("0% (-1/0)")]
        [InlineData("0% (0/-1)")]
        [InlineData("99% (65535/65536)")]
        [InlineData("100% (65536/65535)")]
        public void Parse_InvalidConditionCoverage_Throws(string condition)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", "10", condition: condition)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            XmlException ex = Assert.Throws<XmlException>(() => _ = parser.Parse());
            AssertXmlException(ex, "Invalid attribute 'condition-coverage'", report);
        }

        [Fact]
        public void Parse_MissingConditionCoverage_EqualsNoBranch()
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", "10", branch: true, condition: null)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            LineInfo line = result.SourceFiles["file1"].Line(1);
            Assert.True(line.IsLine);
            Assert.Equal(10, line.Hits);
            Assert.Equal(0, line.CoveredBranches);
            Assert.Equal(0, line.TotalBranches);
        }

        [Theory]
        [InlineData("100%", 0, 0)]
        [InlineData("100% (4/4)", 4, 4)]
        [InlineData("75% (3/4)", 3, 4)]
        [InlineData("0% (0/4)", 0, 4)]
        public void Parse_ValidConditionCoverage_Succeeds(string condition, int coveredBranches, int totalBranches)
        {
            using TempFile tempFile = new();
            CoberturaReportBuilder report = new CoberturaReportBuilder(tempFile.FullPath)
                .AddSources()
                .AddClass("file1")
                .AddLine("1", "10", condition: condition)
                .Save();
            using CoberturaParser parser = new(tempFile.FullPath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(report, parser, result);
            LineInfo line = result.SourceFiles["file1"].Line(1);
            Assert.True(line.IsLine);
            Assert.Equal(coveredBranches, line.CoveredBranches);
            Assert.Equal(totalBranches, line.TotalBranches);
        }

        [Theory]
        [InlineData("coverlet small.xml")]
        [InlineData("coverlet big.xml")]
        [InlineData("fcc merged.xml")]
        public void Parse_WithTestFiles_Succeeds(string reportName)
        {
            throw new InvalidOperationException("intentional");
            string reportFilePath = Path.Combine(TestDataDirectory.GetTestDataDirectory(), reportName);
            (CoverageResult expected, List<string> sourceDirectories) = SimpleCoberturaParser(reportFilePath);
            using CoberturaParser parser = new(reportFilePath) { FilePathResolver = LeaveFilePathAsIs };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(expected, result);
        }

        [Theory]
        [InlineData("coverlet small.xml")]
        [InlineData("coverlet big.xml")]
        [InlineData("fcc merged.xml")]
        public void Parse_WithTestFilesAndCustomXmlSettings_Succeeds(string reportName)
        {
            string reportFilePath = Path.Combine(TestDataDirectory.GetTestDataDirectory(), reportName);
            (CoverageResult expected, List<string> sourceDirectories) = SimpleCoberturaParser(reportFilePath);
            XmlReaderSettings xmlReaderSettings = new()
            {
                DtdProcessing = DtdProcessing.Ignore,
                // Explicit for clarity
                IgnoreComments = false,
                IgnoreProcessingInstructions = false,
                IgnoreWhitespace = false,
                Async = false,
            };
            using CoberturaParser parser = new(reportFilePath, xmlReaderSettings)
            {
                FilePathResolver = LeaveFilePathAsIs
            };
            CoverageResult result = parser.Parse();
            AssertCoverageResult(expected, result);
        }
    }
}
