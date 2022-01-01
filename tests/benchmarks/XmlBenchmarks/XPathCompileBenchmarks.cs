// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Xml.XPath;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace XmlBenchmarks
{
    public class XPathCompileBenchmarks
    {
        public IEnumerable<ParamWrapper<string>> Compile_Arguments() => new ParamWrapper<string>[]
        {
            new ("/coverage/packages/package/classes/class",
                 "/<path_to>/class"),
            new ("lines/line[@hits = '0' or (@condition-coverage and not(starts-with(@condition-coverage, '100%')))]",
                 "line['0' !'100%']"),
            new ("lines/line[number(@hits) < 1 or (number(substring-before(@condition-coverage, '%')) < 100 and number(substring-before(substring-after(@condition-coverage, '/'), ')')) >= 4)]",
                 "line[<1 <100 >=4]"),
        };

        [Benchmark]
        [ArgumentsSource(nameof(Compile_Arguments))]
        public XPathExpression Compile(ParamWrapper<string> xpath)
        {
            return XPathExpression.Compile(xpath.Value);
        }
    }
}
