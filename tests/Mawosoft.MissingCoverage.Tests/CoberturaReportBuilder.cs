// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class CoberturaReportBuilder
    {
        private static readonly Regex s_regexCoverage = new(@"\((\d+)/(\d+)\)", RegexOptions.CultureInvariant);

        // Default DTD for Cobertura reports
        public static readonly XDocumentType DefaultDTD = new("coverage", null, "http://cobertura.sourceforge.net/xml/coverage-04.dtd", null!);

        private DateTime _reportTimestamp;
        private readonly List<string> _normalizedSourceDirectories = new();
        private XElement? _root;
        private XElement? _sources;
        private XElement? _packages;
        private XElement? _lastClass;
        private XElement? _firstInvalidElement;
        private int _packageId;
        private int _classId;
        private string? _lastClassFileNormalized;

        // Malformed packages/class vs. proper packages/package/classes/class
        public bool ClassBelowPackages { get; set; }

        // If true, AddLine() will add the line to both class/lines and class/methods/method/lines.
        // If false, class/methods/method/lines will only contain an invalid fake line that will
        // trip up the CoberturaParser if it doesn't process the correct lines/line collection.
        public bool MethodLinesMatch { get; set; }

        // If true, lines with branches will contain conditions/condition entries (as created by coverlet).
        // If false, those elements are omitted (as in merged reports by FineCodeCoverage (FCC)).
        public bool ConditionsSubtree { get; set; }

        // If true, branch attribute will have lowercase true/false values (FCC).
        // If false, branch attribute will have titlecase True/False values (coverlet).
        public bool TrueFalseLowerCase { get; set; }

        // The XML declaration or null for none. If a DTD is present, there will always be
        // a default XML declaration (XDocument behavior).
        public XDeclaration? XDeclaration { get; set; }

        // A DTD or null for none. For the default Cobertura one, see above
        public XDocumentType? XDocumentType { get; set; }

        // The CoverageResult the CoberturaParser should produce from this report.
        public CoverageResult CoverageResult { get; }

        // An optional valid report file path, needed for Save().
        public string? ReportFilePath { get; }

        // The list of sources/source directories, normalized. CoberturaParser.SourceDirectories should have these.
        public IReadOnlyList<string> NormalizedSourceDirectories => _normalizedSourceDirectories;

        // The first element in document order with invalid, but well-formed content/attributes.
        public XElement? FirstInvalidElement
        {
            get => _firstInvalidElement;
            set
            {
                if (FirstInvalidElement == null || (value != null && value.IsBefore(_firstInvalidElement)))
                {
                    _firstInvalidElement = value;
                }
            }
        }

        // The root element <coverage>
        public XElement Root
        {
            get
            {
                if (_root == null) AddRoot();
                return _root!;
            }
        }

        // fccFormat controls whether the general format follows FineCodeCoverage (FCC) or coverlet.
        public CoberturaReportBuilder(bool fccFormat = false)
        {
            XDeclaration = new("1.0", "utf-8", null);
            if (fccFormat)
            {
                XDocumentType = new(DefaultDTD);
                TrueFalseLowerCase = true;
            }
            else
            {
                ConditionsSubtree = true;
            }
            CoverageResult = new();
        }

        public CoberturaReportBuilder(string reportFilePath, bool fccFormat = false) : this(fccFormat)
        {
            CoverageResult = new(reportFilePath);
            ReportFilePath = reportFilePath;
            _reportTimestamp = File.GetLastWriteTimeUtc(reportFilePath);
        }

        public CoberturaReportBuilder AddRoot()
        {
            return AddRoot("coverage", ("version", "1.9"), ("timestamp", "1628548975"));
        }

        public CoberturaReportBuilder AddRoot(string rootElement, params (string, string)[] attributes)
        {
            if (_root == null)
            {
                _root = new(rootElement);
                foreach ((string name, string value) in attributes)
                {
                    _root.SetAttributeValue(name, value);
                }
            }
            return this;
        }

        public CoberturaReportBuilder AddMinimalDefaults()
        {
            if (_root == null)
            {
                AddRoot();
            }
            if (_sources == null)
            {
                // Only add if <sources> itself is missing to allow for empty sources.
                AddSources((@"C:\Users\mw\", true));
            }
            if (_lastClass == null)
            {
                // Will add package etc. as well if nessessary.
                AddClass(@".nuget\packages\microsoft.net.test.sdk\16.10.0\build\netcoreapp2.1\Microsoft.NET.Test.Sdk.Program.cs");
            }
            if (_lastClass?.Element("lines")?.Elements("line").FirstOrDefault() == null)
            {
                // Only add if none are present
                AddLine(4, 0);
            }
            return this;
        }

        public CoberturaReportBuilder AddSources(params (string sourceDirectory, bool normalize)[] sourceDirectories)
        {
            XElement sources = Sources(); // Ensure <sources> is created even if no params
            foreach ((string sourceDirectory, bool normalize) in sourceDirectories)
            {
                sources.Add(new XElement("source", sourceDirectory));
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    FirstInvalidElement = sources.Elements().Last();
                }
                else
                {
                    string normalized = normalize ? Normalize(sourceDirectory) : sourceDirectory;
                    if (normalize && normalized.Length == 2 && normalized[1] == ':')
                    {
                        normalized += Path.DirectorySeparatorChar;
                    }
                    _normalizedSourceDirectories.Add(normalized);
                }
            }
            return this;
        }

        public CoberturaReportBuilder AddPackage()
        {
            if (!ClassBelowPackages)
            {
                FinalizeClass();
                Packages().Add(new XElement("package",
                    new XAttribute("name", "Sample.Package" + _packageId++),
                    new XAttribute("line-rate", "0.9"),
                    new XAttribute("branch-rate", "0.8"),
                    new XAttribute("complexity", "5")));
            }
            return this;
        }

        public CoberturaReportBuilder AddClass(string? fileName, string? resolvedFileName = null, bool normalize = true)
        {
            XElement parent = LastClassParent();
            FinalizeClass();
            XElement @class = new("class", new XAttribute("name", "SampleClass" + _classId++));
            parent.Add(@class);
            if (fileName != null)
            {
                @class.Add(new XAttribute("filename", fileName));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                FirstInvalidElement = @class;
                resolvedFileName = null;
            }
            else
            {
                resolvedFileName ??= fileName;
            }

            XElement lines = new("lines");
            @class.Add(new XAttribute("line-rate", "0.9"),
                       new XAttribute("branch-rate", "0.8"),
                       new XAttribute("complexity", "5"));
            @class.Add(new XElement("methods",
                           new XElement("method",
                               new XAttribute("name", "Method1"),
                               new XAttribute("signature", "(System.String[])"),
                               new XAttribute("line-rate", "0.9"),
                               new XAttribute("branch-rate", "0.8"),
                               new XAttribute("complexity", "5"),
                               lines)));
            if (!MethodLinesMatch)
            {
                // This will throw if the parser doesn't correctly select class/lines/line.
                lines.Add(new XElement("line"));
            }
            _lastClass = @class;
            _lastClassFileNormalized = normalize ? Normalize(resolvedFileName) : resolvedFileName;
            return this;
        }

        public CoberturaReportBuilder AddLine(string? number, string? hits, bool? branch = null, string? condition = null)
        {
            bool validLine = true;
            LineInfo lineInfo = default;
            if (!int.TryParse(number, out int lineNumber) || lineNumber < 1 || lineNumber > SourceFileInfo.MaxLineNumber)
            {
                validLine = false;
            }
            long hitsVal = 0;
            if (!string.IsNullOrEmpty(hits) && (!long.TryParse(hits, out hitsVal) || hitsVal < 0))
            {
                validLine = false;
            }
            else
            {
                lineInfo.Hits = (int)Math.Min(hitsVal, int.MaxValue);
            }
            if (condition != null && condition.Length != 0 && condition != "100%")
            {
                Match m = s_regexCoverage.Match(condition);
                if (!m.Success
                    || !ushort.TryParse(m.Groups[1].Value, out lineInfo.CoveredBranches)
                    || !ushort.TryParse(m.Groups[2].Value, out lineInfo.TotalBranches))
                {
                    validLine = false;
                }
            }

            XElement line = new("line");
            if (number != null)
            {
                line.SetAttributeValue("number", number);
            }
            if (hits != null)
            {
                line.SetAttributeValue("hits", hits);
            }
            branch ??= condition != null;
            line.SetAttributeValue("branch", condition != null || branch.Value
                ? (TrueFalseLowerCase ? "true" : "True")
                : (TrueFalseLowerCase ? "false" : "False"));
            if (condition != null)
            {
                line.SetAttributeValue("condition-coverage", condition);
            }
            if (ConditionsSubtree && branch.Value)
            {
                LineInfo branchInfo = lineInfo;
                if (branchInfo.TotalBranches == 0)
                {
                    branchInfo.CoveredBranches = 2;
                    branchInfo.TotalBranches = 4;
                }
                string percent = $"{(int)Math.Round((double)branchInfo.CoveredBranches / branchInfo.TotalBranches * 100)}%";
                XElement conditions = new("conditions");
                line.Add(conditions);
                int n = branchInfo.TotalBranches / 2;
                for (int i = 0; i < n; i++)
                {
                    conditions.Add(new XElement("condition", new XAttribute("number", i), new XAttribute("type", "jump"), new XAttribute("coverage", percent)));
                }
            }

            XElement @class = LastClass();
            XElement? lines = @class.Element("lines");
            if (lines == null)
            {
                lines = new XElement("lines");
                @class.Add(lines);
            }
            lines.Add(line);
            if (!validLine)
            {
                FirstInvalidElement = line;
            }
            if (MethodLinesMatch && (lines = @class.Element("methods")?.Element("method")?.Element("lines")) != null)
            {
                lines.Add(line);
            }

            if (validLine && _lastClassFileNormalized != null)
            {
                SourceFileInfo sourceFileInfo = new(_lastClassFileNormalized, _reportTimestamp);
                sourceFileInfo.AddOrMergeLine(lineNumber, lineInfo);
                CoverageResult.AddOrMergeSourceFile(sourceFileInfo);
            }
            return this;
        }

        public CoberturaReportBuilder AddLine(int number, int hits, int coveredBranches = 0, int totalBranches = 0)
        {
            string? condition = null;
            if (totalBranches != 0)
            {
                int percent = (int)Math.Round((double)coveredBranches / totalBranches * 100);
                condition = $"{percent}% ({coveredBranches}/{totalBranches})";
            }
            return AddLine(number.ToString(), hits.ToString(), condition: condition);
        }

        public CoberturaReportBuilder FinalizeClass()
        {
            if (_lastClass != null)
            {
                XElement? lines = _lastClass.Element("lines");
                if (lines == null)
                {
                    FirstInvalidElement = _lastClass;
                }
                else if (lines.Element("line") == null)
                {
                    // TODO Is empty <lines> still an error? As opposed to missing <lines>, which is.
                    //FirstInvalidElement = lines;
                }
                _lastClass = null;
                _lastClassFileNormalized = null;
            }
            return this;
        }

        public CoberturaReportBuilder Finalize()
        {
            FinalizeClass();
            // Report may not really be finalized here since a possible Save() will update the timestamps.
            return this;
        }

        public CoberturaReportBuilder Save()
        {
            Finalize();
            if (ReportFilePath == null)
            {
                throw new InvalidOperationException("No report file is associated with this CoberturaReportBuilder.");
            }
            if (XDeclaration != null || XDocumentType != null)
            {
                new XDocument(XDeclaration, XDocumentType!, Root).Save(ReportFilePath);
            }
            else
            {
                File.WriteAllText(ReportFilePath, Root.ToString());
            }
            // Update timestamps
            _reportTimestamp = File.GetLastWriteTimeUtc(ReportFilePath);
            foreach (SourceFileInfo info in CoverageResult.SourceFiles.Values)
            {
                info.ReportTimestamp = _reportTimestamp;
            }
            return this;
        }

        private XElement Sources()
        {
            if (_sources == null)
            {
                _sources = new("sources");
                Root.AddFirst(_sources);
            }
            return _sources;
        }

        private XElement Packages()
        {
            if (_packages == null)
            {
                _packages = new("packages");
                Root.Add(_packages);
            }
            return _packages;
        }

        private XElement LastClass()
        {
            // Intentionally allowing NullReferenceException
            return _lastClass!;
        }

        private XElement LastClassParent()
        {
            if (_lastClass != null)
            {
                return _lastClass.Parent!;
            }
            if (ClassBelowPackages)
            {
                return Packages();
            }

            XElement? package = Packages().Elements().LastOrDefault();
            if (package == null)
            {
                AddPackage();
                package = Packages().Elements().Last();
            }
            XElement? classes = package.Element("classes");
            if (classes == null)
            {
                classes = new("classes");
                package.Add(classes);
            }
            return classes;
        }

        [return: NotNullIfNotNull(nameof(path))]
        private static string? Normalize(string? path)
        {
            if (path == null)
            {
                return null;
            }
            char replace = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            return path.Replace(replace, Path.DirectorySeparatorChar);
        }
    }
}
