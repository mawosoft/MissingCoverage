// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;

namespace Mawosoft.MissingCoverage
{
    /// <summary>Cobertura report parser</summary>
    // There is currently no benefit in maintaining a state here between ctor and Parse().
    internal static class CoberturaParser
    {
        /// <summary>
        /// Parses a Cobertura report file and returns a new <see cref="CoverageResult"/>.
        /// </summary>
        public static CoverageResult Parse(string reportFilePath)
        {
            DateTime reportTimestamp = File.GetLastWriteTime(reportFilePath);
            // TODO Benchmark async vs sync single file and multi/parallel
            // None of the ReadToXxx functions support async processing, plus there is extra overhead
            // in the internal async implementation. Since a single report file seems the most common
            // use case, we keep this sync for now.
            XmlReaderSettings settings = new()
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                Async = false, // Explicit for clarity
            };
            using XmlReader reader = XmlReader.Create(reportFilePath, settings);

            while (reader.Read() && reader.NodeType != XmlNodeType.Element) { /* do nothing*/ }
            if (reader.Name != "coverage" || reader.AttributeCount < 2
                || reader.GetAttribute("profilerVersion") != null
                || reader.GetAttribute("clover") != null
                || reader.GetAttribute("generated") != null)
            {
                ThrowXmlException(reader, "This is not a valid Cobertura report.");
            }

            List<string> sourceDirectories = new();
            if (reader.ReadToChild("") && reader.Name == "sources" && reader.ReadToChild("source"))
            {
                // <sources> is optional
                do
                {
                    // Note ReadElementContentAsXxx reads the content of the current element and moves past it.
                    // Hence we can't use ReadToNextSibling() because we probably are already on the next sibling.
                    string sourcedir = reader.ReadElementContentAsString();
                    if (sourcedir.Length == 2 && sourcedir[1] == ':' && Path.VolumeSeparatorChar == ':')
                    {
                        sourcedir += Path.DirectorySeparatorChar;
                    }
                    else
                    {
                        sourcedir = NormalizePathSeparators(sourcedir);
                    }
                    sourceDirectories.Add(sourcedir);
                    while (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement && reader.Read()) { /* do nothing*/ }
                } while (reader.Name == "source");
            }

