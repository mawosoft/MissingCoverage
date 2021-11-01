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
                  + string.Join(Environment.NewLine, Benchmarks.OperationCountAndCapacity()
                      .Select(item => item.count)
                      .Append(TestDataSource.Instance.Count)
                      .Distinct()
                      .OrderBy(count => count)
                      .Select(count => new Stats(TestDataSource.Instance.Take(count)).ToString())));
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

        public class Stats
        {
            public int Count, MaxLineNo, Gaps, Branches, Hits;

            public Stats(IEnumerable<LineInfo1Class> lines)
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
}
