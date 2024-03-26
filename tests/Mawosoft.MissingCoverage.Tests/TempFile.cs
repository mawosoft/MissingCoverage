// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

// Based on:
// - Mawosoft.ImdbScrape.Http.Tests.MockCacheDirectory
// - https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/IO/TempFile.cs

namespace Mawosoft.MissingCoverage.Tests;

#pragma warning disable CA1031 // Do not catch general exception types

public sealed class TempFile : IDisposable
{
    public string FullPath { get; }
    public bool AutoDelete { get; set; }

    public TempFile([CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        FullPath = GetRandomTempFilePath(memberName, lineNumber);
        AutoDelete = true;
        File.WriteAllBytes(FullPath, []);
    }

    public TempFile(string path, bool autoDelete)
    {
        FullPath = Path.GetFullPath(path);
        AutoDelete = autoDelete;
        File.WriteAllBytes(FullPath, []);
    }

    ~TempFile()
    {
        if (AutoDelete) DeleteFile();
    }

    public static TempFile Create(string content, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        TempFile file = new(GetRandomTempFilePath(memberName, lineNumber), true);
        file.WriteAllText(content);
        return file;
    }

    public static TempFile Create(byte[] content, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        TempFile file = new(GetRandomTempFilePath(memberName, lineNumber), true);
        File.WriteAllBytes(file.FullPath, content);
        return file;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (AutoDelete) DeleteFile();
    }

    private static string GetRandomTempFilePath(string memberName, int lineNumber)
    {
        string path;
        do
        {
            string file = $"{Path.GetRandomFileName()}_{memberName}_{lineNumber}";
            path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), file));
            // File.Exists would return false for an existing directory.
        } while (new FileInfo(path).Attributes != (FileAttributes)(-1));
        return path;
    }

    private void DeleteFile()
    {
        try { File.Delete(FullPath); }
        catch { /* Ignore exceptions on disposal paths */ }
    }

    public string ReadAllText() => File.ReadAllText(FullPath);
    public void WriteAllText(string content) => File.WriteAllText(FullPath, content);
}
