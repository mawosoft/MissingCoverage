// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage;

internal sealed class CoverageResult
{
    public bool LatestOnly { get; }
    public List<string> ReportFilePaths { get; } = [];
    public Dictionary<string, SourceFileInfo> SourceFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    public CoverageResult() { }

    public CoverageResult(bool latestOnly) => LatestOnly = latestOnly;

    public CoverageResult(string reportFilePath)
    {
        if (string.IsNullOrWhiteSpace(reportFilePath))
        {
            throw new ArgumentException(null, nameof(reportFilePath));
        }
        ReportFilePaths.Add(reportFilePath);
    }

    public void AddOrMergeSourceFile(SourceFileInfo sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
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
        ReportFilePaths.AddRange(other.ReportFilePaths);
        foreach (SourceFileInfo fileInfo in other.SourceFiles.Values)
        {
            AddOrMergeSourceFile(fileInfo);
        }
    }
}
