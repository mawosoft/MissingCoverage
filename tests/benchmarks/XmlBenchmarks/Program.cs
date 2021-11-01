// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace XmlBenchmarks
{
    internal class Program
    {
        // Additional console arguments
        private static readonly string s_helpInfo = @"
  -fi|--fileinfo              Print test file info with summaries
  -fs|--fileselector <enum>   Test file(s) to use: All|AllKnown|Small|Big|Merged|Large
  -ms|--mocksize <mb>         Size in MB of large mock file (0: no mock file)
   -v|--validate              Validate consistent results in XmlLoadAndParseBenchmarks
   -w|--whatif                Print what-if summaries
";
        private bool _printFileInfo = false;
        private TestFiles.FileSelector? _testFileSelector = null;
        private int? _mockFileSizeMB = null;
        private bool _validate = false;
        private bool _printHelp = false;

        internal static void Main(string[] args)
        {
            Program program = new();
            program.Run(args);
        }
        private void Run(string[] args)
        {
            WhatifFilter whatifFilter = new();
            args = whatifFilter.PreparseConsoleArguments(args);
            if (whatifFilter.Enabled)
            {
                _mockFileSizeMB = 0; // Default to no mock file if --whatif
            }

            args = PreparseArguments(args);

            TestFiles.Setup(directoryPath: null, testFileSelector: _testFileSelector, mockFileSizeMB: _mockFileSizeMB);

            ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
                .ReplaceColumnCategory(new JobColumnSelectionProvider("-all +Job"))
                .ReplaceLoggers(ConsoleLogger.Unicode)
                .ReplaceExporters(MarkdownExporter.Console, JsonExporter.FullCompressed)
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns: false)))
                .AddFilter(whatifFilter)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true); // We do get a warning anyway
            if (_printFileInfo)
            {
                config.AddAnalyser(new TestFilesAnalyser(runOnce: true));
            }
            if (_validate)
            {
                // XmlLoadAndParseBenchmarks implements its own validator
                config.AddValidator(new XmlLoadAndParseBenchmarks());
            }

            Summary[] summaries;
            if (!whatifFilter.Enabled && args.Length == 0 && Debugger.IsAttached)
            {
                BenchmarkRunInfos runInfos = new(config, BenchmarkRunInfos.FastInProcessJob);
                runInfos.ConvertAssemblyToBenchmarks(typeof(Program).Assembly);
                summaries = runInfos.RunAll();
            }
            else
            {
                summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
            }
            _ = summaries;

            if (_printHelp)
            {
                ConsoleLogger.Unicode.WriteLineInfo(s_helpInfo);
            }

            if (whatifFilter.Enabled)
            {
                whatifFilter.PrintAsSummaries(ConsoleLogger.Unicode);
                whatifFilter.Clear(dispose: true);
            }

            TestFiles.Cleanup();
        }

        private string[] PreparseArguments(string[] args)
        {
            // Reserved BDN single char arguments: -a -d -e -f -i -j -m -p -r -t
            // Reserved WhatifFilter: -w
            int i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-fi" or "--fileinfo");
            if (i >= 0)
            {
                _printFileInfo = true;
                args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();
            }
            i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-fs" or "--fileselector");
            if (i >= 0 && i < args.Length - 1
                && Enum.TryParse(args[i + 1], ignoreCase: true, out TestFiles.FileSelector fs)
                && Enum.IsDefined(fs))
            {
                _testFileSelector = fs;
                args = args.Take(i).Concat(args.Skip(i + 2)).ToArray();
            }
            i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-ms" or "--mocksize");
            if (i >= 0 && i < args.Length - 1 && int.TryParse(args[i + 1], out int ms))
            {
                _mockFileSizeMB = ms;
                args = args.Take(i).Concat(args.Skip(i + 2)).ToArray();
            }
            i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-v" or "--validate");
            if (i >= 0)
            {
                _validate = true;
                args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();
            }
            i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-h" or "--help");
            if (i >= 0)
            {
                _printHelp = true;
                // Don't remove. "--help" is a BDN option.
                // While BDN doesn't recognize "-h", it will print the help screen anyway.
            }
            return args;
        }

        private class TestFilesAnalyser : IAnalyser
        {
            private static readonly Lazy<string> s_fileInfo = new(
                () => Environment.NewLine
                      + CoberturaFileInfo.ToMarkDown(
                          TestFiles.GetFilePaths().Select(f => new CoberturaFileInfo(f)),
                          CoberturaFileInfo.TablePaddingOptions.Pad,
                          CultureInfo.InvariantCulture, "N0")
                );
            private readonly bool _runOnce = false;
            private bool _hasRun = false;

            public TestFilesAnalyser(bool runOnce = true) => _runOnce = runOnce;
            public string Id => nameof(TestFilesAnalyser);

            public IEnumerable<Conclusion> Analyse(Summary summary)
            {
                if (_runOnce && _hasRun)
                {
                    yield break;
                }
                _hasRun = true;
                yield return Conclusion.CreateHint(Id, s_fileInfo.Value);
            }
        }

    }
}
