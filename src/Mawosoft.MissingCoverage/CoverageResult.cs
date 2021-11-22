// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mawosoft.MissingCoverage
{
    internal class SourceFileInfo
    {
        public static int DefaultLineCount { get; set; } = 500;
        public static int MaxLineNumber { get; set; } = 50_000;
        public string SourceFilePath { get; set; }
        public DateTime ReportTimestamp { get; }
        public int LastLineNumber { get; private set; }

        private LineInfo[] _lines;
        public ref LineInfo this[int index] => ref _lines[index];

        public SourceFileInfo(string sourceFilePath, DateTime reportTimestamp)
        {
            SourceFilePath = sourceFilePath;
            ReportTimestamp = reportTimestamp;
            LastLineNumber = 0;
            _lines = new LineInfo[DefaultLineCount];
        }

        public void AddOrMergeLine(int lineNumber, LineInfo line)
        {
            if (lineNumber <= LastLineNumber)
            {
                if (_lines[lineNumber].IsLine)
                {
                    _lines[lineNumber].Merge(line);
                }
                else
                {
                    _lines[lineNumber] = line;
                }
            }
            else
            {
                if (lineNumber > MaxLineNumber)
                {
                    throw new IndexOutOfRangeException($"{nameof(lineNumber)} larger than {MaxLineNumber}.");
                }
                LastLineNumber = lineNumber;
                if (lineNumber >= _lines.Length)
                {
                    GrowLines(lineNumber);
                }
                _lines[lineNumber] = line;
            }
        }

        public void Merge(SourceFileInfo other)
        {
            Debug.Assert(SourceFilePath == other.SourceFilePath, "SourceFilePath should be identical");
            if (other.LastLineNumber >= _lines.Length)
            {
                Array.Resize(ref _lines, other.LastLineNumber + 1);
            }
            for (int i = 1; i <= other.LastLineNumber; i++)
            {
                if (other._lines[i].IsLine) AddOrMergeLine(i, other._lines[i]);
            }
        }

        private void GrowLines(int minLineNumber)
        {
            int minCapacity = minLineNumber + 1;
            if (minCapacity <= _lines.Length) return;
            int newCapacity = _lines.Length + DefaultLineCount;
            // net60 has Array.MaxLength which is lower than int.MaxValue.
            if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
            if (newCapacity < minCapacity) newCapacity = minCapacity;
            Array.Resize(ref _lines, newCapacity);
        }
    }

    internal struct LineInfo
    {
        private const uint HitsMask = int.MaxValue;
        private const uint LineFlag = ~HitsMask;

        private uint _hits;
        public bool IsLine => (_hits & LineFlag) != 0;
        public int Hits
        {
            get => (int)(_hits & HitsMask);
            set => _hits = (value < 0 ? 0 : (uint)value) | LineFlag;
        }
        public ushort CoveredBranches;
        public ushort TotalBranches;

        public void Merge(LineInfo other)
        {
            if (!IsLine)
            {
                this = other;
                return;
            }
            // We don't need to remove flags for this comparison
            if (_hits < other._hits) _hits = other._hits;
            if (TotalBranches == other.TotalBranches)
            {
                if (CoveredBranches < other.CoveredBranches) CoveredBranches = other.CoveredBranches;
            }
            else if (other.TotalBranches != 0)
            {
                // This should rather not happen as it indicates that we merge reports from different
                // source file versions.
                double covered = TotalBranches <= 1
                               ? 0
                               : (double)CoveredBranches / TotalBranches;
                double otherCovered = other.TotalBranches <= 1
                                    ? 0
                                    : (double)other.CoveredBranches / other.TotalBranches;
                if (covered < otherCovered)
                {
                    CoveredBranches = other.CoveredBranches;
                    TotalBranches = other.TotalBranches;
                }
            }
        }
    }

    internal class CoverageResult
    {
        public bool LatestOnly { get; }
        public List<string> InputFilePaths { get; } = new();
        public Dictionary<string, SourceFileInfo> SourceFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public CoverageResult(bool latestOnly)
        {
            LatestOnly = latestOnly;
        }

        public CoverageResult(string inputFilePath)
        {
            if (!string.IsNullOrEmpty(inputFilePath))
            {
                InputFilePaths.Add(inputFilePath);
            }
        }

        public void AddOrMergeSourceFile(SourceFileInfo sourceFile)
        {

            if (SourceFiles.TryGetValue(sourceFile.SourceFilePath, out SourceFileInfo? existing))
            {
                if (LatestOnly)
                {
                    if (sourceFile.ReportTimestamp > existing.ReportTimestamp)
                    {
                        SourceFiles[existing.SourceFilePath] = sourceFile;
                    }
                }
                else
                {
                    existing.Merge(sourceFile);
                }
            }
            else
            {
                SourceFiles.Add(sourceFile.SourceFilePath, sourceFile);
            }
        }

        public void Merge(CoverageResult other)
        {
            InputFilePaths.AddRange(other.InputFilePaths);
            foreach (SourceFileInfo fileInfo in other.SourceFiles.Values)
            {
                AddOrMergeSourceFile(fileInfo);
            }
        }
    }
}
