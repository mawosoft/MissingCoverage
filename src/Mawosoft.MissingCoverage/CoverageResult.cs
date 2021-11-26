// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;

namespace Mawosoft.MissingCoverage
{
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
