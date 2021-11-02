// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Reports;

namespace LineInfoBenchmarks
{
    public class TestDataAnalyser : IAnalyser
    {
        private static readonly Lazy<string> s_stats = new(
            () => Environment.NewLine
                  + string.Join(Environment.NewLine, Benchmarks.GenericArgumentsSource()
                      .Select(item => item.stats.Count)
                      .Append(TestDataSource.Instance.Count)
                      .Distinct()
                      .OrderBy(count => count)
                      .Select(count => new TestDataStats(TestDataSource.Instance.Take(count)).ToString())));
        private readonly bool _runOnce = false;
        private bool _hasRun = false;
        public TestDataAnalyser(bool runOnce = true) => _runOnce = runOnce;
        public string Id => nameof(TestDataAnalyser);

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            if (_runOnce && _hasRun)
            {
                yield break;
            }
            _hasRun = true;
            yield return Conclusion.CreateHint(Id, s_stats.Value);
        }
    }
}
