// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace XmlBenchmarks
{
    [ThreadingDiagnoser]
    public class XmlLoadAndParseBenchmarks : IValidator
    {
        private readonly XmlParseOnlyBenchmarks _parser = new();

        bool IValidator.TreatsWarningsAsErrors => false;

        // While BDN has a ReturnValueValidator, it doesn't seem to work with arguments
        IEnumerable<ValidationError> IValidator.Validate(ValidationParameters validationParameters)
        {
            IEnumerable<BenchmarkCase> benchmarks =
                validationParameters.Benchmarks.Where(b => b.Descriptor.Type == typeof(XmlLoadAndParseBenchmarks));
            if (!benchmarks.Any())
            {
                return Enumerable.Empty<ValidationError>();
            }
            List<ValidationError> _validationErrors = new();
            foreach (IGrouping<string, BenchmarkCase> benchmarkGroup
                in benchmarks.GroupBy(b => b.Parameters.FolderInfo))
            {
                List<(int retVal, string displayInfo)> results = new();
                foreach (BenchmarkCase benchmark in benchmarkGroup)
                {
                    string displayInfo = benchmark.Descriptor.WorkloadMethod.Name
                                         + benchmark.Parameters.DisplayInfo;
                    if (benchmark.Descriptor.WorkloadMethod.ReturnType != typeof(int))
                    {
                        // BDN doesn't automatically print benchmark info even when included in
                        // ValidationError.
                        _validationErrors.Add(new ValidationError(false,
                            "Return type is not 'int': " + displayInfo, benchmark));
                    }
                    else
                    {
                        try
                        {
                            object? retVal = benchmark.Descriptor.WorkloadMethod.Invoke(this,
                                benchmark.Parameters.Items.Where(p => p.IsArgument)
                                    .Select(p => p.Value).ToArray());
                            if (retVal is not int intVal)
                            {
                                _validationErrors.Add(new ValidationError(true,
                                    "Method's return type is 'int' but returned value is not: "
                                    + displayInfo, benchmark));
                            }
                            else
                            {
                                results.Add((intVal, displayInfo));
                            }
                        }
                        catch (Exception ex)
                        {
                            _validationErrors.Add(new ValidationError(true,
                                "Exception thrown in: " + displayInfo + Environment.NewLine
                                + ex.ToString(), benchmark));
                        }
                    }
                }
                if (results.Count > 0 && results.Any(r => r.retVal != results[0].retVal))
                {
                    _validationErrors.Add(new ValidationError(true,
                        "Inconsistent results:" + Environment.NewLine + string.Join(
                            Environment.NewLine, results.Select(r => $"  {r.retVal,8}  {r.displayInfo}"))));
                }
            }
            return _validationErrors;
        }

        public IEnumerable<FileBytesWrapper> FileBytes_Arguments() => TestFiles.GetWrappedFileBytes();

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XPathDocument_XPath_LinesPerClass(FileBytesWrapper file)
        {
            return _parser.XPathDocument_XPath_LinesPerClass(
                new FileParamWrapper<XPathDocument>(
                    new XPathDocument(new MemoryStream(file.Value)),
                    file.DisplayText!));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XElement_Linq_LinesPerClass(FileBytesWrapper file)
        {
            return _parser.XElement_Linq_LinesPerClass(
                new FileParamWrapper<XElement>(
                    XElement.Load(new MemoryStream(file.Value)),
                    file.DisplayText!));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XElement_Linq_LinesPerClass_Parallel(FileBytesWrapper file)
        {
            return _parser.XElement_Linq_LinesPerClass_Parallel(
                new FileParamWrapper<XElement>(
                    XElement.Load(new MemoryStream(file.Value)),
                    file.DisplayText!));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XElement_Linq_LinesParentLookup(FileBytesWrapper file)
        {
            return _parser.XElement_Linq_LinesParentLookup(
                new FileParamWrapper<XElement>(
                    XElement.Load(new MemoryStream(file.Value)),
                    file.DisplayText!));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XmlDocument_XPathStr_LinesPerClass(FileBytesWrapper file)
        {
            XmlDocument doc = new();
            doc.Load(new MemoryStream(file.Value));
            return _parser.XmlDocument_XPathStr_LinesPerClass(
                new FileParamWrapper<XmlDocument>(doc, file.DisplayText!));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XmlReader_ReadValidTree(FileBytesWrapper file)
        {
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
            XmlReader reader = XmlReader.Create(new MemoryStream(file.Value), settings);
            int sum = 0;

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowXml() => throw new XmlException();

            while (reader.MoveToContent() != XmlNodeType.Element
                   && reader.NodeType != XmlNodeType.EndElement
                   && reader.Read()) { }
            reader.ReadStartElement("coverage");
            while (reader.MoveToContent() != XmlNodeType.Element
                   && reader.NodeType != XmlNodeType.EndElement
                   && reader.Read()) { }
            if (reader.LocalName != "packages") reader.ReadToNextSibling("packages");
            reader.ReadStartElement("packages");
            while (!reader.EOF)
            {
                while (reader.MoveToContent() != XmlNodeType.Element
                       && reader.NodeType != XmlNodeType.EndElement
                       && reader.Read()) { }
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "packages")
                {
                    reader.Read();
                    break;
                }
                reader.ReadStartElement("package");
                while (!reader.EOF)
                {
                    while (reader.MoveToContent() != XmlNodeType.Element
                           && reader.NodeType != XmlNodeType.EndElement
                           && reader.Read()) { }
                    if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "package")
                    {
                        reader.Read();
                        break;
                    }
                    reader.ReadStartElement("classes");
                    while (!reader.EOF)
                    {
                        while (reader.MoveToContent() != XmlNodeType.Element
                               && reader.NodeType != XmlNodeType.EndElement
                               && reader.Read()) { }
                        if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "classes")
                        {
                            reader.Read();
                            break;
                        }
                        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "class") ThrowXml();
                        if (!reader.MoveToAttribute("filename")) ThrowXml();
                        string fileName = reader.Value;
                        reader.MoveToElement();
                        reader.Read();
                        while (!reader.EOF)
                        {
                            while (reader.MoveToContent() != XmlNodeType.Element
                                   && reader.NodeType != XmlNodeType.EndElement
                                   && reader.Read()) { }
                            if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "class")
                            {
                                reader.Read();
                                break;
                            }
                            if (reader.LocalName != "lines") reader.ReadToNextSibling("lines");
                            reader.ReadStartElement("lines");
                            while (!reader.EOF)
                            {
                                while (reader.MoveToContent() != XmlNodeType.Element
                                       && reader.NodeType != XmlNodeType.EndElement
                                       && reader.Read()) { }
                                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "lines")
                                {
                                    reader.Read();
                                    break;
                                }
                                if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "line") ThrowXml();
                                if (!reader.MoveToFirstAttribute()) ThrowXml();
                                string coverage = string.Empty;
                                string number = string.Empty;
                                string hits = string.Empty;
                                do
                                {
                                    switch (reader.LocalName)
                                    {
                                        case "number":
                                            number = reader.Value;
                                            break;
                                        case "hits":
                                            hits = reader.Value;
                                            break;
                                        case "condition-coverage":
                                            coverage = reader.Value;
                                            break;
                                    }
                                } while (reader.MoveToNextAttribute());
                                if (hits == "0" || (coverage.Length != 0
                                                    && !coverage.StartsWith("100%", StringComparison.Ordinal)))
                                {
                                    sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                                }
                                reader.MoveToElement();
                                reader.ReadToNextSibling("line");
                            }
                        }
                    }
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int XmlReader_ReadFastForward(FileBytesWrapper file)
        {
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
            XmlReader reader = XmlReader.Create(new MemoryStream(file.Value), settings);
            int sum = 0;

            while (reader.ReadToFollowing("class"))
            {
                if (!reader.MoveToAttribute("filename")) throw new XmlException();
                string fileName = reader.Value;
                reader.MoveToElement();
                reader.Read();
                // NextSibling makes sure we skip over "class/methods/method/lines" directly to
                // "class/lines".
                if (!reader.ReadToNextSibling("lines")) throw new XmlException();
                reader.Read();
                while (reader.ReadToNextSibling("line"))
                {
                    if (!reader.MoveToFirstAttribute()) throw new XmlException();
                    string coverage = string.Empty;
                    string number = string.Empty;
                    string hits = string.Empty;
                    do
                    {
                        switch (reader.LocalName)
                        {
                            case "number":
                                number = reader.Value;
                                break;
                            case "hits":
                                hits = reader.Value;
                                break;
                            case "condition-coverage":
                                coverage = reader.Value;
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                    if (hits == "0" || (coverage.Length != 0
                                        && !coverage.StartsWith("100%", StringComparison.Ordinal)))
                    {
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                    reader.MoveToElement();
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int StringSpan_IndexOf(FileBytesWrapper file)
        {
            ReadOnlySpan<char> content = Encoding.UTF8.GetString(new ReadOnlySpan<byte>(file.Value)).AsSpan();
            ReadOnlySpan<char> zero = "0".AsSpan();
            int sum = 0;
            int classStart, classEnd;

            // StringComparison.Ordinal is default for IndexOf
            while ((classStart = content.IndexOf("<class ")) >= 0)
            {
                ReadOnlySpan<char> classContent = content.Slice(classStart);
                classEnd = classContent.IndexOf("</class>");
                classContent = classContent.Slice(0, classEnd);
                content = content.Slice(classStart + classEnd);
                int end = classContent.IndexOf('>');
                ReadOnlySpan<char> classTag = classContent.Slice(0, end);
                int attrStart = classTag.IndexOf("filename=\"") + 10;
                ReadOnlySpan<char> fileName = classTag.Slice(attrStart);
                end = fileName.IndexOf('"');
                fileName = fileName.Slice(0, end);

                int linesStart, linesEnd, methodsEnd;
                ReadOnlySpan<char> linesContent;
                methodsEnd = classContent.IndexOf("</methods>");
                if (methodsEnd >= 0)
                {
                    linesContent = classContent.Slice(methodsEnd);
                    linesStart = linesContent.IndexOf("<lines>") + methodsEnd + 1;
                }
                else
                {
                    linesStart = classContent.IndexOf("<lines>") + 1;
                }
                linesContent = classContent.Slice(linesStart);
                linesEnd = linesContent.IndexOf("</lines>");
                linesContent = linesContent.Slice(0, linesEnd);
                int lineStart, lineEnd;
                while ((lineStart = linesContent.IndexOf("<line ")) >= 0)
                {
                    ReadOnlySpan<char> lineContent = linesContent.Slice(lineStart);
                    lineEnd = lineContent.IndexOf('>');
                    lineContent = lineContent.Slice(0, lineEnd);
                    linesContent = linesContent.Slice(lineStart + lineEnd);
                    ReadOnlySpan<char> number = default, hits = default, coverage = default;
                    attrStart = lineContent.IndexOf("number=\"") + 8;
                    if (attrStart >= 8)
                    {
                        number = lineContent.Slice(attrStart);
                        end = number.IndexOf('"');
                        number = number.Slice(0, end);
                    }
                    attrStart = lineContent.IndexOf("hits=\"") + 6;
                    if (attrStart >= 6)
                    {
                        hits = lineContent.Slice(attrStart);
                        end = hits.IndexOf('"');
                        hits = hits.Slice(0, end);
                    }
                    attrStart = lineContent.IndexOf("condition-coverage=\"") + 20;
                    if (attrStart >= 20)
                    {
                        coverage = lineContent.Slice(attrStart);
                        end = coverage.IndexOf('"');
                        coverage = coverage.Slice(0, end);
                    }
                    if (hits.SequenceEqual(zero) || (coverage.Length != 0 && !coverage.StartsWith("100%")))
                    {
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int ByteSpan_IndexOf(FileBytesWrapper file)
        {
            ReadOnlySpan<byte> content = file.Value.AsSpan();
            ReadOnlySpan<byte> zero = Encoding.UTF8.GetBytes("0").AsSpan();
            ReadOnlySpan<byte> hundredPercent = Encoding.UTF8.GetBytes("100%").AsSpan();
            ReadOnlySpan<byte> gt = Encoding.UTF8.GetBytes(">").AsSpan();
            ReadOnlySpan<byte> quote = Encoding.UTF8.GetBytes("\"").AsSpan();
            ReadOnlySpan<byte> tagClassStart = Encoding.UTF8.GetBytes("<class ").AsSpan();
            ReadOnlySpan<byte> tagClassEnd = Encoding.UTF8.GetBytes("</class>").AsSpan();
            ReadOnlySpan<byte> attrFilename = Encoding.UTF8.GetBytes("filename=\"").AsSpan();
            ReadOnlySpan<byte> tagMethodsEnd = Encoding.UTF8.GetBytes("</methods>").AsSpan();
            ReadOnlySpan<byte> tagLinesStart = Encoding.UTF8.GetBytes("<lines>").AsSpan();
            ReadOnlySpan<byte> tagLinesEnd = Encoding.UTF8.GetBytes("</lines>").AsSpan();
            ReadOnlySpan<byte> tagLineStart = Encoding.UTF8.GetBytes("<line ").AsSpan();
            ReadOnlySpan<byte> attrNumber = Encoding.UTF8.GetBytes("number=\"").AsSpan();
            ReadOnlySpan<byte> attrHits = Encoding.UTF8.GetBytes("hits=\"").AsSpan();
            ReadOnlySpan<byte> attrCoverage = Encoding.UTF8.GetBytes("condition-coverage=\"").AsSpan();
            int sum = 0;
            int classStart, classEnd;

            while ((classStart = content.IndexOf(tagClassStart)) >= 0)
            {
                ReadOnlySpan<byte> classContent = content.Slice(classStart);
                classEnd = classContent.IndexOf(tagClassEnd);
                classContent = classContent.Slice(0, classEnd);
                content = content.Slice(classStart + classEnd);
                int end = classContent.IndexOf(gt);
                ReadOnlySpan<byte> classTag = classContent.Slice(0, end);
                int attrStart = classTag.IndexOf(attrFilename) + 10;
                ReadOnlySpan<byte> fileName = classTag.Slice(attrStart);
                end = fileName.IndexOf(quote);
                fileName = fileName.Slice(0, end);

                int linesStart, linesEnd, methodsEnd;
                ReadOnlySpan<byte> linesContent;
                methodsEnd = classContent.IndexOf(tagMethodsEnd);
                if (methodsEnd >= 0)
                {
                    linesContent = classContent.Slice(methodsEnd);
                    linesStart = linesContent.IndexOf(tagLinesStart) + methodsEnd + 1;
                }
                else
                {
                    linesStart = classContent.IndexOf(tagLinesStart) + 1;
                }
                linesContent = classContent.Slice(linesStart);
                linesEnd = linesContent.IndexOf(tagLinesEnd);
                linesContent = linesContent.Slice(0, linesEnd);
                int lineStart, lineEnd;
                while ((lineStart = linesContent.IndexOf(tagLineStart)) >= 0)
                {
                    ReadOnlySpan<byte> lineContent = linesContent.Slice(lineStart);
                    lineEnd = lineContent.IndexOf(gt);
                    lineContent = lineContent.Slice(0, lineEnd);
                    linesContent = linesContent.Slice(lineStart + lineEnd);
                    ReadOnlySpan<byte> number = default, hits = default, coverage = default;
                    attrStart = lineContent.IndexOf(attrNumber) + 8;
                    if (attrStart >= 8)
                    {
                        number = lineContent.Slice(attrStart);
                        end = number.IndexOf(quote);
                        number = number.Slice(0, end);
                    }
                    attrStart = lineContent.IndexOf(attrHits) + 6;
                    if (attrStart >= 6)
                    {
                        hits = lineContent.Slice(attrStart);
                        end = hits.IndexOf(quote);
                        hits = hits.Slice(0, end);
                    }
                    attrStart = lineContent.IndexOf(attrCoverage) + 20;
                    if (attrStart >= 20)
                    {
                        coverage = lineContent.Slice(attrStart);
                        end = coverage.IndexOf(quote);
                        coverage = coverage.Slice(0, end);
                    }
                    if (hits.SequenceEqual(zero) || (coverage.Length != 0 && !coverage.StartsWith(hundredPercent)))
                    {
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                }
            }
            return sum;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int ByteSpan_XmlReader_Tasks(FileBytesWrapper file)
        {
            ReadOnlySpan<byte> content = file.Value.AsSpan();
            ReadOnlySpan<byte> tagClassStart = Encoding.UTF8.GetBytes("<class ").AsSpan();
            ReadOnlySpan<byte> tagClassEnd = Encoding.UTF8.GetBytes("</class>").AsSpan();
            List<Task<int>> classes = new(50);

            int classStart, classEnd = 0;
            int classStartAbsolute = 0;

            while ((classStart = content.IndexOf(tagClassStart)) >= 0)
            {
                classStartAbsolute += classStart + classEnd;
                ReadOnlySpan<byte> classContent = content.Slice(classStart);
                classEnd = classContent.IndexOf(tagClassEnd) + tagClassEnd.Length;
                content = content.Slice(classStart + classEnd);
                classes.Add(Task.Factory.StartNew(
                    ReadClass, new MemoryStream(file.Value, classStartAbsolute, classEnd)));
            }
            int[] sums = Task.WhenAll(classes).GetAwaiter().GetResult();
            int sum = 0;
            for (int i = 0; i < sums.Length; i++) sum += sums[i];
            return sum;

            static int ReadClass(object? state)
            {
                int sum = 0;
                MemoryStream ms = (MemoryStream)state!;
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
                XmlReader reader = XmlReader.Create(ms, settings);
                if (!reader.ReadToFollowing("class")) throw new XmlException();
                if (!reader.MoveToAttribute("filename")) throw new XmlException();
                string fileName = reader.Value;
                reader.MoveToElement();
                reader.Read();
                // NextSibling makes sure we skip over "class/methods/method/lines" directly to
                // "class/lines".
                if (!reader.ReadToNextSibling("lines")) throw new XmlException();
                reader.Read();
                while (reader.ReadToNextSibling("line"))
                {
                    if (!reader.MoveToFirstAttribute()) throw new XmlException();
                    string coverage = string.Empty;
                    string number = string.Empty;
                    string hits = string.Empty;
                    do
                    {
                        switch (reader.LocalName)
                        {
                            case "number":
                                number = reader.Value;
                                break;
                            case "hits":
                                hits = reader.Value;
                                break;
                            case "condition-coverage":
                                coverage = reader.Value;
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                    if (hits == "0" || (coverage.Length != 0
                                        && !coverage.StartsWith("100%", StringComparison.Ordinal)))
                    {
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                    reader.MoveToElement();
                }
                reader.Close();
                ms.Dispose();
                return sum;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(FileBytes_Arguments))]
        public int ByteSpan_XmlReader_Parallel(FileBytesWrapper file)
        {
            ReadOnlySpan<byte> content = file.Value.AsSpan();
            ReadOnlySpan<byte> tagClassStart = Encoding.UTF8.GetBytes("<class ").AsSpan();
            ReadOnlySpan<byte> tagClassEnd = Encoding.UTF8.GetBytes("</class>").AsSpan();
            List<(int start, int count)> classes = new(50);

            int classStart, classEnd = 0;
            int classStartAbsolute = 0;

            while ((classStart = content.IndexOf(tagClassStart)) >= 0)
            {
                classStartAbsolute += classStart + classEnd;
                ReadOnlySpan<byte> classContent = content.Slice(classStart);
                classEnd = classContent.IndexOf(tagClassEnd) + tagClassEnd.Length;
                content = content.Slice(classStart + classEnd);
                classes.Add((classStartAbsolute, classEnd));
            }
            int sumTotal = 0;
            classes.AsParallel().ForAll(p =>
            {
                int sum = 0;
                MemoryStream ms = new(file.Value, p.start, p.count);
                XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore };
                XmlReader reader = XmlReader.Create(ms, settings);
                if (!reader.ReadToFollowing("class")) throw new XmlException();
                if (!reader.MoveToAttribute("filename")) throw new XmlException();
                string fileName = reader.Value;
                reader.MoveToElement();
                reader.Read();
                // NextSibling makes sure we skip over "class/methods/method/lines" directly to
                // "class/lines".
                if (!reader.ReadToNextSibling("lines")) throw new XmlException();
                reader.Read();
                while (reader.ReadToNextSibling("line"))
                {
                    if (!reader.MoveToFirstAttribute()) throw new XmlException();
                    string coverage = string.Empty;
                    string number = string.Empty;
                    string hits = string.Empty;
                    do
                    {
                        switch (reader.LocalName)
                        {
                            case "number":
                                number = reader.Value;
                                break;
                            case "hits":
                                hits = reader.Value;
                                break;
                            case "condition-coverage":
                                coverage = reader.Value;
                                break;
                        }
                    } while (reader.MoveToNextAttribute());
                    if (hits == "0" || (coverage.Length != 0
                                        && !coverage.StartsWith("100%", StringComparison.Ordinal)))
                    {
                        sum += fileName.Length + coverage.Length + number.Length + hits.Length;
                    }
                    reader.MoveToElement();
                }
                reader.Close();
                ms.Dispose();
                Interlocked.Add(ref sumTotal, sum);
            });
            return sumTotal;
        }
    }
}
