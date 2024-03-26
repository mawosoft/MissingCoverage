// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage;

internal sealed class CoberturaParser : IDisposable
{
    private static readonly Regex s_regexDeterministic = new(
        @"^/_\d?/", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly XmlReaderSettings s_xmlReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        Async = false, // Explicit for clarity
    };

    private sealed class XmlLineInfo : IXmlLineInfo
    {
        public int LineNumber { get; private set; }
        public int LinePosition { get; private set; }
        public bool HasLineInfo() => true;
        public void Assign(XmlReader reader)
        {
            LineNumber = 0; LinePosition = 0;
            IXmlLineInfo? lineInfo = reader as IXmlLineInfo;
            if (lineInfo?.HasLineInfo() == true)
            {
                LineNumber = lineInfo.LineNumber;
                LinePosition = lineInfo.LinePosition;
            }
        }
    }

    private readonly XmlReader _xmlReader;
    private readonly List<string> _sourceDirectories;

    public string ReportFilePath { get; }
    public DateTime ReportTimestamp { get; }
    public Func<string, IReadOnlyList<string>, string>? FilePathResolver { get; set; }

    // Internals for tests.
    internal ReadState ReadState => _xmlReader.ReadState;
    internal IReadOnlyList<string> SourceDirectories => _sourceDirectories;


    public void Dispose() => ((IDisposable)_xmlReader).Dispose();

    public CoberturaParser(string reportFilePath) : this(reportFilePath, s_xmlReaderSettings)
    {
    }

