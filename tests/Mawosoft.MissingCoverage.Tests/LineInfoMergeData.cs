// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class LineInfoMergeData : List<(LineInfo target, LineInfo other, LineInfo expected)>
    {
        public LineInfoMergeData() : base(10)
        {
            LineInfo target, other, expected;

            target = new() { Hits = 20, CoveredBranches = 10, TotalBranches = 10 };
            SetPrivateHits(ref target, (uint)target.Hits); // Clear the IsLine flag
            other = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            expected = other;
            Add((target, other, expected));

            target = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            other = new() { Hits = 20, CoveredBranches = 10, TotalBranches = 10 };
            SetPrivateHits(ref other, (uint)other.Hits); // Clear the IsLine flag
            expected = target;
            Add((target, other, expected));

            target = new() { Hits = 20, CoveredBranches = 5, TotalBranches = 10 };
            other = new() { Hits = 10, CoveredBranches = 10, TotalBranches = 10 };
            expected = new() { Hits = 20, CoveredBranches = 10, TotalBranches = 10 };
            Add((target, other, expected));

            target = new() { Hits = 10, CoveredBranches = 8, TotalBranches = 10 };
            other = new() { Hits = 20, CoveredBranches = 5, TotalBranches = 10 };
            expected = new() { Hits = 20, CoveredBranches = 8, TotalBranches = 10 };
            Add((target, other, expected));

            // If TotalBranches differ, best coverage ratio is picked
            target = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 8 };
            other = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            expected = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 8 };
            Add((target, other, expected));

            target = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            other = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 8 };
            expected = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 8 };
            Add((target, other, expected));

            target = new() { Hits = 10, CoveredBranches = 0, TotalBranches = 0 };
            other = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            expected = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            Add((target, other, expected));

            // If ratio is same, most branches is picked
            target = new() { Hits = 10, CoveredBranches = 4, TotalBranches = 8 };
            other = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            expected = new() { Hits = 10, CoveredBranches = 5, TotalBranches = 10 };
            Add((target, other, expected));

            static void SetPrivateHits(ref LineInfo lineInfo, uint hits)
            {
                FieldInfo hitsField =
                    typeof(LineInfo).GetField("_hits", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Assert.NotNull(hitsField);
                object obj = lineInfo;
                hitsField.SetValue(obj, hits);
                lineInfo = (LineInfo)obj;
            }
        }
    }
}
