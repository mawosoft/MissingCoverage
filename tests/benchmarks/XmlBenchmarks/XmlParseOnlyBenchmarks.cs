// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BenchmarkDotNet.Attributes;

namespace XmlBenchmarks
{
    [ThreadingDiagnoser]
    public class XmlParseOnlyBenchmarks
    {
        public IEnumerable<FileBytesWrapper> FileBytes_Arguments() => TestFiles.GetWrappedFileBytes();

        public IEnumerable<FileParamWrapper<XPathDocument>> XPathDocument_Arguments()
        {
            foreach (FileBytesWrapper file in FileBytes_Arguments())
            {
                yield return new FileParamWrapper<XPathDocument>(
                    new XPathDocument(new MemoryStream(file.Value)), file.ToString()!);
            }
        }

        // For XDocument vs XElement, see
        // https://docs.microsoft.com/en-us/dotnet/standard/linq/query-xdocument-vs-query-xelement
        // TL;DR: root of XDocument is a document node with one XElement child,
        // the root node <root><child1/><child2/></root>
        public IEnumerable<FileParamWrapper<XElement>> XElement_Arguments()
        {
            foreach (FileBytesWrapper file in FileBytes_Arguments())
            {
                yield return new FileParamWrapper<XElement>(XElement.Load(
                    new MemoryStream(file.Value)), file.ToString()!);
            }
        }

        public IEnumerable<FileParamWrapper<XmlDocument>> XmlDocument_Arguments()
        {
            foreach (FileBytesWrapper file in FileBytes_Arguments())
            {
                XmlDocument doc = new();
                doc.Load(new MemoryStream(file.Value));
                yield return new FileParamWrapper<XmlDocument>(doc, file.ToString()!);
            }
        }

        private static readonly string s_xpathClassesStr = "/coverage/packages/package/classes/class";
        private static readonly string s_xpathLinesStr = "lines/line[@hits = '0' or (@condition-coverage and not(starts-with(@condition-coverage, '100%')))]";
        private static readonly XPathExpression s_xpathClassesExpr = XPathExpression.Compile(s_xpathClassesStr);
        private static readonly XPathExpression s_xpathLinesExpr = XPathExpression.Compile(s_xpathLinesStr);

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(XPathDocument_Arguments))]
        public int XPathDocument_XPath_LinesPerClass(FileParamWrapper<XPathDocument> file)
        {
            XPathNavigator navi = file.Value.CreateNavigator();
            XPathNodeIterator classes = navi.Select(s_xpathClassesExpr);
            int sum = 0;
            foreach (XPathNavigator @class in classes)
            {
                string fileName = @class.GetAttribute("filename", "");
                XPathNodeIterator lines = @class.Select(s_xpathLinesExpr);
                foreach (XPathNavigator line in lines)
                {
                    string coverage = line.GetAttribute("condition-coverage", "");
                    string number = line.GetAttribute("number", "");
                    string hits = line.GetAttribute("hits", "");
                    sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(XElement_Arguments))]
        public int XElement_Linq_LinesPerClass(FileParamWrapper<XElement> file)
        {
            int sum = 0;
            XElement element = file.Value;
            if (element.Name == "coverage")
            {
                foreach (XElement @class
                    in element.Elements("packages").Elements("package").Elements("classes").Elements("class"))
                {
                    string fileName = @class.Attribute("filename")?.Value ?? string.Empty;
                    IEnumerable<XElement> lines =
                        from l in @class.Elements("lines").Elements("line")
                        where l.Attribute("hits")?.Value == "0"
                              || (l.Attribute("condition-coverage") is XAttribute attr
                                  && !attr.Value.StartsWith("100%", StringComparison.Ordinal))
                        select l;
                    foreach (XElement line in lines)
                    {
                        string coverage = line.Attribute("condition-coverage")?.Value ?? string.Empty;
                        string number = line.Attribute("number")?.Value ?? string.Empty;
                        string hits = line.Attribute("hits")?.Value ?? string.Empty;
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(XElement_Arguments))]
        public int XElement_Linq_LinesPerClass_Parallel(FileParamWrapper<XElement> file)
        {
            int sum = 0;
            XElement element = file.Value;
            if (element.Name == "coverage")
            {
                ParallelQuery<XElement> query =
                    from XElement @class
                    in element.Elements("packages").Elements("package").Elements("classes").Elements("class").AsParallel()
                    select @class;
                query.ForAll(@class =>
                {
                    string fileName = @class.Attribute("filename")?.Value ?? string.Empty;
                    IEnumerable<XElement> lines =
                        from l in @class.Elements("lines").Elements("line")
                        where l.Attribute("hits")?.Value == "0"
                              || (l.Attribute("condition-coverage") is XAttribute attr
                                  && !attr.Value.StartsWith("100%", StringComparison.Ordinal))
                        select l;
                    foreach (XElement line in lines)
                    {
                        string coverage = line.Attribute("condition-coverage")?.Value ?? string.Empty;
                        string number = line.Attribute("number")?.Value ?? string.Empty;
                        string hits = line.Attribute("hits")?.Value ?? string.Empty;
                        Interlocked.Add(ref sum, fileName.Length + coverage.Length + number.Length + hits.Length);
                    }
                });
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(XElement_Arguments))]
        public int XElement_Linq_LinesParentLookup(FileParamWrapper<XElement> file)
        {
            int sum = 0;
            XElement element = file.Value;
            if (element.Name == "coverage")
            {
                IEnumerable<XElement> lines =
                    from l in element.Elements("packages").Elements("package")
                                     .Elements("classes").Elements("class")
                                     .Elements("lines").Elements("line")
                    where l.Attribute("hits")?.Value == "0"
                          || (l.Attribute("condition-coverage") is XAttribute attr
                              && !attr.Value.StartsWith("100%", StringComparison.Ordinal))
                    select l;
                XElement? lastParent = null;
                string fileName = string.Empty;
                foreach (XElement line in lines)
                {
                    XElement parent = line.Parent!;
                    if (parent != lastParent)
                    {
                        lastParent = parent;
                        fileName = parent.Parent!.Attribute("filename")?.Value ?? string.Empty;
                    }
                    string coverage = line.Attribute("condition-coverage")?.Value ?? string.Empty;
                    string number = line.Attribute("number")?.Value ?? string.Empty;
                    string hits = line.Attribute("hits")?.Value ?? string.Empty;
                    sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(XmlDocument_Arguments))]
        public int XmlDocument_XPathStr_LinesPerClass(FileParamWrapper<XmlDocument> file)
        {
            // SelectNodes may return null on certain node types like XmlDeclaration, DocumentType, etc.
            XmlNodeList classes = file.Value.SelectNodes(s_xpathClassesStr)!;
            int sum = 0;
            foreach (XmlElement @class in classes)
            {
                string fileName = @class.GetAttribute("filename", "");
                XmlNodeList lines = @class.SelectNodes(s_xpathLinesStr)!;
                foreach (XmlElement line in lines)
                {
                    string coverage = line.GetAttribute("condition-coverage", "");
                    string number = line.GetAttribute("number", "");
                    string hits = line.GetAttribute("hits", "");
                    sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                }
            }
            return sum;
        }
    }
}
