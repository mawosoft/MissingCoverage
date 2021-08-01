using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Mawosoft.MissingCoverage.Tests
{
    public class CoberturaParserTests
    {
        private readonly ITestOutputHelper _testOutput;
        public CoberturaParserTests(ITestOutputHelper testOutput) => _testOutput = testOutput;

        [Fact]
        public void Test1()
        {
            var c = new CoberturaParser();
        }
    }
}
