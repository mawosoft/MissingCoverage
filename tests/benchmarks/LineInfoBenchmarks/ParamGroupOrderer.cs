// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;


namespace LineInfoBenchmarks
{
    // Assumes that all benchmarks have a parameter named "group" with a distinct value per parameter group.
    // This allows for differing values in other parameters which are supposed to mean the same without hacking
    // the parameter display via ParamWrapper.
    // Example:
    //   - Parameter "count" (line count) may also include the highest line number in index-based implementations.
    //   - Parameter "capacity" equals either line count or highest line number for full capacity depending on
    //     implementation.
    //     For some implementations, it may also include a secondary capacity if the line info is split into
    //     different compounds (e.g. indexed array for hits, dictionary for branches).
    //
    // Currently, we do not check for baselines or logical group rules as the DefaultOrderer does, we simply
    // always return the "group" value and only if it is missing, we call the inner orderer.

    public class ParamGroupOrderer : IOrderer
    {
        private readonly IOrderer _inner;

        public ParamGroupOrderer(IOrderer inner) => _inner = inner;

        public bool SeparateLogicalGroups => _inner.SeparateLogicalGroups;

        public IEnumerable<BenchmarkCase> GetExecutionOrder(ImmutableArray<BenchmarkCase> benchmarksCase, IEnumerable<BenchmarkLogicalGroupRule> order) => _inner.GetExecutionOrder(benchmarksCase, order);
        public string GetHighlightGroupKey(BenchmarkCase benchmarkCase)
            => benchmarkCase.Parameters.Items.FirstOrDefault(
                   p => p.Name.Equals("group", StringComparison.OrdinalIgnoreCase))?.Value?.ToString()
               ?? _inner.GetHighlightGroupKey(benchmarkCase);

        public string GetLogicalGroupKey(ImmutableArray<BenchmarkCase> allBenchmarksCases, BenchmarkCase benchmarkCase)
            => benchmarkCase.Parameters.Items.FirstOrDefault(
                   p => p.Name.Equals("group", StringComparison.OrdinalIgnoreCase))?.Value?.ToString()
               ?? _inner.GetLogicalGroupKey(allBenchmarksCases, benchmarkCase);

        public IEnumerable<IGrouping<string, BenchmarkCase>> GetLogicalGroupOrder(IEnumerable<IGrouping<string, BenchmarkCase>> logicalGroups, IEnumerable<BenchmarkLogicalGroupRule> order) => _inner.GetLogicalGroupOrder(logicalGroups, order);
        public IEnumerable<BenchmarkCase> GetSummaryOrder(ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary) => _inner.GetSummaryOrder(benchmarksCases, summary);
    }
}
