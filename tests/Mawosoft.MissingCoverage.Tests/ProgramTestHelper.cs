// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage.Tests;

internal static class ProgramTestHelper
{

    // Wrapper will be closed and successfully asserted lines at the top will be removed.

    public static void AssertLines(RedirectWrapper wrapper, string prefix, string expectedMultiLine)
    {
        wrapper.Close();
        string[] expected = expectedMultiLine.Split(Environment.NewLine).Select(s => prefix + s).ToArray();
        Assert.Equal(expected, wrapper.Lines.Take(expected.Length));
        wrapper.Lines.RemoveRange(0, expected.Length);
    }

    public static void AssertOut(RedirectWrapper wrapper, string expectedMultiLine)
    {
        AssertLines(wrapper, "1>", expectedMultiLine);
    }

    public static void AssertError(RedirectWrapper wrapper, string expectedMultiLine)
    {
        AssertLines(wrapper, "2>", expectedMultiLine);
    }

    private static readonly Regex s_regexAppTitle = new(
        @"^1>MissingCoverage \d+\.\d+\.\d+.*? Copyright \(c\) \d\d\d\d(-\d\d\d\d)? Matthias Wolf, Mawosoft$",
        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

    public static void AssertAppTitle(RedirectWrapper wrapper)
    {
        wrapper.Close();
        if (wrapper.Program.Options.NoLogo || wrapper.Program.Options.Verbosity == VerbosityLevel.Quiet)
        {
            Assert.DoesNotMatch(s_regexAppTitle, wrapper.Lines.FirstOrDefault());
        }
        else
        {
            Assert.Matches(s_regexAppTitle, wrapper.Lines.FirstOrDefault());
            wrapper.Lines.RemoveAt(0);
        }
    }

    public static void AssertInputTitle(RedirectWrapper wrapper)
    {
        wrapper.Close();
        if (wrapper.Program.Options.Verbosity >= VerbosityLevel.Normal)
        {
            AssertOut(wrapper, "Input files:");
        }
        else
        {
            Assert.NotEqual("1>Input files:", wrapper.Lines.FirstOrDefault());
        }
    }

    public static void AssertResultTitle(RedirectWrapper wrapper)
    {
        wrapper.Close();
        if (wrapper.Program.Options.Verbosity >= VerbosityLevel.Normal)
        {
            AssertOut(wrapper, "Results:");
        }
        else
        {
            Assert.NotEqual("1>Results:", wrapper.Lines.FirstOrDefault());
        }
    }

    private static readonly Regex s_regexResultLine = new(
        @"^\((?'first'\d+)(-(?'last'\d+))?\): warning MC000(?'warn'[12]): Hits: (?'hits'\d+)( Branches: (?'percent'\d+)% \((?'covered'\d+)/(?'total'\d+)\))?$",
        RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

    public static void AssertResultLine(RedirectWrapper wrapper, string fileName, int firstLine, int lastLine,
                                        LineInfo lineInfo)
    {
        wrapper.Close();
        while (firstLine <= lastLine)
        {
            string line = wrapper.Lines.FirstOrDefault() ?? string.Empty;
            Assert.StartsWith("1>" + fileName, line, StringComparison.Ordinal);
            line = line[(fileName.Length + 2)..];
            Assert.Matches(s_regexResultLine, line);
            Match match = s_regexResultLine.Match(line);
            Assert.Equal(firstLine.ToString(), match.Groups["first"].Value);
            if (wrapper.Program.Options.NoCollapse || firstLine == lastLine)
            {
                Assert.Equal(string.Empty, match.Groups["last"].Value);
                firstLine++;
            }
            else
            {
                Assert.Equal(lastLine.ToString(), match.Groups["last"].Value);
                firstLine = lastLine + 1;
            }
            Assert.Equal(lineInfo.TotalBranches > 0 ? "1" : "2", match.Groups["warn"].Value);
            Assert.Equal(lineInfo.Hits.ToString(), match.Groups["hits"].Value);
            if (lineInfo.TotalBranches > 0)
            {
                Assert.Equal(lineInfo.CoveredBranches.ToString(), match.Groups["covered"].Value);
                Assert.Equal(lineInfo.TotalBranches.ToString(), match.Groups["total"].Value);
                int percent = (int)Math.Round((double)lineInfo.CoveredBranches / lineInfo.TotalBranches * 100);
                Assert.Equal(percent.ToString(), match.Groups["percent"].Value);
            }
            else
            {
                Assert.Equal(string.Empty, match.Groups["percent"].Value);
                Assert.Equal(string.Empty, match.Groups["covered"].Value);
                Assert.Equal(string.Empty, match.Groups["total"].Value);
            }
            wrapper.Lines.RemoveAt(0);
        }
    }

    public static List<string> CreateMinimalReportFiles(TempDirectory tempDirectory, int reportFileCount)
    {
        int fileId = 1;
        string? content = null;
        List<string> files = [];
        while (files.Count < reportFileCount)
        {
            for (int level1 = 1; level1 <= 2 && files.Count < reportFileCount; level1++)
            {
                for (int level2 = 1; level2 <= 2 && files.Count < reportFileCount; level2++)
                {
                    for (int i = 0; i < 2 && files.Count < reportFileCount; i++)
                    {
                        string filePath = Path.Combine(tempDirectory.FullPath, $"subdir{level1}", $"subdir{level2}", $"report{fileId++}.xml");
                        if (content == null)
                        {
                            tempDirectory.WriteFile(filePath, string.Empty); // ensure subdirs
                            new CoberturaReportBuilder(filePath).AddMinimalDefaults().Save();
                            content = tempDirectory.ReadFile(filePath);
                        }
                        tempDirectory.WriteFile(filePath, content);
                        files.Add(filePath);
                    }
                }
            }
        }
        return files;
    }
}
