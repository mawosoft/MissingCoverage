// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

// Based on:
// - Mawosoft.ImdbScrape.Http.Tests.MockCacheDirectory
// - https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/System/IO/TempFile.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mawosoft.MissingCoverage.Tests
{
    public class TempFile : IDisposable
    {
        public string FullPath { get; }
        public bool AutoDelete { get; set; }

        public TempFile([CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            FullPath = GetRandomTempFilePath(memberName, lineNumber);
            AutoDelete = true;
            File.WriteAllBytes(FullPath, Array.Empty<byte>());
        }

        public TempFile(string path, bool autoDelete)
        {
            FullPath = Path.GetFullPath(path);
            AutoDelete = autoDelete;
            File.WriteAllBytes(FullPath, Array.Empty<byte>());
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
}
