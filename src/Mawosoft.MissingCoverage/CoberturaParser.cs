// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace Mawosoft.MissingCoverage
{
    internal class CoberturaParser
    {
        private static readonly XPathExpression s_xpathVerify = XPathExpression.Compile("/coverage/packages/package/classes/class/lines/line");
        private static readonly XPathExpression s_xpathAltVerify = XPathExpression.Compile("/coverage/packages/class/lines/line");
        private static readonly XPathExpression s_xpathSources = XPathExpression.Compile("/coverage/sources/source");
        private static readonly XPathExpression s_xpathClasses = XPathExpression.Compile("/coverage/packages/package/classes/class");
        private static readonly XPathExpression s_xpathAltClasses = XPathExpression.Compile("/coverage/packages/class");
        private static readonly ConcurrentDictionary<(int hitThreshold, int coverageThreshold, int branchThreshold), XPathExpression> s_xpathLinesCached = new();
        private static readonly Regex s_regexConditionCoverage = new(@"\((\d+)/(\d+)\)", RegexOptions.CultureInvariant);

        private readonly XPathExpression _xpathClasses;

        public XPathDocument Document { get; }

        public CoberturaParser(string filePath)
        {
            Document = new XPathDocument(filePath);
            _xpathClasses = VerifyDocumentAndGetXPathClasses();
        }

        public CoberturaParser(Stream stream)
        {
            Document = new XPathDocument(stream);
            _xpathClasses = VerifyDocumentAndGetXPathClasses();
        }

        private XPathExpression VerifyDocumentAndGetXPathClasses()
        {
            XPathNavigator navi = Document.CreateNavigator();
            if (navi.SelectSingleNode(s_xpathVerify) != null)
            {
                return s_xpathClasses;
            }
            else if (navi.SelectSingleNode(s_xpathAltVerify) != null)
            {
                return s_xpathAltClasses;
            }
            ThrowXmlException(null, "This is not a valid Cobertura report.");
            throw null; // Compiler doesn't seem to recognize [DoesNotReturn] attribute?
        }

        public CoverageResult Parse(int hitThreshold, int coverageThreshold, int branchThreshold,
                                    Func<string, IReadOnlyList<string>, string>? filePathResolver)
        {
            if (!s_xpathLinesCached.TryGetValue((hitThreshold, coverageThreshold, branchThreshold),
                                                out XPathExpression? xpathLines))
            {
                xpathLines = CreateCachedXpathLines(hitThreshold, coverageThreshold, branchThreshold);
            }
            if (filePathResolver == null)
            {
                filePathResolver = ResolveFilePath;
            }
            XPathNavigator navi = Document.CreateNavigator();
            List<string> sourceDirectories = new();
            XPathNodeIterator sources = navi.Select(s_xpathSources);
            foreach (XPathNavigator source in sources)
            {
                if (source.Value.Length > 0)
                {
                    sourceDirectories.Add(source.Value);
                }
            }
            // We temporarly use a dictionary of relative source file names to avoid resolving file names we
            // don't need, or the same file name multiple times.
            Dictionary<string, SourceFileInfo> sourceFiles = new();
            XPathNodeIterator classes = navi.Select(_xpathClasses);
            foreach (XPathNavigator @class in classes)
            {
                string fileName = @class.GetAttribute("filename", "");
                if (fileName.Length == 0)
                {
                    ThrowXmlException(@class, "Invalid or missing attribute 'filename'.");
                }
                bool newFile = false;
                if (!sourceFiles.TryGetValue(fileName, out SourceFileInfo fileInfo))
                {
                    fileInfo = new(fileName);
                    newFile = true;
                }
                XPathNodeIterator lines = @class.Select(xpathLines);
                foreach (XPathNavigator line in lines)
                {
                    LineInfo lineInfo = default;
                    string coverage = line.GetAttribute("condition-coverage", "");
                    if (coverage.Length != 0)
                    {
                        Match m = s_regexConditionCoverage.Match(coverage);
                        if (!m.Success)
                        {
                            ThrowXmlException(line, "Invalid attribute value 'condition-coverage'.");
                        }
                        lineInfo.CoveredBranches = int.Parse(m.Groups[1].Value);
                        lineInfo.TotalBranches = int.Parse(m.Groups[2].Value);
                    }
                    if (!int.TryParse(line.GetAttribute("number", ""), out lineInfo.LineNumber))
                    {
                        ThrowXmlException(line, "Invalid or missing attribute 'number'.");
                    }
                    if (!int.TryParse(line.GetAttribute("hits", ""), out lineInfo.Hits))
                    {
                        ThrowXmlException(line, "Invalid or missing attribute 'hits'.");
                    }
                    fileInfo.AddOrMergeLine(lineInfo);
                }
                if (newFile && fileInfo.Lines.Count > 0)
                {
                    sourceFiles[fileName] = fileInfo;
                }
            }
            string inputFilePath = Document.CreateNavigator().BaseURI;
            try
            {
                Uri uri = new(inputFilePath);
                if (uri.IsFile)
                {
                    inputFilePath = uri.LocalPath;
                }
            }
            catch (Exception)
            {
            }
            CoverageResult result = new(hitThreshold, coverageThreshold, branchThreshold, inputFilePath);
            // Resolve relative file names against source dirs and add to result.
            foreach (SourceFileInfo fileInfo in sourceFiles.Values)
            {
                string resolved = filePathResolver(fileInfo.FilePath, sourceDirectories);
                SourceFileInfo sourceFileInfo = new(resolved, fileInfo.Lines);
                result.SourceFiles.Add(sourceFileInfo.FilePath, sourceFileInfo);
            }
            return result;
        }

        // Internal file path resolver if caller hasn't provided one
        private static string ResolveFilePath(string fileName, IReadOnlyList<string> sourceDirectories)
        {
            if (sourceDirectories.Count == 0)
                return fileName;
            else if (sourceDirectories.Count == 1)
                return Path.Combine(sourceDirectories[0], fileName);
            else
            {
                foreach (string sourceDirectory in sourceDirectories)
                {
                    string combined = Path.Combine(sourceDirectory,fileName);
                    if (File.Exists(combined))
                    {
                        return combined;
                    }
                }
                return fileName;
            }
        }

        private static XPathExpression CreateCachedXpathLines(int hitThreshold, int coverageThreshold,
                                                              int branchThreshold)
        {
            // XPath expressions optimized for certain parameter sets.
            string s = (hitThreshold, coverageThreshold, branchThreshold) switch
            {
                (1, 100, < 4) => "lines/line[@hits = '0' or (@condition-coverage and not(starts-with(@condition-coverage, '100%')))]",
                (0, 100, < 4) => "lines/line[@condition-coverage and not(starts-with(@condition-coverage, '100%'))]",
                (0, > 100, < 4) => "lines/line[@condition-coverage]",
                (1, 0, < 4) => "lines/line[@hits = '0']",
                (_, 0, < 4) => $"lines/line[number(@hits) < {hitThreshold}]",
                (_, _, < 4) => $"lines/line[number(@hits) < {hitThreshold} or number(substring-before(@condition-coverage, '%')) < {coverageThreshold}]",
                _ => $"lines/line[number(@hits) < {hitThreshold} or (number(substring-before(@condition-coverage, '%')) < {coverageThreshold} and number(substring-before(substring-after(@condition-coverage, '/'), ')')) >= {branchThreshold})]",
            };
            XPathExpression xpath = XPathExpression.Compile(s);
            // We don't care if someone else has already added it, we can still return our result.
            _ = s_xpathLinesCached.TryAdd((hitThreshold, coverageThreshold, branchThreshold), xpath);
            return xpath;
        }

        [DoesNotReturn]
        private static void ThrowXmlException(XPathNavigator? node = null, string? message = null,
                                              Exception? innerException = null)
        {
            int lineNumber = 0, linePosition = 0;
            if (node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                lineNumber = lineInfo.LineNumber;
                linePosition = lineInfo.LinePosition;
            }
            throw new XmlException(message, innerException, lineNumber, linePosition);
        }
    }
}
