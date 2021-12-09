// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mawosoft.MissingCoverage.Tests
{
    internal static class TestFiles
    {
        // Get 'testdata' directory
        internal static string GetTestDataDirectory()
        {
            static string WhereAmI([CallerFilePath] string callerFilePath = "") => callerFilePath;
            const string relativeToMePath = "../testdata/";
            string basePath = WhereAmI();
            if (basePath.StartsWith("/_", StringComparison.Ordinal))
            {
                // Resolve deterministic paths for GitHub (with local fallback)
                // TODO Make this generic CI, not GitHub-specific (also in XmlBenchmarks)
                int pos = basePath.IndexOf('/', 1);
                if (pos >= 0) pos++; // Substring will throw if pos < 0
                basePath = Path.GetDirectoryName(basePath.Substring(pos)) ?? string.Empty;
                string repoPath = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")
                                  ?? @"C:/Users/mw/Projects/MissingCoverage";
                basePath = Path.Combine(repoPath, basePath);
            }
            else
            {
                basePath = Path.GetDirectoryName(basePath) ?? string.Empty;
            }
            string path = Path.GetFullPath(Path.Combine(basePath, relativeToMePath));
            if (!Path.EndsInDirectorySeparator(path)) path += Path.DirectorySeparatorChar;
            return Directory.Exists(path)
                ? path
                : throw new DirectoryNotFoundException("Could not locate 'testdata' directory.");
        }

    }
}
