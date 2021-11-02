// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public static IEnumerable<(string group, string operation, TestDataStats stats, int capacity, int branchCapacity)> GenericArgumentsSource()
        {
            // Note: capacity == testDataStats.Count means full capacity. For models using an array indexed
            // by line number, this must be replaced with the highest line number.
            // Likeewise, branchCapacity == testDataStats.Branches means fullCapacity (no replacements here).
            int count = Math.Min(1000, TestDataSource.Instance.Count);
            TestDataStats stats = new(TestDataSource.Instance.Take(count));
            yield return ("1", "add", stats, 4, stats.Branches);
            yield return ("2", "add", stats, count, stats.Branches);
            yield return ("3", "update", stats, 0, 0);
        }
    }
}
