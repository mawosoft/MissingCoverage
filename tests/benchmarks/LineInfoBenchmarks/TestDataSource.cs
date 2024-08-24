// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;

namespace LineInfoBenchmarks
{
    public sealed class TestDataSource : List<LineInfo1Class>
    {
        public static readonly TestDataSource Instance = new();

        // Data generation default specs
        private const int RandomSeed = 8765;
        private const int MaxLines = 5000;
        private const int GapPercent = 10;
        private const int BranchPercent = 10;
        private const int HitPercent = 90;

        private TestDataSource(int randomSeed = RandomSeed,
                               int maxLines = MaxLines,
                               int gapPercent = GapPercent,
                               int branchPercent = BranchPercent,
                               int hitPercent = HitPercent) : base(maxLines)
        {
            Random random = new(randomSeed);
            int gapCeiling = (int)Math.Round(100.0 / gapPercent);
            int branchCeiling = (int)Math.Round(100.0 / branchPercent);
            int hitCeiling = (int)Math.Round(100.0 / (100 - hitPercent));
            for (int i = 1; i <= MaxLines; i++)
            {
                if (random.Next(gapCeiling) != 0)
                {
                    LineInfo1Class line = new();
                    line.LineNumber = i;
                    line.Hits = random.Next(hitCeiling);
                    line.TotalBranches = random.Next(branchCeiling) != 0 ? 0 : 2;
                    if (line.Hits != 0 && line.TotalBranches != 0)
                    {
                        line.CoveredBranches = random.Next(1, 3);
                    }
                    Add(line);
                }
            }
        }
    }
}
