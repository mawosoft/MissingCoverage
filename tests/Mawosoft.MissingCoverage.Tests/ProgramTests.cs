// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Xml;
using Xunit;
using Xunit.Abstractions;

using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    public partial class ProgramTests
    {
        [Fact]
        public void WriteResultLine_WritesTitles()
        {
            RedirectWrapper wrapper = new();
            string filePath = "somedir/somefile.cs";
            LineInfo line = new() { Hits = 0 };
            wrapper.Program.WriteResultLine(filePath, 1, 1, line);
            AssertAppTitle(wrapper);
            AssertResultTitle(wrapper);
            AssertResultLine(wrapper, filePath, 1, 1, line);
            Assert.Empty(wrapper.Lines);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 0, 2)]
        [InlineData(10, 2, 2)]
        [InlineData(111, 5, 6)]
        internal void WriteResultLine_Succeeds_HonorsNoCollapse(int hits,
                                                                ushort coveredBranches,
                                                                ushort totalBranches)
        {
            string filePath = "somedir/somefile.cs";
            foreach ((int firstLine, int lastLine) in new[] { (5, 5), (10, 12) })
            {
                foreach (bool noCollapse in new[] { true, false })
                {
                    LineInfo lineInfo = new()
                    {
                        Hits = hits,
                        CoveredBranches = coveredBranches,
                        TotalBranches = totalBranches
                    };
                    RedirectWrapper wrapper = new();
                    wrapper.Program.Options.Verbosity = VerbosityLevel.Quiet; // Suppress titles
                    wrapper.Program.Options.NoCollapse = noCollapse;
                    wrapper.Program.WriteResultLine(filePath, firstLine, lastLine, lineInfo);
                    AssertResultLine(wrapper, filePath, firstLine, lastLine, lineInfo);
                    Assert.Empty(wrapper.Lines);
                }
            }
        }

        [Fact]
        public void WriteLineNormal_WriteLineDetailed_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                string normal = "The normal line written.";
                string detailed = "The detailed line written.";
                wrapper.Program.WriteLineNormal(normal);
                wrapper.Program.WriteLineDetailed(detailed);
                switch (verbosity)
                {
                    case VerbosityLevel.Quiet or VerbosityLevel.Minimal:
                        wrapper.Close();
                        break;
                    case VerbosityLevel.Normal:
                        AssertOut(wrapper, normal);
                        break;
                    case VerbosityLevel.Detailed or VerbosityLevel.Diagnostic:
                        AssertOut(wrapper, normal + Environment.NewLine + detailed);
                        break;
                    default:
                        Assert.Fail($"Unexpected VerbosityLevel: {verbosity}");
                        break;
                };
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void WriteFileError_WritesStderr_WritesAppTitle_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                Exception ex = Assert.ThrowsAny<Exception>(
                    (Action)(static () => throw new InvalidOperationException("My message")));
                string filePath = "somedir/somefile.cs";
                wrapper.Program.WriteFileError(filePath, ex);
                AssertAppTitle(wrapper);
                string expected = filePath + "(0,0): error MC9002: ";
                expected += verbosity == VerbosityLevel.Diagnostic ? ex.ToString() : ex.Message;
                AssertError(wrapper, expected);
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void WriteFileError_WithXmlException_WritesLineInfo()
        {
            RedirectWrapper wrapper = new();
            Exception ex = Assert.Throws<XmlException>((Action)(static () => throw new XmlException(null, null, 7, 20)));
            string filePath = "somedir/somefile.cs";
            wrapper.Program.WriteFileError(filePath, ex);
            AssertAppTitle(wrapper);
            string expected = filePath + "(7,20): error MC9001: " + ex.Message;
            AssertError(wrapper, expected);
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void WriteFileError_WithoutFilePath_WritesToolError()
        {
            RedirectWrapper wrapper = new();
            Exception ex = Assert.Throws<XmlException>((Action)(static () => throw new XmlException(null, null, 7, 20)));
            wrapper.Program.WriteFileError(null!, ex);
            AssertAppTitle(wrapper);
            string expected = "MissingCoverage : error MC9000: " + ex.Message;
            AssertError(wrapper, expected);
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void WriteToolError_WritesStderr_WritesAppTitle_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                Exception ex = Assert.ThrowsAny<Exception>(
                    (Action)(static () => throw new InvalidOperationException("My message")));
                wrapper.Program.WriteToolError(ex);
                AssertAppTitle(wrapper);
                string expected = "MissingCoverage : error MC9000: ";
                expected += verbosity == VerbosityLevel.Diagnostic ? ex.ToString() : ex.Message;
                AssertError(wrapper, expected);
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void WriteAppTitle_WritesOnce_HonorsNoLogo()
        {
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.NoLogo = true;
            wrapper.Program.WriteAppTitle();
            wrapper.Program.WriteAppTitle();
            AssertAppTitle(wrapper);
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void WriteAppTitle_WritesOnce_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                wrapper.Program.WriteAppTitle();
                wrapper.Program.WriteAppTitle();
                AssertAppTitle(wrapper);
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void WriteInputTitle_WritesOnce_WritesAppTitle_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                wrapper.Program.WriteInputTitle();
                wrapper.Program.WriteInputTitle();
                AssertAppTitle(wrapper);
                AssertInputTitle(wrapper);
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void WriteResultTitle_WritesOnce_WritesAppTitle_HonorsVerbosity()
        {
            foreach (VerbosityLevel verbosity in EnumGetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                wrapper.Program.WriteResultTitle();
                wrapper.Program.WriteResultTitle();
                AssertAppTitle(wrapper);
                AssertResultTitle(wrapper);
                Assert.Empty(wrapper.Lines);
            }
        }
    }

    [Collection(nameof(NoParallelTests))]
    public partial class ProgramTests_NoParallelTests
    {
    }
}
