// Copyright (c) 2021 Matthias Wolf, Mawosoft.

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
        public static TextWriter Out { get; set; } = Console.Out;
        public static TextWriter Error { get; set; } = Console.Error;
        public Options Options { get; set; } = new();
        public HashSet<string> InputFilePaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public CoverageResult? MergedResult { get; private set; }

        internal static void Main(string[] args)
        {
            Program program = new();
            program.Run(args);
        }

        internal void Run(string[] args)
        {
            try
            {
                ParseArguments(args);
            }
            catch (Exception ex)
            {
                WriteAppTitle();
                WriteToolError("", ex.Message);
                WriteHelpText();
                return;
            }
            if (Options.ShowHelpOnly)
            {
                WriteAppTitle();
                WriteHelpText();
                return;
            }
            try
            {
                WriteAppTitle();
                ProcessInputFiles();
                WriteResults();
            }
            catch (Exception ex)
            {
                WriteToolError("", ex.Message);
            }
        }

        // TODO implement newly added Options here: NoLogo, Verbosity, NoCollapse, MaxLineNumber
        internal void ParseArguments(string[] args)
        {
            Matcher? matcher = null;
            string lastRoot = string.Empty;
            Options.ParseCommandLineArguments(args);
            if (Options.GlobPatterns.Count == 0)
            {
                Options.GlobPatterns.Add(@"**\*cobertura*.xml");
            }
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
            if (InputFilePaths.Count == 0)
            {
                throw new InvalidOperationException("No matching input files.");
            }

            void ExecuteMatcher()
            {
                if (matcher != null)
                {
                    DirectoryInfo dirInfo = new(lastRoot.Length == 0 ? "." : lastRoot);
                    PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(dirInfo));
                    foreach (FilePatternMatch file in result.Files)
                    {
                        // Hashset will exclude dupes but may not preserve order.
                        InputFilePaths.Add(Path.GetFullPath(Path.Combine(dirInfo.FullName, file.Path)));
                    }
                    matcher = null;
                }
            }
        }

        internal void ProcessInputFiles()
        {
            Out.WriteLine("Input files:");
            MergedResult = new(Options.LatestOnly);
            foreach (string inputFile in InputFilePaths)
            {
                CoberturaParser? parser = null;
                try
                {
                    parser = new(inputFile);
                    CoverageResult result = parser.Parse();
                    parser.Dispose();
                    MergedResult.Merge(result);
                    Out.WriteLine(inputFile);
                }
                catch (Exception ex)
                {
                    int lineNumber = 0, linePosition = 0;
                    string msgcode = "MC9002";
                    if (ex is XmlException xex)
                    {
                        lineNumber = xex.LineNumber;
                        linePosition = xex.LinePosition;
                        msgcode = "MC9001";
                    }
                    Out.WriteLine($"{inputFile}({lineNumber},{linePosition}): error {msgcode}: {ex.Message}");
                    MergedResult = null;
                    parser?.Dispose();
                    break;
                }
            }
        }

        // For navigable message format see:
        // https://docs.microsoft.com/en-us/cpp/build/formatting-the-output-of-a-custom-build-step-or-build-event?view=msvc-160
        internal void WriteResults()
        {
            if (MergedResult == null)
                return;
            Out.WriteLine("Results:");
            List<SourceFileInfo> sourceFiles = new(MergedResult.SourceFiles.Values);
            sourceFiles.Sort((x, y) => string.Compare(x.SourceFilePath, y.SourceFilePath));
            foreach (SourceFileInfo sourceFile in sourceFiles)
            {
                string fileName = sourceFile.SourceFilePath;
                for (int lineNumber = 1; lineNumber <= sourceFile.LastLineNumber; lineNumber++)
                {
                    ref readonly LineInfo line = ref sourceFile.Line(lineNumber);
                    if (line.IsLine)
                    {
                        int percent = 0;
                        if (line.TotalBranches > 0)
                        {
                            percent = (int)Math.Round((double)line.CoveredBranches / line.TotalBranches * 100);
                        }
                        if (line.Hits < Options.HitThreshold
                            || (line.TotalBranches >= Options.BranchThreshold
                                && percent < Options.CoverageThreshold))
                        {
                            string msgcode = line.TotalBranches > 0 ? "MC0001" : "MC0002";
                            string condition = string.Empty;
                            if (line.TotalBranches > 0)
                            {
                                condition = $" Condition coverage: {percent}% ({line.CoveredBranches}/{line.TotalBranches})";
                            }
                            Out.WriteLine($"{fileName}({lineNumber}): warning {msgcode}: Hits: {line.Hits}{condition}");
                        }
                    }
                }
            }
        }

        internal static (string name, string version, string copyright) GetAppInfo()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName asmName = asm.GetName();
            string name = nameof(MissingCoverage);
            string version = (Attribute.GetCustomAttribute(asm, typeof(AssemblyInformationalVersionAttribute))
                as AssemblyInformationalVersionAttribute)?.InformationalVersion ?? string.Empty;
            int pos = version.IndexOf('+');
            if (pos >= 0) version = version.Substring(0, pos);
            string copyright = (Attribute.GetCustomAttribute(asm, typeof(AssemblyCopyrightAttribute))
                as AssemblyCopyrightAttribute)?.Copyright ?? string.Empty;
            return (name, version, copyright);
        }

        internal static void WriteToolError(string msgcode, string message)
        {
            (string name, _, _) = GetAppInfo();
            if (string.IsNullOrEmpty(msgcode)) msgcode = "MC9000";
            Out.WriteLine($"{name} : error {msgcode}: {message}");
        }

        internal void WriteAppTitle()
        {
            if (!Options.NoLogo)
            {
                (string name, string version, string copyright) = GetAppInfo();
                Out.WriteLine($"{name} {version} {copyright}");
            }
        }

        internal static void WriteHelpText()
        {
            (string name, _, _) = GetAppInfo();
            Out.WriteLine(@$"
Usage: {name} [options] [filespecs]

Options:
  -h|--help                            Display this help.
  -ht|--hit-threshold <INTEGER>        Lowest # of line hits to consider a line as covered, i.e. to not include it as missing coverage in report.
  -ct|--coverage-threshold <INTEGER>   Lowest coverage in percent to consider a line with branches as covered.
  -bt|--branch-threshold <INTEGER>     Minimum # of total branches a line must have before the coverage threshold gets applied.
  -lo|--latest-only                    For each source file, uses only the data from the newest of all matching report files.
  --                                   Indicates that any subsequent arguments are filespecs, even if starting with hyphen (-).

Filespecs:
  Any number of space separated file specs. Wildcards * ? ** are supported.
  Absolute or relative paths can be used. Relative paths are based on the current directory.

Default:
  {name} --hit-threshold 1 --coverage-threshold 100 --branch-threshold 2 **\*cobertura*.xml

Examples:
  C:\MyProjects\**\*cobertura*.xml                     Process all xml files with name containing 'cobertura' recursively in all subdirectories of 'C:\MyProjects'.
  --latest-only TestResults\*\coverage.cobertura.xml   Process only the newest report in the randomly named subdirectories of 'TestResults' in the current directory.
  -hit-threshold 0                                     Report only lines with incomplete branch coverage, ignore lines that don't contain branches.
");
        }
    }
}
