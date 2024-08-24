// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests;

public partial class ProgramTests
{
    [Fact]
    public void ProcessInputFiles_Succeeds_HonorsLatestOnly()
    {
        foreach (bool latestOnly in new[] { true, false })
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            using RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Quiet;
            wrapper.Program.Options.LatestOnly = latestOnly;
            CoverageResult result = wrapper.Program.ProcessInputFiles(files);
            wrapper.Close();
            Assert.Equal(latestOnly, result.LatestOnly);
            Assert.Equal(files, result.ReportFilePaths);
            Assert.Empty(wrapper.Lines);
        }
    }

    [Fact]
    public void ProcessInputFiles_Succeeds_HonorsVerbosity()
    {
        foreach (VerbosityLevel verbosity in Enum.GetValues<VerbosityLevel>())
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            using RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = verbosity;
            CoverageResult result = wrapper.Program.ProcessInputFiles(files);
            Assert.Equal(files, result.ReportFilePaths);
            AssertAppTitle(wrapper);
            AssertInputTitle(wrapper);
            if (verbosity >= VerbosityLevel.Normal)
            {
                foreach (string file in files)
                {
                    AssertOut(wrapper, file);
                }
            }
            Assert.Empty(wrapper.Lines);
        }
    }

    [Fact]
    public void ProcessInputFiles_StopsOnException_HonorsVerbosity()
    {
        foreach (VerbosityLevel verbosity in Enum.GetValues<VerbosityLevel>())
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            File.Delete(files[1]);
            using RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = verbosity;
            CoverageResult result = wrapper.Program.ProcessInputFiles(files);
            Assert.Empty(result.ReportFilePaths);
            AssertAppTitle(wrapper);
            AssertInputTitle(wrapper);
            if (verbosity >= VerbosityLevel.Normal)
            {
                AssertOut(wrapper, files[0]);
            }
            Assert.StartsWith("2>" + files[1] + "(0,0): error MC9002: ", wrapper.Lines.FirstOrDefault(), StringComparison.Ordinal);
            wrapper.Lines.RemoveAt(0);
            if (verbosity == VerbosityLevel.Diagnostic)
            {
                Assert.NotEmpty(wrapper.Lines);
                Assert.DoesNotContain(wrapper.Lines, s => !s.StartsWith("2>", StringComparison.Ordinal));
            }
            else
            {
                Assert.Empty(wrapper.Lines);
            }
        }
    }

    [Fact]
    public void ProcessInputFiles_AfterPreviousError_DoesNothing()
    {
        using TempDirectory tempDirectory = new();
        List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
        using RedirectWrapper wrapper = new();
        wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
        wrapper.Program.WriteToolError(new Exception());
        int expectedLineCount = wrapper.CloneLines().Count;
        CoverageResult result = wrapper.Program.ProcessInputFiles(files);
        wrapper.Close();
        Assert.Empty(result.ReportFilePaths);
        Assert.Equal(expectedLineCount, wrapper.Lines.Count);
    }
}