            // We temporarly use a dictionary of relative source file names to avoid resolving file names
            // multiple times. Also, sourceFileInfo holds the last processed one, which is the most likely
            // to appear again.
            Dictionary<string, SourceFileInfo> sourceFiles = new(StringComparer.OrdinalIgnoreCase);
            SourceFileInfo? sourceFileInfo = null;
            while (reader.ReadToFollowing("class"))
            {
                string fileName = reader.GetAttribute("filename") ?? string.Empty;
                if (fileName.Length == 0)
                {
                    ThrowInvalidOrMissingAttribute(reader, "filename");
                }
                if (sourceFileInfo == null
                    || !fileName.Equals(sourceFileInfo.SourceFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    fileName = NormalizePathSeparators(fileName);
                    if (sourceFileInfo == null
                        || !fileName.Equals(sourceFileInfo.SourceFilePath, StringComparison.OrdinalIgnoreCase)
)
                    {
                        if (!sourceFiles.TryGetValue(fileName, out sourceFileInfo))
                        {
                            sourceFileInfo = new(fileName, reportTimestamp);
                            sourceFiles[fileName] = sourceFileInfo;
                        }
                    }
                }
                // Skip over "class/methods/method/lines" directly to "class/lines".
                if (!reader.ReadToChild("lines"))
                {
                    ThrowXmlException(reader, "Subtree 'lines' not found.");
                }
                if (reader.ReadToChild("line"))
                {
                    do
                    {
                        LineInfo lineInfo = default;
                        if (!int.TryParse(reader.GetAttribute("number"), out int lineNumber) || lineNumber < 1)
                        {
                            ThrowInvalidOrMissingAttribute(reader, "number");
                        }

                        // 'hits' is not required in loose DTD.
                        string? hitsAttr = reader.GetAttribute("hits");
                        long hits = 0;
                        if (hitsAttr != null && (!long.TryParse(hitsAttr, out hits) || hits < 0))
                        {
                            ThrowXmlException(reader, "Invalid attribute 'hits'.");
                        }
                        lineInfo.Hits = (int)Math.Min(hits, int.MaxValue);

                        // Just "100%" is possible for non-branch, otherwise empty or "percent% (covered/total)"
                        ReadOnlySpan<char> coverage = reader.GetAttribute("condition-coverage") ?? string.Empty;
                        int pos = coverage.IndexOf('(');
                        if (pos >= 0)
                        {
                            coverage = coverage.Slice(pos + 1);
                            pos = coverage.IndexOf('/');
                            if (pos < 0 || !ushort.TryParse(coverage.Slice(0, pos), out lineInfo.CoveredBranches))
                            {
                                ThrowInvalidConditionCoverage(reader);
                            }
                            coverage = coverage.Slice(pos + 1);
                            pos = coverage.IndexOf(')');
                            if (pos < 0 || !ushort.TryParse(coverage.Slice(0, pos), out lineInfo.TotalBranches))
                            {
                                ThrowInvalidConditionCoverage(reader);
                            }
                        }
                        else if (coverage.Length != 0 && !coverage.SequenceEqual("100%"))
                        {
                            ThrowInvalidConditionCoverage(reader);
                        }

                        sourceFileInfo.AddOrMergeLine(lineNumber, lineInfo);

                    } while (reader.ReadToNextSibling("line"));
                }
            }
            CoverageResult result = new(reportFilePath);
            foreach (SourceFileInfo fileInfo in sourceFiles.Values)
            {
                fileInfo.SourceFilePath = ResolveFilePath(fileInfo.SourceFilePath, sourceDirectories);
                result.AddOrMergeSourceFile(fileInfo);
            }
            return result;
        }

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
                    string combined = Path.Combine(sourceDirectory, fileName);
                    if (File.Exists(combined))
                    {
                        return combined;
                    }
                }
                return fileName;
            }
        }

        private static string NormalizePathSeparators(string path)
        {
            if (path.Length >= 7 && path.StartsWith("http", StringComparison.Ordinal))
            {
                // exclude http(s)://
                int index = 4;
                if (path[index] == 's') index++;
                if (path.Length > (index + 2) && path[index + 1] == '/' && path[index + 2] == '/')
                {
                    return path;
                }
            }
            char replace = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            return path.Replace(replace, Path.DirectorySeparatorChar);
        }

        private static bool ReadToChild(this XmlReader reader, string name)
        {
            reader.MoveToElement();
            if (reader.NodeType != XmlNodeType.Element)
            {
                ThrowXmlException(reader, "{nameof(ReadToChild)} called on non-XmlElement node.");
            }
            if (!reader.IsEmptyElement)
            {
                while (reader.Read() & reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (name.Length == 0 || name == reader.Name) return true;
                        return reader.ReadToNextSibling(name);
                    }
                }
            }
            return false;
        }

        [DoesNotReturn]
        private static void ThrowInvalidConditionCoverage(XmlReader reader)
        {
            ThrowXmlException(reader, "Invalid attribute 'condition-coverage'.");
        }

        [DoesNotReturn]
        private static void ThrowInvalidOrMissingAttribute(XmlReader reader, string attrName)
        {
            ThrowXmlException(reader, $"Invalid or missing attribute '{attrName}'.");
        }

        [DoesNotReturn]
        private static void ThrowXmlException(XmlReader reader, string? message = null,
                                              Exception? innerException = null)
        {
            int lineNumber = 0, linePosition = 0;
            if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                lineNumber = lineInfo.LineNumber;
                linePosition = lineInfo.LinePosition;
            }
            throw new XmlException(message, innerException, lineNumber, linePosition);
        }
    }
}
