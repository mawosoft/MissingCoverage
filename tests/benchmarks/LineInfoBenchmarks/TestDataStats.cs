// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LineInfoBenchmarks
{
    public class TestDataStats
    {
        public int Count, MaxLineNo, Gaps, Branches, Hits;

        public TestDataStats(IEnumerable<LineInfo1Class> lines)
        {
            Count = lines.Count();
            MaxLineNo = lines.LastOrDefault()?.LineNumber ?? 0;
            Gaps = 0;
            int lineNumber = 0;
            foreach (LineInfo1Class line in lines)
            {
                Gaps += line.LineNumber - lineNumber - 1;
                lineNumber = line.LineNumber;

            }
            Branches = lines.Count(l => l.TotalBranches != 0);
            Hits = lines.Count(l => l.Hits != 0);
        }

        public override string ToString()
            => $"Count: {Count}, MaxLineNo: {MaxLineNo}, "
               + $"Gaps: {Gaps} ({Math.Round((double)Gaps / Count * 100)}%), "
               + $"Branches: {Branches} ({Math.Round((double)Branches / Count * 100)}%), "
               + $"Hits: {Hits} ({Math.Round((double)Hits / Count * 100)}%)";
    }
}
