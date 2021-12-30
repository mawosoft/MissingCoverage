// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.IO;
using System.Linq;
using Xunit;

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;
using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    public partial class ProgramTests
    {
        [Fact]
        public void Configure_StopsOnException_SetsShowHelpOnly()
        {
            RedirectWrapper wrapper = new();
            wrapper.Program.Configure(SplitArguments("--badarg"));
            Assert.True(wrapper.Program.Options.ShowHelpOnly);
            AssertAppTitle(wrapper);
            Assert.NotEmpty(wrapper.Lines);
            Assert.Empty(wrapper.Lines.Where(s => !s.StartsWith("2>")));
        }

        [Fact]
        public void Configure_AddsDefaultGlobPattern()
        {
            RedirectWrapper wrapper = new();
            wrapper.Program.Configure(SplitArguments("--nologo"));
            Assert.Equal(Path.Combine("**", "*cobertura*.xml"),
                         Assert.Single(wrapper.Program.Options.GlobPatterns));
            wrapper.Close();
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void Configure_AppliesMaxLineNumber()
        {
            int maxLineNumber = SourceFileInfo.MaxLineNumber;
            try
            {
                Assert.NotEqual(100_000, SourceFileInfo.MaxLineNumber);
                RedirectWrapper wrapper = new();
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
}
