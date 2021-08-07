// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Mawosoft.MissingCoverage
{
    internal class Program
    {
        public TextWriter Out { get; set; } = Console.Out;
        public TextWriter Error { get; set; } = Console.Error;
        public int HitThreshold { get; set; } = 1;
        public int ConditionThreshold { get; set; } = 100;
        public bool LatestOnly { get; set; }
        public bool ShowHelpOnly { get; set; }
        public List<string> InputFilePaths { get; } = new();
        public CoverageResult? MergedResult { get; private set; }

        internal Program() { }

        internal static void Main(string[] args)
        {
            Program program = new();
            program.Run(args);
        }

        internal void Run(string[] args)
        {
            WriteProgramTitle();
            try
            {
                ParseArguments(args);
            }
            catch (Exception ex)
            {
                Out.WriteLine(ex.Message);
                WriteHelpText();
                return;
            }
            if (ShowHelpOnly)
            {
                WriteHelpText();
                return;
            }
            try
            {
                FilterInputFiles();
                ProcessInputFiles();
                WriteResults();
            }
            catch (Exception ex)
            {
                Out.WriteLine(ex.Message);
            }
        }

        internal void ParseArguments(string[] args)
        {
            Matcher? matcher = null;
            string lastRoot = string.Empty;
            bool canHaveOptions = true;
            bool hasFileSpec = false;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (canHaveOptions && arg.StartsWith('-'))
                {
                    string? nextArg = (i + 1) < args.Length ? args[i + 1] : null;
                    switch (arg.ToLowerInvariant())
                    {
                        case "--":
                            canHaveOptions = false;
                            break;
                        case "-h":
                        case "--help":
                            ShowHelpOnly = true;
                            return; // Ignore remaining args
                        case "-lo":
                        case "--latest-only":
                            LatestOnly = true;
                            break;
                        case "-ht" or "--hit-threshold" when int.TryParse(nextArg, out int result) && result >= 0:
                            HitThreshold = result;
                            i++;
                            break;
                        case "-ct" or "--condition-threshold" when int.TryParse(nextArg, out int result) && result >= 0:
                            ConditionThreshold = result;
                            i++;
                            break;
                        default:
                            throw new ArgumentException($"Invalid command line argument: {arg}");
                    }

                }
                else
                {
                    hasFileSpec = true;
                    string root = Path.GetPathRoot(arg) ?? string.Empty;
                    if (root != lastRoot)
                    {
                        ExecuteMatcher();
                        matcher = null;
                        lastRoot = root;
                    }
                    matcher ??= new();
                    matcher.AddInclude(root.Length == 0 ? arg : Path.GetRelativePath(root, arg));
                }
            }
            if (!hasFileSpec)
            {
                matcher ??= new();
                matcher.AddInclude(@"**\*cobertura*.xml");
            }
            ExecuteMatcher(); // Handle any remains
            if (InputFilePaths.Count == 0)
            {
                throw new InvalidOperationException("No matching input files.");
            }

            void ExecuteMatcher()
            {
                if (matcher == null) return;
                DirectoryInfo dirInfo = new(lastRoot.Length == 0 ? "." : lastRoot);
                PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(dirInfo));
                foreach (FilePatternMatch file in result.Files)
                {
                    InputFilePaths.Add(Path.GetFullPath(Path.Combine(dirInfo.FullName, file.Path)));
                }
            }
        }

        internal void FilterInputFiles()
        {
            if (!LatestOnly || InputFilePaths.Count <= 1)
            {
                return;
            }
            Dictionary<string, (DateTime lastModified, string inputFilePath)> fileNames = new(InputFilePaths.Count);
            foreach (string inputFilePath in InputFilePaths)
            {
                DateTime lastModified = File.GetLastWriteTime(inputFilePath);
                string fileName = Path.GetFileName(inputFilePath);
                if (fileNames.TryGetValue(fileName, out (DateTime lastModified, string inputFilePath) existing))
                {
                    if (lastModified > existing.lastModified)
                    {
                        fileNames[fileName] = (lastModified, inputFilePath);
                    }
                }
                else
                {
                    fileNames.Add(fileName, (lastModified, inputFilePath));
                }
            }
            if (fileNames.Count != InputFilePaths.Count)
            {
                InputFilePaths.Clear();
                foreach ((_, string inputFilePath) in fileNames.Values)
                {
                    InputFilePaths.Add(inputFilePath);
                }
            }
        }

        internal void ProcessInputFiles()
        {
            MergedResult = new(HitThreshold, ConditionThreshold);
            foreach (string inputFile in InputFilePaths)
            {
                Out.WriteLine($"Input file: {inputFile}");
                CoberturaParser parser = new(inputFile);
                CoverageResult result = parser.Parse(MergedResult.HitThreshold, MergedResult.ConditionThreshold, null);
                MergedResult.Merge(result);
            }
        }

        internal void WriteResults()
        {
            if (MergedResult == null)
                return;
            List<SourceFileInfo> sourceFiles = new(MergedResult.SourceFiles.Values);
            sourceFiles.Sort((x, y) => string.Compare(x.FilePath, y.FilePath));
            foreach (SourceFileInfo sourceFile in sourceFiles)
            {
                string fileName = sourceFile.FilePath;
                List<LineInfo> lines = new(sourceFile.Lines.Values);
                lines.Sort((x, y) => x.LineNumber - y.LineNumber);
                foreach (LineInfo line in lines)
                {
                    string msgcode = line.TotalConditions > 0 ? "MC0001" : "MC0002";
                    string condition = string.Empty;
                    if (line.TotalConditions > 0)
                    {
                        condition = $" Condition coverage: {Math.Round((double)line.CoveredConditions / line.TotalConditions * 100)}% ({line.CoveredConditions}/{line.TotalConditions})";
                    }
                    Out.WriteLine($"{fileName}({line.LineNumber}): warning {msgcode}: Hits: {line.Hits}{condition}");
                }
            }
        }

        internal void WriteProgramTitle()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName asmName = asm.GetName();
            object[] copyrights = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            AssemblyCopyrightAttribute? copyright = copyrights.Length == 1 ? copyrights[0] as AssemblyCopyrightAttribute : null;
            Out.WriteLine($"{asmName.Name} {asmName.Version} {copyright?.Copyright}");
        }

        internal void WriteHelpText()
        {
            Out.WriteLine(@"
Usage: MissingCoverage [options] [filespecs]

Default:
  MissingCoverage --hit-threshold 1 --condition-threshold 100 **\*cobertura*.xml

Options:
  -h|--help                            Display this help.
  -ht|--hit-threshold <INTEGER>        Lowest # of line hits to mark line as covered (i.e. don't report it).
  -ct|--condition-threshold <INTEGER>  Lowest percentage to mark a condition (branch) as covered.
  -lo|--latest-only                    Of multiple files with the same name in different directories,
                                       only the one modified latest will be used.
  --                                   Indicates that everything afterwards are filespecs, even if starting with -/--.

Filespecs:
  Any number of space separated file specs. Wildcards * ? ** are supported.
  Absolute or relative paths can be used. Relative paths are based on the current directory.

Examples:
  C:\MyProjects\**\*cobertura*.xml   Process all xml files with name containing 'cobertura' recursively in all
                                     subdirectories of 'C:\MyProjects'.
  --latest-only TestResults\*\coverage.cobertura.xml    Process only the newest report in the randomly named
                                     subdirectories of 'TestResults' in the current directory.
  -hit-threshold 0                   Report only lines with incomplete condition coverage, ignore lines that
                                     are not branches.
");
        }
    }
}
