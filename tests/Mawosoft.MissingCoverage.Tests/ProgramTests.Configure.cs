// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;
using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests;

public partial class ProgramTests
{
    [Fact]
    public void Configure_StopsOnException_SetsShowHelpOnly()
    {
        using RedirectWrapper wrapper = new();
        wrapper.Program.Configure(SplitArguments("--badarg"));
        Assert.True(wrapper.Program.Options.ShowHelpOnly);
        AssertAppTitle(wrapper);
        Assert.NotEmpty(wrapper.Lines);
        Assert.DoesNotContain(wrapper.Lines, s => !s.StartsWith("2>", StringComparison.Ordinal));
    }

    [Fact]
    public void Configure_AddsDefaultGlobPattern()
    {
        using RedirectWrapper wrapper = new();
        wrapper.Program.Configure(SplitArguments("--nologo"));
        Assert.Equal(Path.Combine("**", "*cobertura*.xml"),
                     Assert.Single(wrapper.Program.Options.GlobPatterns));
        wrapper.Close();
        Assert.Empty(wrapper.Lines);
    }
}

public partial class ProgramTests_NoParallelTests
{
    [Fact]
    public void Configure_AppliesMaxLineNumber()
    {
        int maxLineNumber = SourceFileInfo.MaxLineNumber;
        try
        {
            Assert.NotEqual(100_000, SourceFileInfo.MaxLineNumber);
            using RedirectWrapper wrapper = new();
            wrapper.Program.Configure(SplitArguments("--nologo --max-linenumber 100000"));
            Assert.Equal(100_000, SourceFileInfo.MaxLineNumber);
            wrapper.Close();
            Assert.Empty(wrapper.Lines);
        }
        finally
        {
            SourceFileInfo.MaxLineNumber = maxLineNumber;
        }
    }
}
