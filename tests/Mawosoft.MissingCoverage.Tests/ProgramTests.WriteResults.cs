// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using Xunit;

using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    public partial class ProgramTests
    {
        [Fact]
        public void WriteResults_SortsFilesAlphabetical()
        {
            CoverageResult result = new();
            LineInfo line = new() { Hits = 0 };
            SourceFileInfo source = new("ghi", DateTime.MinValue);
            source.AddOrMergeLine(1, line);
            result.AddOrMergeSourceFile(source);
            source = new("abc", DateTime.MinValue);
            source.AddOrMergeLine(1, line);
            result.AddOrMergeSourceFile(source);
            source = new("def", DateTime.MinValue);
            source.AddOrMergeLine(1, line);
            result.AddOrMergeSourceFile(source);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Quiet; // Suppress titles
            wrapper.Program.WriteResults(result);
            AssertResultLine(wrapper, "abc", 1, 1, line);
            AssertResultLine(wrapper, "def", 1, 1, line);
            AssertResultLine(wrapper, "ghi", 1, 1, line);
        }

        [Theory]
        [InlineData(1, 100, 2, new int[] { 1, 4, 5, 6 })]
        [InlineData(2, 100, 2, new int[] { 1, 2, 4, 5, 6 })]
        [InlineData(0, 100, 2, new int[] { 4, 5, 6 })]
        [InlineData(1, 75, 2, new int[] { 1, 4, 6 })]
        [InlineData(1, 75, 4, new int[] { 1, 6 })]
        public void WriteResults_Succeeds_HonorsThresholds(int hitThreshold,
                                                           int coverageThreshold,
                                                           int branchThreshold,
                                                           int[] expected)
        {
            CoverageResult result = new();
            SourceFileInfo source = new("somedir/somefile.cs", DateTime.UtcNow);
            source.AddOrMergeLine(1, new LineInfo() { Hits = 0 });
            source.AddOrMergeLine(2, new LineInfo() { Hits = 1 });
            source.AddOrMergeLine(3, new LineInfo() { Hits = 2, TotalBranches = 2, CoveredBranches = 2 });
            source.AddOrMergeLine(4, new LineInfo() { Hits = 2, TotalBranches = 2, CoveredBranches = 1 });
            source.AddOrMergeLine(5, new LineInfo() { Hits = 10, TotalBranches = 4, CoveredBranches = 3 });
            source.AddOrMergeLine(6, new LineInfo() { Hits = 10, TotalBranches = 4, CoveredBranches = 2 });
            result.AddOrMergeSourceFile(source);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Quiet; // Suppress titles
            wrapper.Program.Options.HitThreshold = hitThreshold;
            wrapper.Program.Options.CoverageThreshold = coverageThreshold;
            wrapper.Program.Options.BranchThreshold = branchThreshold;
            wrapper.Program.WriteResults(result);
            for (int i = 0; i < expected.Length; i++)
            {
                int lineNumber = expected[i];
                AssertResultLine(wrapper, source.SourceFilePath, lineNumber, lineNumber, source.Line(lineNumber));
            }
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void WriteResults_NoMissingCoverage_HonorsVerbosity()
        {
            foreach (bool withSourceFile in new[] { true, false })
            {
                CoverageResult result = new();
                if (withSourceFile)
                {
                    SourceFileInfo source = new("somedir/somefile.cs", DateTime.UtcNow);
                    source.AddOrMergeLine(1, new LineInfo() { Hits = 1 });
                    result.AddOrMergeSourceFile(source);
                }
                foreach (VerbosityLevel verbosity in Enum.GetValues<VerbosityLevel>())
                {
                    RedirectWrapper wrapper = new();
                    wrapper.Program.Options.Verbosity = verbosity;
                    wrapper.Program.WriteResults(result);
                    AssertAppTitle(wrapper);
                    AssertResultTitle(wrapper);
                    if (verbosity >= VerbosityLevel.Detailed)
                    {
                        AssertOut(wrapper, "No missing coverage found.");
                    }
                    Assert.Empty(wrapper.Lines);
                }
            }
        }

        [Fact]
        public void WriteResults_AfterPreviousError_DoesNothing()
        {
            CoverageResult result = new();
            LineInfo line = new() { Hits = 0 };
            SourceFileInfo source = new("abc", DateTime.MinValue);
            source.AddOrMergeLine(1, line);
            result.AddOrMergeSourceFile(source);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
            wrapper.Program.WriteToolError(new Exception());
            int expectedLineCount = wrapper.CloneLines().Count;
            wrapper.Program.WriteResults(result);
            wrapper.Close();
            Assert.Equal(expectedLineCount, wrapper.Lines.Count);
        }
    }
}
