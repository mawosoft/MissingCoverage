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
        public int CoveredConditions;
        public int TotalConditions;

        public void Merge(LineInfo other)
        {
            Debug.Assert(LineNumber == other.LineNumber, "LineNumber should be identical");
            if (LineNumber != other.LineNumber)
                return;
            Hits = Math.Max(Hits, other.Hits);
            if (TotalConditions == other.TotalConditions)
            {
                CoveredConditions = Math.Max(CoveredConditions, other.CoveredConditions);
            }
            else
            {
                double covered = TotalConditions == 0 ? 0 : (double)CoveredConditions / TotalConditions;
                double otherCovered = other.TotalConditions == 0 ? 0 : (double)other.CoveredConditions / other.TotalConditions;
                if (otherCovered > covered)
                {
                    CoveredConditions = other.CoveredConditions;
                    TotalConditions = other.TotalConditions;
                }
            }
        }
    }

    internal class CoverageResult
    {
        public int HitThreshold { get; }
        public int ConditionThreshold { get; }
        public List<string> InputFilePaths { get; } = new();
        public Dictionary<string, SourceFileInfo> SourceFiles { get; } = new();

        public CoverageResult(int hitThreshold, int conditionThreshold)
        {
            HitThreshold = hitThreshold;
            ConditionThreshold = conditionThreshold;
        }

        public CoverageResult(int hitThreshold, int conditionThreshold, string inputFilePath)
        {
            HitThreshold = hitThreshold;
            ConditionThreshold = conditionThreshold;
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
