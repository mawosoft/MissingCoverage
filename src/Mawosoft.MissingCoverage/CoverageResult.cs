// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mawosoft.MissingCoverage
{
    internal class SourceFileInfo
    {
        public string FileName { get; }
        public string? FullName { get; set; }
        public Dictionary<int, LineInfo> Lines { get; } = new();
        public SourceFileInfo(string fileName) => FileName = fileName;
    }

    internal class LineInfo
    {
        public int LineNumber;
        public int Hits;
        public bool Branch;
        public int CoveredConditions;
        public int TotalConditions;
        public LineInfo(int lineNumber, int hits, string conditionCoverage)
        {
            LineNumber = lineNumber;
            Hits = hits;
            if (!string.IsNullOrEmpty(conditionCoverage))
            {
                Match m = Regex.Match(conditionCoverage, @"\((\d+)/(\d+)\)", RegexOptions.CultureInvariant);
                if (m.Success)
                {
                    CoveredConditions = int.Parse(m.Groups[1].Value);
                    TotalConditions = int.Parse(m.Groups[2].Value);
                    Branch = true;
                }
            }

        }
    }

    internal class CoverageResult
    {
        public string InputFilePath { get; }
        public HashSet<string> SourceDirectories { get; } = new();
        public Dictionary<string, SourceFileInfo> SourceFiles { get; } = new();
        public CoverageResult(string inputFilePath) => InputFilePath = inputFilePath;
    }
}
