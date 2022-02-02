// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Diagnostics;
using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace EnumBenchmarks
{
    internal enum VerbosityLevel
    {
        Quiet,
        Minimal,
        Normal,
        Detailed,
        Diagnostic
    };

    public class Benchmarks
    {
        [Benchmark(Baseline = true)]
        [Arguments(2)]
        [Arguments((int)VerbosityLevel.Diagnostic + 1)]
        public bool IsDefinedGeneric(int value) => Enum.IsDefined((VerbosityLevel) value);

        [Benchmark]
        [Arguments(2)]
        [Arguments((int)VerbosityLevel.Diagnostic + 1)]
        public bool IsDefinedNonGeneric(int value) => Enum.IsDefined(typeof(VerbosityLevel), value);
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            WhatifFilter whatifFilter = new();
            args = whatifFilter.PreparseConsoleArguments(args);

            ManualConfig config = DefaultConfig.Instance
                .ReplaceColumnCategory(new JobColumnSelectionProvider("-all +Job"))
                .ReplaceExporters(MarkdownExporter.Console)
                .ReplaceLoggers(ConsoleLogger.Unicode)
                .AddFilter(whatifFilter)
                .WithCultureInfo(CultureInfo.CurrentCulture)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

            Summary[] summaries;
            if (!whatifFilter.Enabled && args.Length == 0 && Debugger.IsAttached)
            {
                BenchmarkRunInfos runInfos = new(config, BenchmarkRunInfos.FastInProcessJob);
                //runInfos.ConvertAssemblyToBenchmarks(typeof(Program).Assembly);
                runInfos.ConvertMethodsToBenchmarks(typeof(Benchmarks), "IsDefinedGeneric");
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
