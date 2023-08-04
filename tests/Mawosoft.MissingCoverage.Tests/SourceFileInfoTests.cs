// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests;

public class SourceFileInfoTests
{
    internal static LineInfo[] GetPrivateLinesArray(SourceFileInfo sourceFileInfo)
    {
        FieldInfo linesField =
            typeof(SourceFileInfo).GetField("_lines", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(linesField);
        LineInfo[] lines = (linesField.GetValue(sourceFileInfo) as LineInfo[])!;
        Assert.NotNull(lines);
        return lines;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Ctor_InvalidSourceFilePath_Throws(string sourceFilePath)
    {
        ArgumentException ex = Assert.ThrowsAny<ArgumentException>(
            () => _ = new SourceFileInfo(sourceFilePath, DateTime.UtcNow));
        Assert.Equal(nameof(SourceFileInfo.SourceFilePath), ex.ParamName, ignoreCase: true);
    }

    [Theory]
    [InlineData("somefile")]
    [InlineData(@"c:\somedir\somefile.cs")]
    [InlineData("/home/somedir/somefile")]
    public void Ctor_ValidSourceFilePath_Succeeds(string sourceFilePath)
    {
        DateTime initialStamp = DateTime.UtcNow;
        SourceFileInfo info = new(sourceFilePath, initialStamp);
        Assert.Equal(sourceFilePath, info.SourceFilePath);
        Assert.Equal(initialStamp, info.ReportTimestamp);
        Assert.Equal(0, info.LastLineNumber);
        Assert.Equal(SourceFileInfo.DefaultLineCount, GetPrivateLinesArray(info).Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SourceFilePath_InvalidValue_Throws(string sourceFilePath)
    {
        string initialPath = "foo";
        SourceFileInfo info = new(initialPath, DateTime.UtcNow);
        ArgumentException ex = Assert.ThrowsAny<ArgumentException>(
            () => info.SourceFilePath = sourceFilePath);
        Assert.Equal(nameof(SourceFileInfo.SourceFilePath), ex.ParamName);
        Assert.Equal(initialPath, info.SourceFilePath);
    }

    [Theory]
    [InlineData("somefile")]
    [InlineData(@"c:\somedir\somefile.xml")]
    [InlineData("/home/somedir/somefile")]
    public void SourceFilePath_Roundtrip(string sourceFilePath)
    {
        string initialPath = "foo";
        SourceFileInfo info = new(initialPath, DateTime.UtcNow);
        Assert.Equal(initialPath, info.SourceFilePath);
        info.SourceFilePath = sourceFilePath;
        Assert.Equal(sourceFilePath, info.SourceFilePath);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    [InlineData(int.MinValue)]
    public void DefaultLineCount_InvalidValue_Throws(int defaultLineCount)
    {
        int initialValue = SourceFileInfo.DefaultLineCount;
        try
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                nameof(SourceFileInfo.DefaultLineCount),
                () => SourceFileInfo.DefaultLineCount = defaultLineCount);
            Assert.Equal(initialValue, SourceFileInfo.DefaultLineCount);
        }
        finally
        {
            SourceFileInfo.DefaultLineCount = initialValue;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(int.MaxValue)]
    public void DefaultLineCount_Roundtrip(int defaultLineCount)
    {
        int initialValue = SourceFileInfo.DefaultLineCount;
        Assert.Equal(500, initialValue); // Track changes
        try
        {
            SourceFileInfo.DefaultLineCount = defaultLineCount;
            Assert.Equal(defaultLineCount, SourceFileInfo.DefaultLineCount);
        }
        finally
        {
            SourceFileInfo.DefaultLineCount = initialValue;
            Assert.Equal(initialValue, SourceFileInfo.DefaultLineCount);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100_000)]
    [InlineData(int.MaxValue)]
    [InlineData("Length")]
    public void IndexerRefGet_InvalidIndex_Throws(object index)
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        int idx = index switch
        {
            string s when s == "Length" => GetPrivateLinesArray(info).Length,
            _ => (int)index,
        };
        Exception ex1 = Assert.ThrowsAny<Exception>(() => { _ = ref info.Line(idx); });
        Exception ex2 = Assert.ThrowsAny<Exception>(() => { _ = GetPrivateLinesArray(info)[idx]; });
        Assert.Equal(ex2.GetType(), ex1.GetType());
        Assert.Equal(ex2.Message, ex1.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    public void IndexerRefGet_ValidIndex_Succeeds(int index)
    {
        Assert.InRange(index, 0, SourceFileInfo.DefaultLineCount - 1);
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        GetPrivateLinesArray(info)[index].Hits = index;
        GetPrivateLinesArray(info)[index].TotalBranches = 1;
        ref readonly LineInfo line = ref info.Line(index);
        Assert.Equal(index, line.Hits);
        Assert.Equal(1, line.TotalBranches);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100_000)]
    [InlineData(int.MaxValue)]
    [InlineData("MaxLineNumber+1")]
    public void AddOrMergeLine_InvalidLineNumber_Throws(object lineNumber)
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo line = default;
        int lineNo = lineNumber switch
        {
            string s when s == "MaxLineNumber+1" => SourceFileInfo.MaxLineNumber + 1,
            _ => (int)lineNumber,
        };
        _ = Assert.Throws<IndexOutOfRangeException>(() => info.AddOrMergeLine(lineNo, line));
    }

    [Fact]
    public void AddOrMergeLine_NotIsLine_IgnoresLine()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo line = default;
        info.AddOrMergeLine(10, line);
        Assert.Equal(0, info.LastLineNumber);
    }

    [Fact]
    public void AddOrMergeLine_IncreasesLastLineNumber()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo line = new() { Hits = 0 };
        info.AddOrMergeLine(10, line);
        Assert.Equal(10, info.LastLineNumber);
        info.AddOrMergeLine(5, line);
        Assert.Equal(10, info.LastLineNumber);
        info.AddOrMergeLine(42, line);
        Assert.Equal(42, info.LastLineNumber);
    }

    [Fact]
    public void AddOrMergeLine_GrowsLinesArray()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo line = new() { Hits = 0 };
        int length = GetPrivateLinesArray(info).Length;
        info.AddOrMergeLine(length - 1, line);
        Assert.Equal(length, GetPrivateLinesArray(info).Length);
        info.AddOrMergeLine(length, line);
        Assert.InRange(GetPrivateLinesArray(info).Length, length + 1, int.MaxValue);
    }

