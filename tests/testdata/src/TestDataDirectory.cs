// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

#pragma warning disable IDE0005 // Using directive is unnecessary.
using System.Runtime.CompilerServices;
#pragma warning restore IDE0005 // Using directive is unnecessary.

namespace System.IO;

internal static class TestDataDirectory
{
    // For more general processing, see:
    // https://github.com/dotnet/sourcelink/blob/main/src/Microsoft.Build.Tasks.Git/GitDataReader/GitRepository.cs
    // For example, ".git" can also be a file pointing to a directory.
    public static string? GetGitDirectory(string? fromPath = null)
    {
        if (fromPath == null)
        {
            fromPath = Directory.GetCurrentDirectory();
        }
        else
        {
            fromPath = Path.GetFullPath(fromPath);
            if (File.Exists(fromPath))
            {
                fromPath = Path.GetDirectoryName(fromPath);
            }
        }
        while (fromPath != null)
        {
            if (File.Exists(Path.Combine(fromPath, ".git", "HEAD")))
            {
                return fromPath;
            }
            fromPath = Path.GetDirectoryName(fromPath);
        }
        return null;
    }

    // Get repo directory via known CI environment variables
    // - GitHub Actions: https://docs.github.com/en/actions/learn-github-actions/environment-variables
    //   - GITHUB_ACTIONS (true) , CI (true)
    //   - GITHUB_WORKSPACE (directory)
    // - Azure DevOps: https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=azure-devops&tabs=yaml
    //   - TF_BUILD (True)
    //   - BUILD_SOURCESDIRECTORY (directory if single repo)
    // - AppVeyor: https://www.appveyor.com/docs/environment-variables/
    //   - APPVEYOR (True/true), CI (True/true)
    //   - APPVEYOR_BUILD_FOLDER (path to clone directory)
    public static string? GetCIDirectory()
    {
        string? path;
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null
            && (path = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")) != null
            && Directory.Exists(path))
        {
            return path;
        }
        if (Environment.GetEnvironmentVariable("TF_BUILD") != null
            && (path = Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY")) != null
            && Directory.Exists(path))
        {
            return path;
        }
        if (Environment.GetEnvironmentVariable("APPVEYOR") != null
            && (path = Environment.GetEnvironmentVariable("APPVEYOR_BUILD_FOLDER")) != null
            && Directory.Exists(path))
        {
            return path;
        }
        return null;
    }

    // Get 'testdata' directory
    public static string GetTestDataDirectory()
    {
        static string WhereAmI([CallerFilePath] string callerFilePath = "") => callerFilePath;
        const string relativeToMePath = "./..";
        string basePath = WhereAmI();
        if (basePath.StartsWith("/_", StringComparison.Ordinal))
        {
            // Resolve deterministic paths
            int pos = basePath.IndexOf('/', 1);
            if (pos >= 0) pos++; // Substring will throw if pos < 0
            basePath = Path.GetDirectoryName(basePath[pos..]) ?? string.Empty;
            string? rootPath = GetCIDirectory();
            rootPath ??= GetGitDirectory() ?? string.Empty;
            basePath = Path.Combine(rootPath, basePath);
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
