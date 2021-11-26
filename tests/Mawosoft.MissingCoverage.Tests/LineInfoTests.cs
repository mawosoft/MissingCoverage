// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
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
                LineInfoMergeData mergeData = new();
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
    }
}