    [Fact]
    public void AddOrMergeLine_AddsNewLine()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo line = new() { Hits = 3, CoveredBranches = 1, TotalBranches = 2 };
        info.AddOrMergeLine(10, line);
        Assert.Equal(default, info.Line(5));
        info.AddOrMergeLine(5, line);
        Assert.Equal(line, info.Line(5));
    }

    [Fact]
    public void AddOrMergeLine_MergesExistingLine()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        LineInfo target = new() { Hits = 20, CoveredBranches = 5, TotalBranches = 10 };
        LineInfo other = new() { Hits = 10, CoveredBranches = 10, TotalBranches = 10 };
        LineInfo expected = new() { Hits = 20, CoveredBranches = 10, TotalBranches = 10 };
        info.AddOrMergeLine(10, target);
        Assert.Equal(default, info.Line(5));
        info.AddOrMergeLine(5, target);
        Assert.Equal(target, info.Line(5));
        info.AddOrMergeLine(5, other);
        Assert.Equal(expected, info.Line(5));
    }

    [Fact]
    public void Merge_NullArgument_Throws()
    {
        SourceFileInfo info = new("somefile", DateTime.UtcNow);
        Assert.Throws<ArgumentNullException>("other", () => info.Merge(null!));
    }

    private class Merge_TheoryData : TheoryData<SourceFileInfo, SourceFileInfo, SourceFileInfo>
    {
        public Merge_TheoryData()
        {
            SourceFileInfo targetFile1 = new("somefile", DateTime.UtcNow - TimeSpan.FromHours(1));
            SourceFileInfo otherFile1 = new(targetFile1.SourceFilePath, DateTime.UtcNow);
            SourceFileInfo expectedFile1 = new(targetFile1.SourceFilePath, targetFile1.ReportTimestamp);
            SourceFileInfo targetFile2 = new(otherFile1.SourceFilePath, otherFile1.ReportTimestamp);
            SourceFileInfo otherFile2 = new(targetFile1.SourceFilePath, targetFile1.ReportTimestamp);
            SourceFileInfo expectedFile2 = new(targetFile2.SourceFilePath, targetFile2.ReportTimestamp);
            LineInfoMergeData mergeData = new();
            int lineNumber = 0;
            foreach ((LineInfo target, LineInfo other, LineInfo expected) in mergeData)
            {
                lineNumber++;
                if ((lineNumber % 3) == 0) lineNumber++; // Add some gaps
                targetFile1.AddOrMergeLine(lineNumber, target);
                targetFile2.AddOrMergeLine(lineNumber, target);
                otherFile1.AddOrMergeLine(lineNumber, other);
                otherFile2.AddOrMergeLine(lineNumber, other);
                expectedFile1.AddOrMergeLine(lineNumber, expected);
                expectedFile2.AddOrMergeLine(lineNumber, expected);
            }
            lineNumber = GetPrivateLinesArray(targetFile1).Length;
            foreach ((_, _, LineInfo expected) in mergeData)
            {
                lineNumber++;
                if ((lineNumber % 3) == 0) lineNumber++; // Add some gaps
                targetFile1.AddOrMergeLine(lineNumber, expected);
                otherFile2.AddOrMergeLine(lineNumber, expected);
                expectedFile1.AddOrMergeLine(lineNumber, expected);
                expectedFile2.AddOrMergeLine(lineNumber, expected);
            }
            Add(targetFile1, otherFile1, expectedFile1);
            Add(targetFile2, otherFile2, expectedFile2);
        }
    }

    [Theory]
    [ClassData(typeof(Merge_TheoryData))]
    internal void Merge_Succeeds(SourceFileInfo target, SourceFileInfo other, SourceFileInfo expected)
    {
        target.Merge(other);
        Assert.Equal(expected.SourceFilePath, target.SourceFilePath);
        Assert.Equal(expected.ReportTimestamp, target.ReportTimestamp);
        Assert.Equal(expected.LastLineNumber, target.LastLineNumber);
        for (int i = 1; i <= expected.LastLineNumber; i++)
        {
            Assert.Equal((i, expected.Line(i)), (i, target.Line(i)));
        }
    }

    [Fact]
    internal void LineSequences_Succeeds()
    {
        SourceFileInfo source = new("somefile", DateTime.UtcNow);
        List<(int, int)> expected = new();
        Assert.Equal(expected, source.LineSequences());
        // Force LastLineNumber = 5, but clear line afterwards
        source.AddOrMergeLine(5, new() { Hits = 0 });
        GetPrivateLinesArray(source)[5] = default;
        Assert.Equal(expected, source.LineSequences());
        source.AddOrMergeLine(2, new() { Hits = 0 });
        expected.Add((2, 2));
        Assert.Equal(expected, source.LineSequences());
        source.AddOrMergeLine(3, source.Line(2));
        expected[^1] = (2, 3);
        Assert.Equal(expected, source.LineSequences());
        source.AddOrMergeLine(4, new() { Hits = 10 });
        source.AddOrMergeLine(5, source.Line(4));
        source.AddOrMergeLine(6, source.Line(4));
        expected.Add((4, 6));
        Assert.Equal(expected, source.LineSequences());
        source.AddOrMergeLine(7, new() { Hits = 10, TotalBranches = 4 });
        expected.Add((7, 7));
        Assert.Equal(expected, source.LineSequences());
        source.AddOrMergeLine(9, source.Line(7));
        source.AddOrMergeLine(10, source.Line(7));
        expected.Add((9, 10));
        Assert.Equal(expected, source.LineSequences());
    }
}

[Collection(nameof(NoParallelTests))]
public class SourceFileInfoTests_NoParallelTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    [InlineData(int.MinValue)]
    public void MaxLineNumber_InvalidValue_Throws(int maxLineNumber)
    {
        int initialValue = SourceFileInfo.MaxLineNumber;
        try
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                nameof(SourceFileInfo.MaxLineNumber),
                () => SourceFileInfo.MaxLineNumber = maxLineNumber);
            Assert.Equal(initialValue, SourceFileInfo.MaxLineNumber);
        }
        finally
        {
            SourceFileInfo.MaxLineNumber = initialValue;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(int.MaxValue)]
    public void MaxLineNumber_Roundtrip(int maxLineNumber)
    {
        int initialValue = SourceFileInfo.MaxLineNumber;
        Assert.Equal(50_000, initialValue); // Track changes
        try
        {
            SourceFileInfo.MaxLineNumber = maxLineNumber;
            Assert.Equal(maxLineNumber, SourceFileInfo.MaxLineNumber);
        }
        finally
        {
            SourceFileInfo.MaxLineNumber = initialValue;
            Assert.Equal(initialValue, SourceFileInfo.MaxLineNumber);
        }
    }
}
