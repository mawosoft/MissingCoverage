// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Xml.XPath;


namespace Mawosoft.MissingCoverage
{
    internal class CoberturaParser
    {
        private static readonly string[] s_rootElementAttributes = { "line-rate", "branch-rate", "timestamp" };
        private static readonly XPathExpression s_xpathRoot = XPathExpression.Compile("/coverage");
        private static readonly XPathExpression s_xpathSources = XPathExpression.Compile("/coverage/sources/source");
        private static readonly XPathExpression s_xpathClasses = XPathExpression.Compile("/coverage/packages/package/classes/class");
        private static readonly XPathExpression s_xpathLines = XPathExpression.Compile("lines/line[@hits='0' or (@condition-coverage and not(starts-with(@condition-coverage, '100%')))]");
        public XPathDocument Document;
        public CoberturaParser(string filePath)
        {
            Document = new(filePath);
            XPathNavigator? rootElem = Document.CreateNavigator().SelectSingleNode(s_xpathRoot);
            if (rootElem == null || Array.Exists(s_rootElementAttributes, i => string.IsNullOrEmpty(rootElem.GetAttribute(i, ""))))
            {
                throw new Exception("invalid doc"); // TODO proper error handling
            }
        }

        public CoverageResult Parse()
        {
            XPathNavigator navi = Document.CreateNavigator();
            CoverageResult result = new(navi.BaseURI);
            XPathNodeIterator sources = navi.Select(s_xpathSources);
            foreach (XPathNavigator source in sources)
            {
                if (!string.IsNullOrEmpty(source.Value))
                    result.SourceDirectories.Add(source.Value);
            }
            XPathNodeIterator classes = navi.Select(s_xpathClasses);
            foreach (XPathNavigator @class in classes)
            {
                string fileName = @class.GetAttribute("filename", "");
                bool newFile = false;
                if (!result.SourceFiles.TryGetValue(fileName, out SourceFileInfo? fileInfo))
                {
                    fileInfo = new(fileName);
                    newFile = true;
                }
                XPathNodeIterator lines = @class.Select(s_xpathLines);
                foreach (XPathNavigator line in lines)
                {
                    LineInfo lineInfo = new(int.Parse(line.GetAttribute("number", "")), int.Parse(line.GetAttribute("hits", "")), line.GetAttribute("condition-coverage", ""));
                    if (!fileInfo.Lines.TryAdd(lineInfo.LineNumber, lineInfo))
                    {
                        // TODO duplicate line info
                    }
                }
                if (newFile && fileInfo.Lines.Count > 0)
                {
                    result.SourceFiles[fileName] = fileInfo;
                }
            }
            return result;
        }
    }
}
