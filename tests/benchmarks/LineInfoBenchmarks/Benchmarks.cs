// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public static IEnumerable<(string operation, int count, int capacity)> OperationCountAndCapacity()
        {
            // Note: capacity == count means full capacity.
            // For models using an array indexed by line number, this must be replaced with the highest
            // line number - which is also the last one, because TestDataSource provides ordered data.
            int count = Math.Min(1000, TestDataSource.Instance.Count);
            yield return ("add", count, 4);
            yield return ("add", count, count);
            yield return ("update", count, 0);
        }
    }
}