    internal CoberturaParser(string reportFilePath, XmlReaderSettings xmlReaderSettings)
    {
        if (string.IsNullOrWhiteSpace(reportFilePath))
        {
            throw new ArgumentException(null, nameof(reportFilePath));
        }
        ReportFilePath = reportFilePath;
        ReportTimestamp = File.GetLastWriteTimeUtc(ReportFilePath);
        _xmlReader = XmlReader.Create(ReportFilePath, xmlReaderSettings);
        _sourceDirectories = [];
    }

    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "TODO false positive?")]
    public CoverageResult Parse()
    {
        CheckDisposed();
        try
        {
            while (_xmlReader.Read() && _xmlReader.NodeType != XmlNodeType.Element) { /* do nothing*/ }
            if (_xmlReader.Name != "coverage" || _xmlReader.AttributeCount < 2
                || _xmlReader.GetAttribute("profilerVersion") != null
                || _xmlReader.GetAttribute("clover") != null
                || _xmlReader.GetAttribute("generated") != null)
            {
                ThrowXmlException("This is not a valid Cobertura report.");
            }

            if (ReadToChild("") && _xmlReader.Name == "sources" && ReadToChild("source"))
            {
                // <sources> is optional
                // Note: ReadElementContentAsXxx reads the content of the current element and moves past it.
                // - We can't use ReadToNextSibling() because we probably are already on the next sibling.
                // - We have to get IXmlLineInfo before reading the content if we want to throw with with
                //   proper context.
                XmlLineInfo xmlLineInfo = new();
                do
                {
                    xmlLineInfo.Assign(_xmlReader);
                    string sourcedir = _xmlReader.ReadElementContentAsString();
                    if (sourcedir.Length == 0)
                    {
                        ThrowXmlException(xmlLineInfo, "Invalid element 'source'.");
                    }
                    else if (sourcedir.EndsWith(':'))
                    {
                        sourcedir += Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        sourcedir = NormalizeDirectorySeparators(sourcedir);
                    }
                    _sourceDirectories.Add(sourcedir);
                    while (_xmlReader.NodeType != XmlNodeType.Element
                           && _xmlReader.NodeType != XmlNodeType.EndElement
                           && _xmlReader.Read()) { /* do nothing*/ }
                } while (_xmlReader.Name == "source");
            }

            // We temporarly use a dictionary of relative source file names to avoid resolving file names
            // multiple times. Also, sourceFileInfo holds the last processed one, which is the most likely
            // to appear again.
            Dictionary<string, SourceFileInfo> sourceFiles = new(StringComparer.OrdinalIgnoreCase);
            SourceFileInfo? sourceFileInfo = null;
            string? sourceFileNameAsIs = null;
            while (_xmlReader.ReadToFollowing("class"))
            {
                string fileName = _xmlReader.GetAttribute("filename") ?? string.Empty;
                if (fileName.Length == 0)
                {
                    ThrowInvalidOrMissingAttribute("filename");
                }
                // Do a quick check against last processed file name.
                if (sourceFileInfo == null || !fileName.Equals(sourceFileNameAsIs, StringComparison.Ordinal))
                {
                    sourceFileNameAsIs = fileName;
                    fileName = NormalizeDirectorySeparators(fileName);
                    if (!sourceFiles.TryGetValue(fileName, out sourceFileInfo))
                    {
                        sourceFileInfo = new(fileName, ReportTimestamp);
                        sourceFiles[fileName] = sourceFileInfo;
                    }
                }
                // Skip over "class/methods/method/lines" directly to "class/lines".
                if (!ReadToChild("lines"))
                {
                    ThrowXmlException("Subtree 'lines' not found.");
                }
                if (ReadToChild("line"))
                {
                    do
                    {
                        LineInfo lineInfo = default;
                        if (!int.TryParse(_xmlReader.GetAttribute("number"), out int lineNumber)
                            || lineNumber < 1)
                        {
                            ThrowInvalidOrMissingAttribute("number");
                        }
                        else if (lineNumber > SourceFileInfo.MaxLineNumber)
                        {
                            ThrowXmlException($"Line attribute 'number' is larger than MaxLineNumber ({SourceFileInfo.MaxLineNumber}).");
                        }

                        // 'hits' is not required in loose DTD.
                        string? hitsAttr = _xmlReader.GetAttribute("hits");
                        long hits = 0;
                        if (hitsAttr != null && (!long.TryParse(hitsAttr, out hits) || hits < 0))
                        {
                            ThrowXmlException("Invalid attribute 'hits'.");
                        }
                        lineInfo.Hits = (int)Math.Min(hits, int.MaxValue);

                        // Just "100%" is possible for non-branch, otherwise empty or "percent% (covered/total)"
                        ReadOnlySpan<char> coverage = _xmlReader.GetAttribute("condition-coverage")
                                                      ?? string.Empty;
                        int pos = coverage.IndexOf('(');
                        if (pos >= 0)
                        {
                            coverage = coverage[(pos + 1)..];
                            pos = coverage.IndexOf('/');
                            if (pos < 0
                                || !ushort.TryParse(coverage[..pos], out lineInfo.CoveredBranches))
                            {
                                ThrowInvalidConditionCoverage();
                            }
                            coverage = coverage[(pos + 1)..];
                            pos = coverage.IndexOf(')');
                            if (pos < 0
                                || !ushort.TryParse(coverage[..pos], out lineInfo.TotalBranches))
                            {
                                ThrowInvalidConditionCoverage();
                            }
                        }
                        else if (coverage.Length != 0 && !coverage.SequenceEqual("100%"))
                        {
                            ThrowInvalidConditionCoverage();
                        }

                        sourceFileInfo.AddOrMergeLine(lineNumber, lineInfo);

                    } while (_xmlReader.ReadToNextSibling("line"));
                }
            }

            Func<string, IReadOnlyList<string>, string> filePathResolver = FilePathResolver ?? ResolveFilePath;
            CoverageResult result = new(ReportFilePath);
            foreach (SourceFileInfo fileInfo in sourceFiles.Values)
            {
                fileInfo.SourceFilePath = filePathResolver(fileInfo.SourceFilePath, _sourceDirectories);
                result.AddOrMergeSourceFile(fileInfo);
            }
            return result;
        }
        finally
        {
            _xmlReader.Close();
        }

    }

    private bool ReadToChild(string name)
    {
        _xmlReader.MoveToElement();
        if (_xmlReader.NodeType != XmlNodeType.Element)
        {
            ThrowXmlException($"{nameof(ReadToChild)} called on non-XmlElement node.");
        }
        if (!_xmlReader.IsEmptyElement)
        {
            while (_xmlReader.Read() & _xmlReader.NodeType != XmlNodeType.EndElement)
            {
                if (_xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (name.Length == 0 || name == _xmlReader.Name) return true;
                    return _xmlReader.ReadToNextSibling(name);
                }
            }
        }
        return false;
    }

    public static string NormalizeDirectorySeparators(string path)
    {
        if (path.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
            || (path.StartsWith("/_", StringComparison.Ordinal) && s_regexDeterministic.IsMatch(path)))
        {
            return path;
        }
        char replace = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
        return path.Replace(replace, Path.DirectorySeparatorChar);
    }

    private static string ResolveFilePath(string fileName, IReadOnlyList<string> sourceDirectories)
    {
        if (sourceDirectories.Count == 0)
            return fileName;
        else if (sourceDirectories.Count == 1)
        {
            // We cannot rely on Path.Combine to detect an absolute win path on MacOS or Linux.
            // "c:\something\" will read as relative there. We don't check if the file exists
            // for a single source dir, so we have to prevent Combine in this case.
            return fileName.Length >= 2
                   && fileName[1] == ':'
                   && fileName[0] is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')
                ? fileName
                : Path.Combine(sourceDirectories[0], fileName);
        }
        else
        {
            foreach (string sourceDirectory in sourceDirectories)
            {
                string combined = Path.Combine(sourceDirectory, fileName);
                if (File.Exists(combined))
                {
                    return combined;
                }
            }
            return fileName;
        }
    }

    private void CheckDisposed()
    {
        if (_xmlReader.ReadState == ReadState.Closed)
        {
            throw new ObjectDisposedException(nameof(CoberturaParser));
        }
    }

    [DoesNotReturn]
    private void ThrowInvalidConditionCoverage()
    {
        ThrowXmlException("Invalid attribute 'condition-coverage'.");
    }

    [DoesNotReturn]
    private void ThrowInvalidOrMissingAttribute(string attrName)
    {
        ThrowXmlException($"Invalid or missing attribute '{attrName}'.");
    }

    [DoesNotReturn]
    private void ThrowXmlException(string message, Exception? innerException = null)
    {
        ThrowXmlException(_xmlReader as IXmlLineInfo, message, innerException);
    }

    [DoesNotReturn]
    private static void ThrowXmlException(IXmlLineInfo? xmlLineInfo, string message, Exception? innerException = null)
    {
        int lineNumber = 0, linePosition = 0;
        if (xmlLineInfo?.HasLineInfo() == true)
        {
            lineNumber = xmlLineInfo.LineNumber;
            linePosition = xmlLineInfo.LinePosition;
        }
        throw new XmlException(message, innerException, lineNumber, linePosition);
    }
}
