// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mawosoft.MissingCoverage
{
    internal struct SourceFileInfo
    {
        public string FilePath;
        public Dictionary<int, LineInfo> Lines;
        public SourceFileInfo(string fileName)
        {
            FilePath = fileName;
            Lines = new();
        }

        public SourceFileInfo(string fileName, Dictionary<int, LineInfo> lines)
        {
            FilePath = fileName;
            Lines = lines;
        }

        public void AddOrMergeLine(LineInfo line)
        {
            if (Lines.TryGetValue(line.LineNumber, out LineInfo existing))
            {
                existing.Merge(line);
                Lines[existing.LineNumber] = existing;
            }
            else
            {
                Lines.Add(line.LineNumber, line);
            }
        }

        public void Merge(SourceFileInfo other)
        {
            Debug.Assert(FilePath == other.FilePath, "FileName should be identical");
            foreach (LineInfo line in other.Lines.Values)
            {
                AddOrMergeLine(line);
            }
        }
    }

    internal struct LineInfo
    {
        public int LineNumber;
        public int Hits;
        public int CoveredBranches;
        public int TotalBranches;

        public void Merge(LineInfo other)
        {
            Debug.Assert(LineNumber == other.LineNumber, "LineNumber should be identical");
            if (LineNumber != other.LineNumber)
                return;
            Hits = Math.Max(Hits, other.Hits);
            if (TotalBranches == other.TotalBranches)
            {
                CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            }
            else
            {
                // This should rather not happen as it indicates that we merge reports from different source file versions.
                double covered = TotalBranches == 0 ? 0 : (double)CoveredBranches / TotalBranches;
                double otherCovered = other.TotalBranches == 0 ? 0 : (double)other.CoveredBranches / other.TotalBranches;
                if (otherCovered > covered)
                {
                    CoveredBranches = other.CoveredBranches;
                    TotalBranches = other.TotalBranches;
                }
            }
        }
    }

    internal class CoverageResult
    {
        public int HitThreshold { get; }
        public int CoverageThreshold { get; }
        public int BranchThreshold { get; }
        public List<string> InputFilePaths { get; } = new();
        public Dictionary<string, SourceFileInfo> SourceFiles { get; } = new();

        public CoverageResult(int hitThreshold, int coverageThreshold, int branchThreshold)
        {
            HitThreshold = hitThreshold;
            CoverageThreshold = coverageThreshold;
            BranchThreshold = branchThreshold;
        }

        public CoverageResult(int hitThreshold, int coverageThreshold, int branchThreshold, string inputFilePath)
        {
            HitThreshold = hitThreshold;
            CoverageThreshold = coverageThreshold;
            BranchThreshold = branchThreshold;
            if (!string.IsNullOrEmpty(inputFilePath))
            {
                InputFilePaths.Add(inputFilePath);
            }
        }

        public void AddOrMergeSourceFile(SourceFileInfo sourceFile)
        {

            if (SourceFiles.TryGetValue(sourceFile.FilePath, out SourceFileInfo existing))
            {
                existing.Merge(sourceFile);
                SourceFiles[existing.FilePath] = existing;
            }
            else
            {
                SourceFiles.Add(sourceFile.FilePath, sourceFile);
            }
        }

        // TODO Could merging lead to lines no longer matching the parameter set?
        // If so we have to verify the final merged CoverageResult and remove lines or even source files.
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
