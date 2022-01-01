// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Mawosoft.MissingCoverage
{
    internal class Program
    {
        public static string Name => nameof(MissingCoverage);
        public static string Version { get; } = (Attribute.GetCustomAttribute(typeof(Program).Assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute)?.InformationalVersion?.Split('+')[0] ?? string.Empty;
        public static string Copyright { get; } = (Attribute.GetCustomAttribute(typeof(Program).Assembly, typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright ?? string.Empty;

        [Flags]
        private enum TitlesWritten
        {
            None = 0,
            AppTitle = 1,
            InputTitle = 2,
            ResultTitle = 4,
        }

        private TitlesWritten _titlesWritten;
        private int _exitCode;

        public TextWriter Out { get; set; } = Console.Out;
        public TextWriter Error { get; set; } = Console.Error;
        public Options Options { get; set; } = new();

        public static int Main(string[] args)
        {
            Program program = new();
            return program.Run(args);
        }

        public int Run(string[] args)
        {
            _titlesWritten = TitlesWritten.None;
            _exitCode = 0;
            try
            {
                Configure(args);
                if (Options.ShowHelpOnly)
                {
                    WriteHelpText();
                }
                else
                {
                    IEnumerable<string> inputFilePaths = GetInputFiles();
                    CoverageResult mergedResult = ProcessInputFiles(inputFilePaths);
                    WriteResults(mergedResult);
                }
            }
            catch (Exception ex)
            {
                WriteToolError(ex);
            }
            return _exitCode;
        }

        internal void Configure(string[] args)
        {
            try
            {
                Options.ParseCommandLineArguments(args);
            }
            catch (Exception ex)
            {
                Options.ShowHelpOnly = true;
                WriteToolError(ex);
                return;
            }
            // TODO process other Options sources like settings file.
            if (Options.GlobPatterns.Count == 0)
            {
                Options.GlobPatterns.Add(Path.Combine("**", "*cobertura*.xml"));
            }
            if (Options.MaxLineNumber.IsSet)
            {
                SourceFileInfo.MaxLineNumber = Options.MaxLineNumber;
            }

        }

        internal IEnumerable<string> GetInputFiles()
        {
            if (_exitCode != 0)
            {
                return Array.Empty<string>();
            }

            List<string> inputFilePaths = new();
            HashSet<string> uniqueInputs = new();
            Matcher? matcher = null;
            string lastRoot = string.Empty;
            foreach (string arg in Options.GlobPatterns)
            {
                string root = Path.GetPathRoot(arg) ?? string.Empty;
                if (root != lastRoot)
                {
                    ExecuteMatcher();
                    lastRoot = root;
                }
                matcher ??= new();
                matcher.AddInclude(root.Length == 0 ? arg : Path.GetRelativePath(root, arg));
            }
            ExecuteMatcher(); // Handle any remains
            if (inputFilePaths.Count == 0)
            {
                WriteAppTitle();
                WriteLineDetailed("Working directory: " + Directory.GetCurrentDirectory());
                throw new InvalidOperationException("No matching input files.");
            }
            return inputFilePaths;

            void ExecuteMatcher()
            {
                if (matcher != null)
                {
                    DirectoryInfo dirInfo = new(lastRoot.Length == 0 ? "." : lastRoot);
                    PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(dirInfo));
                    foreach (FilePatternMatch file in result.Files)
                    {
                        string fullPath = Path.GetFullPath(Path.Combine(dirInfo.FullName, file.Path));
                        if (uniqueInputs.Add(fullPath))
                        {
                            inputFilePaths.Add(fullPath);
                        }
                    }
                    matcher = null;
                }
            }
        }

        internal CoverageResult ProcessInputFiles(IEnumerable<string> inputFilePaths)
        {
            CoverageResult mergedResult = new(Options.LatestOnly);
            if (_exitCode != 0)
            {
                return mergedResult;
            }

            foreach (string inputFile in inputFilePaths)
            {
                CoberturaParser? parser = null;
                try
                {
                    parser = new(inputFile);
                    CoverageResult result = parser.Parse();
                    parser.Dispose();
                    mergedResult.Merge(result);
                    WriteInputTitle();
                    WriteLineNormal(inputFile);
                }
                catch (Exception ex)
                {
                    WriteInputTitle();
                    WriteFileError(inputFile, ex);
                    parser?.Dispose();
                    mergedResult = new(Options.LatestOnly);
                    break;
                }
            }
            return mergedResult;
        }

        internal void WriteResults(CoverageResult mergedResult)
        {
            if (_exitCode != 0)
            {
                return;
            }

            List<SourceFileInfo> sourceFiles = new(mergedResult.SourceFiles.Values);
            sourceFiles.Sort((x, y) => string.Compare(x.SourceFilePath, y.SourceFilePath));
            if (Options.CoverageThreshold < 100)
            {
                foreach (SourceFileInfo sourceFile in sourceFiles)
                {
                    foreach ((int firstLine, int lastLine) in sourceFile.LineSequences())
                    {
                        ref readonly LineInfo line = ref sourceFile.Line(firstLine);
                        if (line.Hits < Options.HitThreshold
                            || (line.TotalBranches >= Options.BranchThreshold
                                && line.TotalBranches != 0
                                && (int)Math.Round((double)line.CoveredBranches / line.TotalBranches * 100)
                                    < Options.CoverageThreshold))
                        {
                            WriteResultLine(sourceFile.SourceFilePath, firstLine, lastLine, line);
                        }
                    }
                }
            }
            else
            {
                foreach (SourceFileInfo sourceFile in sourceFiles)
                {
                    foreach ((int firstLine, int lastLine) in sourceFile.LineSequences())
                    {
                        ref readonly LineInfo line = ref sourceFile.Line(firstLine);
                        if (line.Hits < Options.HitThreshold
                            || (line.CoveredBranches < line.TotalBranches
                                && line.TotalBranches >= Options.BranchThreshold))
                        {
                            WriteResultLine(sourceFile.SourceFilePath, firstLine, lastLine, line);
                        }
                    }
                }
            }
            if (!_titlesWritten.HasFlag(TitlesWritten.ResultTitle))
            {
                WriteResultTitle();
                WriteLineDetailed("No missing coverage found.");
            }
        }

        // For navigable message format see:
        // https://docs.microsoft.com/en-us/cpp/build/formatting-the-output-of-a-custom-build-step-or-build-event?view=msvc-160
        internal void WriteResultLine(string fileName, int firstLine, int lastLine, LineInfo line)
        {
            WriteResultTitle();
            string warning;
            if (line.TotalBranches > 0)
            {
                int percent = (int)Math.Round((double)line.CoveredBranches / line.TotalBranches * 100);
                warning = $"): warning MC0001: Hits: {line.Hits} Branches: {percent}% ({line.CoveredBranches}/{line.TotalBranches})";
            }
            else
            {
                warning = $"): warning MC0002: Hits: {line.Hits}";
            }
            fileName += '(';
            if (Options.NoCollapse || firstLine == lastLine)
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    Out.WriteLine(fileName + i.ToString() + warning);
                }
            }
            else
            {
                Out.WriteLine(fileName + $"{firstLine}-{lastLine}" + warning);
            }
        }

        internal void WriteLineDetailed(string line)
        {
            if (Options.Verbosity < VerbosityLevel.Detailed) return;
            Out.WriteLine(line);
        }

        internal void WriteLineNormal(string line)
        {
            if (Options.Verbosity < VerbosityLevel.Normal) return;
            Out.WriteLine(line);
        }

        internal void WriteFileError(string filePath, Exception ex)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                WriteToolError(ex);
                return;
            }
            if (_exitCode == 0) _exitCode = 1;
            WriteAppTitle();
            int lineNumber = 0, linePosition = 0;
            string msgcode = "MC9002";
            if (ex is XmlException xex)
            {
                lineNumber = xex.LineNumber;
                linePosition = xex.LinePosition;
                msgcode = "MC9001";
            }
            Error.WriteLine($"{filePath}({lineNumber},{linePosition}): error {msgcode}: "
                + (Options.Verbosity == VerbosityLevel.Diagnostic ? ex.ToString() : ex.Message));
        }

        internal void WriteToolError(Exception ex)
        {
            if (_exitCode == 0) _exitCode = 1;
            WriteAppTitle();
            string msgcode = "MC9000";
            Error.WriteLine($"{Name} : error {msgcode}: "
                + (Options.Verbosity == VerbosityLevel.Diagnostic ? ex.ToString() : ex.Message));
        }

        internal void WriteAppTitle()
        {
            if (_titlesWritten.HasFlag(TitlesWritten.AppTitle)) return;
            _titlesWritten |= TitlesWritten.AppTitle;
            if (Options.NoLogo || Options.Verbosity == VerbosityLevel.Quiet) return;
            Out.WriteLine($"{Name} {Version} {Copyright}");
        }

        internal void WriteInputTitle()
        {
            if (_titlesWritten.HasFlag(TitlesWritten.InputTitle)) return;
            _titlesWritten |= TitlesWritten.InputTitle;
            WriteAppTitle();
            WriteLineNormal("Input files:");
        }

        internal void WriteResultTitle()
        {
            if (_titlesWritten.HasFlag(TitlesWritten.ResultTitle)) return;
            _titlesWritten |= TitlesWritten.ResultTitle;
            WriteAppTitle();
            WriteLineNormal("Results:");
        }

        internal void WriteHelpText()
        {
            WriteAppTitle();
            Out.WriteLine(@$"
Usage: {Name} [options] [filespecs]

Options:
  -h|--help                       Display this help.
  -ht|--hit-threshold <INT>       Lowest # of line hits to consider a line as covered, i.e. to not include it as missing coverage in report.
  -ct|--coverage-threshold <INT>  Lowest coverage in percent to consider a line with branches as covered.
  -bt|--branch-threshold <INT>    Minimum # of total branches a line must have before the coverage threshold gets applied.
  -lo|--latest-only               For each source file, uses only the data from the newest of all matching report files.
  --no-collapse                   Reports each line separately. By default, lines with identical information are reported as range.
  --max-linenumber <INT>          Sets the maximum line number allowed.
  --no-logo                       Supresses version and copyright information.
  -v|--verbosity <LEVEL>          Sets the verbosity level to q[uiet], m[inimal], n[ormal], d[etailed], or diag[nostic].
  --                              Indicates that any subsequent arguments are filespecs, even if starting with hyphen (-).

Filespecs:
  Any number of space separated file specs. Wildcards * ? ** are supported.
  Absolute or relative paths can be used. Relative paths are based on the current directory.

Default:
  {Name} --hit-threshold 1 --coverage-threshold 100 --branch-threshold 2 --max-linenumber 50000 --verbosity normal **\*cobertura*.xml

Examples:
  C:\MyProjects\**\*cobertura*.xml                     Process all xml files with name containing 'cobertura' recursively in all subdirectories of 'C:\MyProjects'.
  --latest-only TestResults\*\coverage.cobertura.xml   Process only the newest report in the randomly named subdirectories of 'TestResults' in the current directory.
  -hit-threshold 0                                     Report only lines with incomplete branch coverage, ignore lines that don't contain branches.
");
        }
    }
}
