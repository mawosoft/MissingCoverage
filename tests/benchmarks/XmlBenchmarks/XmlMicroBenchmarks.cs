// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using BenchmarkDotNet.Attributes;

namespace XmlBenchmarks
{
    public class XmlMicroBenchmarks1
    {
        public IEnumerable<FileBytesWrapper> FileBytes_Arguments() => TestFiles.GetWrappedFileBytes();

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public XmlReader XmlReader_Create(FileBytesWrapper file)
        {
            // We can't pass an XmlReader as argument. Again due to BDN argument caching and reuse.
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
            XmlReader reader = XmlReader.Create(new MemoryStream(file.Value), settings);
            return reader;
        }
    }

    // Separated because this is in ns range while the other is in µs range.
    public class XmlMicroBenchmarks2
    {
        public IEnumerable<FileBytesWrapper> FileBytes_Arguments() => TestFiles.GetWrappedFileBytes();

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public MemoryStream MemoryStream_New(FileBytesWrapper file)
        {
            return new MemoryStream(file.Value);
        }
    }
}
