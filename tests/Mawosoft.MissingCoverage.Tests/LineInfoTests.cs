// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage.Tests;

public class LineInfoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void Hits_Roundtrip(int hits)
    {
        LineInfo lineInfo = default;
        Assert.Equal(0, lineInfo.Hits);
        Assert.False(lineInfo.IsLine);
        lineInfo.Hits = hits;
        Assert.Equal(hits, lineInfo.Hits);
        Assert.True(lineInfo.IsLine);
        lineInfo.Hits = 0;
        Assert.Equal(0, lineInfo.Hits);
        Assert.True(lineInfo.IsLine);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    [InlineData(int.MinValue)]
    public void Hits_NegativeValue_Throws(int hits)
    {
        LineInfo lineInfo = default;
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => lineInfo.Hits = hits);
        Assert.Equal(nameof(LineInfo.Hits), ex.ParamName);
        Assert.Equal(default, lineInfo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ushort.MaxValue)]
    public void CoveredBranches_Roundtrip(ushort coveredBranches)
    {
        LineInfo lineInfo = default;
        Assert.Equal(0, lineInfo.CoveredBranches);
        lineInfo.CoveredBranches = coveredBranches;
        Assert.Equal(coveredBranches, lineInfo.CoveredBranches);
        lineInfo.CoveredBranches = 0;
        Assert.Equal(default, lineInfo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(ushort.MaxValue)]
    public void TotalBranches_Roundtrip(ushort totalBranches)
    {
        LineInfo lineInfo = default;
        Assert.Equal(0, lineInfo.TotalBranches);
        lineInfo.TotalBranches = totalBranches;
        Assert.Equal(totalBranches, lineInfo.TotalBranches);
        lineInfo.TotalBranches = 0;
        Assert.Equal(default, lineInfo);
    }

    private class Merge_TheoryData : TheoryData<LineInfo, LineInfo, LineInfo>
    {
        public Merge_TheoryData()
        {
            LineInfoMergeData mergeData = [];
            foreach ((LineInfo target, LineInfo other, LineInfo expected) in mergeData)
            {
                Add(target, other, expected);
            }
        }
    }

    [Theory]
    [ClassData(typeof(Merge_TheoryData))]
    internal void Merge_Succeeds(LineInfo target, LineInfo other, LineInfo expected)
    {
        target.Merge(other);
        Assert.Equal(expected, target);
    }

    private class Equals_TheoryData : TheoryData<object?, object?, bool>
    {
        public Equals_TheoryData()
        {
            LineInfo left = default;
            LineInfo right = default;
            Add(left, right, true);
            Add(left, null, false);
            Add(left, 0UL, false);
            left.Hits = 0;
            Add(left, right, false);
            right.Hits = 0;
            Add(left, right, true);
            left.Hits = 42;
            Add(left, right, false);
            right.Hits = 42;
            Add(left, right, true);
            left.TotalBranches = 4;
            Add(left, right, false);
            right.TotalBranches = 4;
            Add(left, right, true);
            left.CoveredBranches = 2;
            Add(left, right, false);
            right.CoveredBranches = 2;
            Add(left, right, true);
        }
    }

    [Theory]
    [ClassData(typeof(Equals_TheoryData))]
    public void Equals_Succeeds(object? left, object? right, bool expected)
    {
        if (left != null)
        {
            Assert.True(left.Equals(left));
            Assert.Equal(expected, left.Equals(right));
            Assert.Equal(expected, left.GetHashCode().Equals(right?.GetHashCode()));
        }
        if (right != null)
        {
            Assert.True(right.Equals(right));
            Assert.Equal(expected, right.Equals(left));
            Assert.Equal(expected, right.GetHashCode().Equals(left?.GetHashCode()));
        }
        if (left is LineInfo leftLine)
        {
            Assert.True(leftLine.Equals(leftLine));
            Assert.Equal(expected, leftLine.Equals(right));
            Assert.Equal(expected, leftLine.GetHashCode().Equals(right?.GetHashCode()));
            if (right is LineInfo rightLine)
            {
                Assert.True(rightLine.Equals(rightLine));
                Assert.Equal(expected, leftLine.Equals(rightLine));
                Assert.Equal(expected, rightLine.Equals(leftLine));
                Assert.Equal(expected, leftLine.GetHashCode().Equals(rightLine.GetHashCode()));
            }
        }
    }
}
