// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
    public class CoverageResultTests
    {
        [Fact]
        public void Ctor_Default_Succeeds()
        {
            CoverageResult result = new();
            Assert.False(result.LatestOnly, "LatestOnly");
            Assert.Empty(result.ReportFilePaths);
            Assert.Empty(result.SourceFiles);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Ctor_LatestOnly_Succeeds(bool latestOnly)
        {
            CoverageResult result = new(latestOnly);
            Assert.Equal(latestOnly, result.LatestOnly);
            Assert.Empty(result.ReportFilePaths);
            Assert.Empty(result.SourceFiles);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Ctor_InvalidReportFilePath_Throws(string reportFilePath)
        {
            ArgumentException ex = Assert.ThrowsAny<ArgumentException>(
                () => _ = new CoverageResult(reportFilePath));
            Assert.Equal("reportFilePath", ex.ParamName);
        }

        [Theory]
        [InlineData("somefile")]
        [InlineData(@"c:\somedir\somefile.xml")]
        [InlineData("/home/somedir/somefile")]
        public void Ctor_ValidReportFilePath_Succeeds(string reportFilePath)
        {
            CoverageResult result = new(reportFilePath);
            Assert.False(result.LatestOnly, "LatestOnly");
            Assert.Equal(reportFilePath, Assert.Single(result.ReportFilePaths));
            Assert.Empty(result.SourceFiles);
        }

        [Fact]
        public void AddOrMergeSourceFile_NullArgument_Throws()
        {
            CoverageResult result = new();
            Assert.Throws<ArgumentNullException>("sourceFile", () => result.AddOrMergeSourceFile(null!));
        }

        [Fact]
        public void AddOrMergeSourceFile_AddsNew()
        {
            CoverageResult result = new();

            SourceFileInfo source1 = new("file1", DateTime.UtcNow);
            source1.AddOrMergeLine(1, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source1);
            Assert.Same(source1, result.SourceFiles["file1"]);

            SourceFileInfo source2 = new("file2", DateTime.UtcNow);
            source2.AddOrMergeLine(2, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source2);
            Assert.Same(source2, result.SourceFiles["file2"]);
        }

        [Fact]
        public void AddOrMergeSourceFile_MergesExisting()
        {
            CoverageResult result = new();

            SourceFileInfo source1 = new("file1", DateTime.UtcNow);
            source1.AddOrMergeLine(1, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source1);

            SourceFileInfo source2 = new("file1", DateTime.UtcNow);
            source2.AddOrMergeLine(2, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source2);

            Assert.Same(source1, result.SourceFiles["file1"]);
            Assert.True(source1.Line(1).IsLine);
            Assert.True(source1.Line(2).IsLine);
        }

        [Fact]
        public void AddOrMergeSourceFile_WithLatestOnly_ReplacesFromNewerReport()
        {
            CoverageResult result = new(latestOnly: true);

            SourceFileInfo source1 = new("file1", DateTime.UtcNow - TimeSpan.FromHours(1));
            source1.AddOrMergeLine(1, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source1);

            SourceFileInfo source2 = new("file1", DateTime.UtcNow);
            source2.AddOrMergeLine(2, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source2);

            Assert.Same(source2, result.SourceFiles["file1"]);
            Assert.False(source2.Line(1).IsLine);
            Assert.True(source2.Line(2).IsLine);
        }

        [Fact]
        public void AddOrMergeSourceFile_WithLatestOnly_IgnoresFromOlderReport()
        {
            CoverageResult result = new(latestOnly: true);

            SourceFileInfo source1 = new("file1", DateTime.UtcNow);
            source1.AddOrMergeLine(1, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source1);

            SourceFileInfo source2 = new("file1", DateTime.UtcNow - TimeSpan.FromHours(1));
            source2.AddOrMergeLine(2, new() { Hits = 0 });
            result.AddOrMergeSourceFile(source2);

            Assert.Same(source1, result.SourceFiles["file1"]);
            Assert.True(source1.Line(1).IsLine);
            Assert.False(source1.Line(2).IsLine);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Merge_Succeeds(bool latestOnly)
        {
            CoverageResult result1, result2, result3;
            DateTime reportTimestamp1, reportTimestamp2, reportTimestamp3;
            SourceFileInfo source;
            CoverageResult mergedResult = new(latestOnly);

            result1 = new("report1");
            reportTimestamp1 = DateTime.UtcNow - TimeSpan.FromHours(2);
            source = new("file1", reportTimestamp1);
            source.AddOrMergeLine(1, new() { Hits = 0 });
            result1.AddOrMergeSourceFile(source);
            source = new("file2", reportTimestamp1);
            source.AddOrMergeLine(2, new() { Hits = 0 });
            result1.AddOrMergeSourceFile(source);

            result2 = new("report2");
            reportTimestamp2 = DateTime.UtcNow;
            source = new("file1", reportTimestamp2);
            source.AddOrMergeLine(3, new() { Hits = 0 });
            result2.AddOrMergeSourceFile(source);
            source = new("file2", reportTimestamp2);
            source.AddOrMergeLine(4, new() { Hits = 0 });
            result2.AddOrMergeSourceFile(source);
            source = new("file3", reportTimestamp2);
            source.AddOrMergeLine(5, new() { Hits = 0 });
            result2.AddOrMergeSourceFile(source);

            result3 = new("report3");
            reportTimestamp3 = DateTime.UtcNow - TimeSpan.FromHours(1);
            source = new("file3", reportTimestamp3);
            source.AddOrMergeLine(6, new() { Hits = 0 });
            result3.AddOrMergeSourceFile(source);

            mergedResult.Merge(result1);
            mergedResult.Merge(result2);
            mergedResult.Merge(result3);

            Assert.Equal(3, mergedResult.ReportFilePaths.Count);
            Assert.Contains("report1", mergedResult.ReportFilePaths);
            Assert.Contains("report2", mergedResult.ReportFilePaths);
            Assert.Contains("report3", mergedResult.ReportFilePaths);

            Assert.Equal(3, mergedResult.SourceFiles.Count);
            // For cast see xunit issue #1857
            source = Assert.Contains("file1", (IDictionary<string, SourceFileInfo>)mergedResult.SourceFiles);
            Assert.Equal(!latestOnly, source.Line(1).IsLine);
            Assert.True(source.Line(3).IsLine);
            source = Assert.Contains("file2", (IDictionary<string, SourceFileInfo>)mergedResult.SourceFiles);
            Assert.Equal(!latestOnly, source.Line(2).IsLine);
            Assert.True(source.Line(4).IsLine);
            source = Assert.Contains("file3", (IDictionary<string, SourceFileInfo>)mergedResult.SourceFiles);
            Assert.True(source.Line(5).IsLine);
            Assert.Equal(!latestOnly, source.Line(6).IsLine);
        }
    }
}
