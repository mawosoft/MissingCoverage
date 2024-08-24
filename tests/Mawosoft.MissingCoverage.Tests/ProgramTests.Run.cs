// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;
using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests;

public partial class ProgramTests
{
    [Fact]
    public void Run_HelpArgument_WritesHelp()
    {
        using RedirectWrapper wrapper = new();
        int exitCode = wrapper.Program.Run(SplitArguments("--help"));
        Assert.Equal(0, exitCode);
        AssertAppTitle(wrapper);
        AssertOut(wrapper, string.Empty);
        AssertOut(wrapper, "Usage: MissingCoverage [options] [filespecs]");
        AssertOut(wrapper, string.Empty);
        AssertOut(wrapper, "Options:");
        Assert.DoesNotContain(wrapper.Lines, s => s.StartsWith("2>", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_InvalidArgument_WritesHelp()
    {
        using RedirectWrapper wrapper = new();
        int exitCode = wrapper.Program.Run(SplitArguments("--badarg"));
        Assert.Equal(1, exitCode);
        AssertAppTitle(wrapper);
        Assert.StartsWith("2>MissingCoverage : error MC9000: ", wrapper.Lines.FirstOrDefault(), StringComparison.Ordinal);
        wrapper.Lines.RemoveAt(0);
        AssertOut(wrapper, string.Empty);
        AssertOut(wrapper, "Usage: MissingCoverage [options] [filespecs]");
        AssertOut(wrapper, string.Empty);
        AssertOut(wrapper, "Options:");
        Assert.DoesNotContain(wrapper.Lines, s => s.StartsWith("2>", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_AnyException_WritesToolError()
    {
        using RedirectWrapper wrapper = new();
        int exitCode = wrapper.Program.Run(SplitArguments("-v:m thisglobdoesntmatch.anything"));
        Assert.Equal(1, exitCode);
        AssertAppTitle(wrapper);
        AssertError(wrapper, "MissingCoverage : error MC9000: No matching input files.");
        Assert.Empty(wrapper.Lines);
    }

    [Fact]
    public void Run_Succeeds()
    {
        using TempDirectory tempDirectory = new();
        List<string> files = CreateMinimalReportFiles(tempDirectory, 1);
        using RedirectWrapper wrapper = new();
        int exitCode = wrapper.Program.Run([files[0]]);
        Assert.Equal(0, exitCode);
        AssertAppTitle(wrapper);
        AssertInputTitle(wrapper);
        AssertOut(wrapper, files[0]);
        AssertResultTitle(wrapper);
        Assert.Contains("warning MC", wrapper.Lines.FirstOrDefault(), StringComparison.Ordinal);
        wrapper.Lines.RemoveAt(0);
        Assert.Empty(wrapper.Lines);
    }
}

public partial class ProgramTests_NoParallelTests
{
    [Theory]
    [InlineData("coverlet small.xml")]
    [InlineData("coverlet big.xml")]
    [InlineData("fcc merged.xml")]
    public void Main_WithTestFiles_Succeeds(string reportName)
    {
        TextWriter stdout = Console.Out;
        TextWriter stderr = Console.Error;
        string currentDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(TestDataDirectory.GetTestDataDirectory());
            using StringWriter output = new();
            using StringWriter error = new();
            Console.SetOut(output);
            Console.SetError(error);
            int exitCode = Program.Main([reportName]);
            Assert.Equal(0, exitCode);
            Assert.Equal(0, error.GetStringBuilder().Length);
            string approvedFile = Path.Combine(TestDataDirectory.GetTestDataDirectory(), "approved", reportName + ".txt");
            string[] actual = output.ToString().Split(Environment.NewLine)
                .SkipWhile(s => s != "Results:").Skip(1)
                .SkipLast(1) // output ends with NewLine, creating a final empty item.
                .ToArray();
            string[] expected = File.ReadAllLines(approvedFile)
                .SkipWhile(s => s != "Results:").Skip(1)
                .Select(s =>
                    s[..s.IndexOf('(', StringComparison.Ordinal)]
                     .Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar)
                    + s[s.IndexOf('(', StringComparison.Ordinal)..])
                .ToArray();
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal((i, expected[i]), (i, actual[i]));
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(currentDir);
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
    }
}
