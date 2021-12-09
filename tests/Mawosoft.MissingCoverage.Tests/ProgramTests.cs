// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Mawosoft.MissingCoverage.Tests
{
    public class ProgramTests
    {
        private readonly ITestOutputHelper _testOutput;
        public ProgramTests(ITestOutputHelper testOutput) => _testOutput = testOutput;

        private class RedirectWriter : TextWriter
        {
            private readonly ITestOutputHelper _testOutput;
            public RedirectWriter(ITestOutputHelper testOutput) => _testOutput = testOutput;
            public override Encoding Encoding => Encoding.Unicode;
            public override void Write(char value) => throw new NotImplementedException();
            public override void WriteLine(string? value) => _testOutput.WriteLine(value);
        }

        // TODO Add proper tests.
        // This is just for coverage and manual validation.
        [Theory(Skip = "Replace with proper tests")]
        //[InlineData("-h")]
        //[InlineData(null)]
        //[InlineData("-ht", "0", "{testdata}*")]
        //[InlineData("-ht", "0", "-bt", "4", "{testdata}*")]
        //[InlineData("{testdata}coverlet big.xml")]
        [InlineData("{testdata}coverlet small.xml")]
        //[InlineData("{testdata}fcc merged.xml")]
        //[InlineData("{testdata}coverlet*.xml")]
        //[InlineData("-lo", "{testdata}*")]
        public void Run_CmdlineArguments(params string[]? args)
        {
            if (args != null)
            {
                string path = TestFiles.GetTestDataDirectory();
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = args[i].Replace("{testdata}", path, StringComparison.Ordinal);
                }
            }
            RedirectWriter redirect = new(_testOutput);
            Program.Out = redirect;
            Program.Error = redirect;
            //Program program = new();
            //program.Run(args ?? Array.Empty<string>());
            Program.Main(args ?? Array.Empty<string>());
        }

    }
}
