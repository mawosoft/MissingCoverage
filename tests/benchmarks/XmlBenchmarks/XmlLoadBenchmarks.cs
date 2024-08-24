// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BenchmarkDotNet.Attributes;

namespace XmlBenchmarks
{
    // Note: Loading from file doesn't make much sense as it will get cached anyway.
    public class XmlLoadBenchmarks
    {
        public IEnumerable<FileBytesWrapper> FileBytes_Arguments() => TestFiles.GetWrappedFileBytes();

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XPathDocument XPathDocument_Load(FileBytesWrapper file)
        {
            // Intentionally no disposal of MemoryStream inside benchmark.
            return new XPathDocument(new MemoryStream(file.Value));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XDocument XDocument_Load(FileBytesWrapper file)
        {
            // XDocument.Load and XElement.Load differ in some inial processing, but bulk of impl is in XContainer.
            // Seems to have same performance and memory usage as XPathDocument
            XDocument doc = XDocument.Load(new MemoryStream(file.Value));
            return doc;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XElement XElement_Load(FileBytesWrapper file)
        {
            XElement element = XElement.Load(new MemoryStream(file.Value));
            return element;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XmlReader XmlReader_ReadAll(FileBytesWrapper file)
        {
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
            XmlReader reader = XmlReader.Create(new MemoryStream(file.Value), settings);
            while (reader.Read()) { }
            return reader;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XmlDocument XmlDocument_Load(FileBytesWrapper file)
        {
            XmlDocument doc = new();
            doc.Load(new MemoryStream(file.Value));
            return doc;
        }
    }
}
