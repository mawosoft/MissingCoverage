// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

// Based on:
// - Mawosoft.ImdbScrape.Http.Tests.MockCacheDirectory
// - https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/IO/TempDirectory.cs

using System;
using System.Diagnostics;
using System.IO;

namespace Mawosoft.MissingCoverage.Tests
{
    public class TempDirectory : IDisposable
    {
        public string FullPath { get; }
        public bool AutoDelete { get; set; }

        public TempDirectory()
        {
            FullPath = GetRandomTempDirectoryPath();
            AutoDelete = true;
            Directory.CreateDirectory(FullPath);
        }

        public TempDirectory(string path, bool autoDelete)
        {
            FullPath = Path.GetFullPath(path);
            AutoDelete = autoDelete;
            Directory.CreateDirectory(path);
        }

        ~TempDirectory()
        {
            if (AutoDelete) DeleteDirectory();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (AutoDelete) DeleteDirectory();
        }

        private static string GetRandomTempDirectoryPath()
        {
            string path;
            do
            {
                path = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
                // Directory.Exists would return false for an existing file.
            } while (new DirectoryInfo(path).Attributes != (FileAttributes)(-1));
            return path;
        }

        private void DeleteDirectory()
        {
            try { Directory.Delete(FullPath, recursive: true); }
            catch { /* Ignore exceptions on disposal paths */ }
        }

        public bool IsEmpty => !Directory.EnumerateFileSystemEntries(FullPath).GetEnumerator().MoveNext();
        public bool FileExists(string filePath) => File.Exists(Path.Combine(FullPath, filePath));

        public string ReadFile(string filePath)
        {
            return File.ReadAllText(Path.Combine(FullPath, filePath)); // No problem if filePath is already absolute
        }

        public void WriteFile(string filePath, string content)
        {
            string fullPath = Path.Combine(FullPath, filePath);
            string? dirPath = Path.GetDirectoryName(fullPath);
            Debug.Assert(!string.IsNullOrEmpty(dirPath), $"Path.GetDirectoryName({fullPath}) returned null/empty string.");
            if (!string.IsNullOrEmpty(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            File.WriteAllText(fullPath, content);
        }
    }
}
