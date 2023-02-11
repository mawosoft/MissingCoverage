// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace LineInfoBenchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WhatifFilter whatifFilter = new();
            args = whatifFilter.PreparseConsoleArguments(args);

            ManualConfig config = DefaultConfig.Instance
                .ReplaceColumnCategory(new JobColumnSelectionProvider("-all +Job"))
                // We don't need the individual "Measurements", so JsonExporter.Brief would be sufficient,
                // except that it also excludes "Metrics" (memory allocation in this case).
                .ReplaceExporters(MarkdownExporter.Console, JsonExporter.FullCompressed)
                .ReplaceLoggers(ConsoleLogger.Unicode)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddAnalyser(new TestDataAnalyser(runOnce: true))
                .AddFilter(whatifFilter)
                .WithOrderer(new ParamGroupOrderer(DefaultOrderer.Instance))
                .WithCultureInfo(CultureInfo.CurrentCulture)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

            Summary[] summaries;
            if (!whatifFilter.Enabled && args.Length == 0 && Debugger.IsAttached)
            {
                BenchmarkRunInfos runInfos = new(config, BenchmarkRunInfos.FastInProcessJob);
                //runInfos.ConvertAssemblyToBenchmarks(typeof(Program).Assembly);
                runInfos.ConvertMethodsToBenchmarks(typeof(Benchmarks), "Array_LineInfo1Class");
                summaries = runInfos.RunAll();
            }
            else
            {
                summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
            }
            _ = summaries;

            if (whatifFilter.Enabled)
            {
                whatifFilter.PrintAsSummaries(ConsoleLogger.Unicode);
                whatifFilter.Clear(dispose: true);
            }
        }
    }
}
