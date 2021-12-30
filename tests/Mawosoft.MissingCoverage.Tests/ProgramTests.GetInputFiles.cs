// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

using static Mawosoft.MissingCoverage.Tests.ProgramTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    public partial class ProgramTests
    {
        [Fact]
        public void GetInputFiles_MatchAll_Succeeds()
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
            wrapper.Program.Options.GlobPatterns.Add(Path.Combine(tempDirectory.FullPath, "**", "*"));
            IEnumerable<string> inputFiles = wrapper.Program.GetInputFiles();
            wrapper.Close();
            Assert.Equal(files.OrderBy(s => s), inputFiles.OrderBy(s => s));
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void GetInputFiles_RelativeGlobs_Succeeds()
        {
            string currentDir = Directory.GetCurrentDirectory();
            TempDirectory tempDirectory = new();
            try
            {
                Directory.SetCurrentDirectory(tempDirectory.FullPath);
                List<string> files = CreateMinimalReportFiles(tempDirectory, 7);
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
                wrapper.Program.Options.GlobPatterns.Add(Path.Combine("subdir1", "subdir1", "report2.xml"));
                wrapper.Program.Options.GlobPatterns.Add(files[0]); // Switch beetween relative/absolute
                wrapper.Program.Options.GlobPatterns.Add(Path.Combine("subdir1", "subdir2", "report*.xml"));
                wrapper.Program.Options.GlobPatterns.Add(Path.Combine("subdir2", "*", "*"));
                IEnumerable<string> inputFiles = wrapper.Program.GetInputFiles();
                wrapper.Close();
                Assert.Equal(files.OrderBy(s => s), inputFiles.OrderBy(s => s));
                Assert.Empty(wrapper.Lines);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDir);
                tempDirectory.Dispose();
            }
        }

        [Fact]
        public void GetInputFiles_RemovesDuplicates()
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
            wrapper.Program.Options.GlobPatterns.Add(Path.Combine(tempDirectory.FullPath, "**", "*"));
            wrapper.Program.Options.GlobPatterns.Add(files[0]);
            IEnumerable<string> inputFiles = wrapper.Program.GetInputFiles();
            wrapper.Close();
            Assert.Equal(files.OrderBy(s => s), inputFiles.OrderBy(s => s));
            Assert.Empty(wrapper.Lines);
        }

        [Fact]
        public void GetInputFiles_NoFiles_Throws_HonorsVerbosity()
        {
            using TempDirectory tempDirectory = new();
            foreach (VerbosityLevel verbosity in Enum.GetValues<VerbosityLevel>())
            {
                RedirectWrapper wrapper = new();
                wrapper.Program.Options.Verbosity = verbosity;
                wrapper.Program.Options.GlobPatterns.Add(Path.Combine(tempDirectory.FullPath, "*.*"));
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                    () => wrapper.Program.GetInputFiles());
                AssertAppTitle(wrapper);
                if (verbosity >= VerbosityLevel.Detailed)
                {
                    AssertOut(wrapper, "Working directory: " + Directory.GetCurrentDirectory());
                }
                Assert.Empty(wrapper.Lines);
            }
        }

        [Fact]
        public void GetInputFiles_AfterPreviousError_DoesNothing()
        {
            using TempDirectory tempDirectory = new();
            List<string> files = CreateMinimalReportFiles(tempDirectory, 3);
            RedirectWrapper wrapper = new();
            wrapper.Program.Options.Verbosity = VerbosityLevel.Diagnostic;
            wrapper.Program.Options.GlobPatterns.Add(Path.Combine(tempDirectory.FullPath, "**", "*"));
            wrapper.Program.WriteToolError(new Exception());
            int expectedLineCount = wrapper.CloneLines().Count;
            IEnumerable<string> inputFiles = wrapper.Program.GetInputFiles();
            wrapper.Close();
            Assert.Empty(inputFiles);
            Assert.Equal(expectedLineCount, wrapper.Lines.Count);
        }
    }
}
