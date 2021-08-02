// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Mawosoft.MissingCoverage
{
    internal class Program
    {
        public TextWriter Out { get; set; } = Console.Out;
        public TextWriter Error { get; set; } = Console.Error;
        public List<string> InputFilePaths { get; } = new();
        public List<CoverageResult> Results { get; } = new();

        internal Program() { }

        internal static void Main(string[] args)
        {
            Program program = new();
            program.Run(args);
        }

        internal void Run(string[] args)
        {
            ParseArguments(args);
            ProcessInputFiles();
            MergeResults();
            OutputResults();
        }

        internal void MergeResults()
        {
            // TODO MergeResults, SortResults
        }

        internal void OutputResults()
        {
            // TODO after merge there is only one result
            foreach (CoverageResult result in Results)
            {
                foreach (KeyValuePair<string, SourceFileInfo> sourceFile in result.SourceFiles)
                {
                    string fileName = sourceFile.Value.FullName ?? sourceFile.Value.FileName;
                    foreach (KeyValuePair<int, LineInfo> line in sourceFile.Value.Lines)
                    {
                        string msgcode = "MC0001"; // TODO msgcode selection
                        string condition = string.Empty;
                        if (line.Value.Branch)
                        {
                            condition = $" Condition coverage: {Math.Round((double)line.Value.CoveredConditions / line.Value.TotalConditions * 100)}% ({line.Value.CoveredConditions}/{line.Value.TotalConditions})";
                        }
                        Out.WriteLine($"{fileName}({line.Value.LineNumber}): warning {msgcode}: Hits: {line.Value.Hits}{condition}");
                    }
                }
            }
        }

        internal void ProcessInputFiles()
        {
            foreach (string inputFile in InputFilePaths)
            {
                CoberturaParser parser = new(inputFile);
                CoverageResult result = parser.Parse();
                foreach (KeyValuePair<string, SourceFileInfo> sourceFile in result.SourceFiles)
                {
                    if (Path.IsPathFullyQualified(sourceFile.Value.FileName))
                    {
                        sourceFile.Value.FullName = sourceFile.Value.FileName;
                    }
                    else
                    {
                        foreach (string sourceDir in result.SourceDirectories)
                        {
                            string combined = Path.Combine(sourceDir, sourceFile.Value.FileName);
                            if (File.Exists(combined))
                            {
                                sourceFile.Value.FullName = combined;
                                break;
                            }
                        }
                    }
                }
                Results.Add(result);
            }
        }

        internal void ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                args = new[] { @"**\*cobertura*.xml" };
            }
            Matcher? matcher = null;
            string? lastRoot = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith('-'))
                {
                    // TODO argument processing
                }
                else
                {
                    string? root = Path.GetPathRoot(arg);
                    if (root != lastRoot)
                    {
                        ExecuteMatcher();
                        matcher = null;
                        lastRoot = root;
                    }
                    if (!string.IsNullOrEmpty(root))
                        arg = Path.GetRelativePath(root, arg);
                    if (matcher == null)
                        matcher = new();
                    matcher.AddInclude(arg);
                }
            }
            ExecuteMatcher(); // Handle any remains

            void ExecuteMatcher()
            {
                if (matcher == null) return;
                DirectoryInfo dirInfo = new(string.IsNullOrEmpty(lastRoot) ? "." : lastRoot);
                PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(dirInfo));
                foreach (FilePatternMatch file in result.Files)
                {
                    string fullPath = Path.Combine(dirInfo.FullName, file.Path);
                    // TODO verify file/dir exists?
                    InputFilePaths.Add(fullPath);
                }
            }
        }
    }
}
