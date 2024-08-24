// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.XPath;

namespace XmlBenchmarks
{
    internal class CoberturaFileInfo
    {
        public string FilePath { get; }
        public long Size { get; }
        public int LinesOfText { get; }
        public int Sources { get; }
        public int Packages { get; }
        public int Classes { get; }
        public bool ClassBelowPackages { get; }
        public int Methods { get; }
        // Note that in files merged by FineCodeCoverage (FCC), MethodLines can be greater than ClassLines,
        // because of duplicated lines in methods. Affected areas are constructors, properties, and nested
        // methods. The duplicates contain exactly the same attribute values.
        public int MethodLines { get; }
        public int ClassLines { get; }
        public int ClassLinesBranchTrue { get; }
        public int ClassLinesBranchFalse { get; }
        public int ClassLinesCoverage { get; }
        public int ClassLinesConditions { get; }

        public CoberturaFileInfo(string filePath)
        {
            FilePath = filePath;
            Size = new FileInfo(filePath).Length;
            LinesOfText = File.ReadAllLines(filePath).Length;
            XPathDocument doc = new(filePath);
            XPathNavigator navi = doc.CreateNavigator();
            Sources = navi.Select("/coverage/sources/source").Count;
            Packages = navi.Select("/coverage/packages/package").Count;
            string classPath = "/coverage/packages/class";
            Classes = navi.Select(classPath).Count;
            if (Classes != 0)
            {
                ClassBelowPackages = true;
            }
            else
            {
                classPath = "/coverage/packages/package/classes/class";
                Classes = navi.Select(classPath).Count;
            }
            Methods = navi.Select(classPath + "/methods/method").Count;
            MethodLines = navi.Select(classPath + "/methods/method/lines/line").Count;
            string classLinesPath = classPath + "/lines/line";
            ClassLines = navi.Select(classLinesPath).Count;
            // Note that branch="False/True" in coverlet and "false/true" in FCC merged.
            // The set of available XPath functions is limited, there is no lower-case(), for example.
            int branchTrue = 0, branchFalse = 0;
            foreach (XPathNavigator node in navi.Select(classLinesPath + "/@branch"))
            {
                switch (node.Value.ToLowerInvariant())
                {
                    case "false":
                        branchFalse++;
                        break;
                    case "true":
                        branchTrue++;
                        break;
                    default:
                        Debug.Assert(false, "branch should be 'true' or 'false'.");
                        break;
                }
            }
            ClassLinesBranchTrue = branchTrue;
            ClassLinesBranchFalse = branchFalse;
            ClassLinesCoverage = navi.Select(classLinesPath + "[@condition-coverage]").Count;
            ClassLinesConditions = navi.Select(classLinesPath + "/conditions/condition").Count;
        }

        public enum TablePaddingOptions
        {
            None, Pad, PadEvenly
        }

        public static string[][] ToTable(IEnumerable<CoberturaFileInfo> fileInfos,
                                         TablePaddingOptions tablePaddingOptions, CultureInfo? cultureInfo,
                                         string? numberFormat = "N0")
        {
            if (tablePaddingOptions < TablePaddingOptions.None || tablePaddingOptions > TablePaddingOptions.PadEvenly)
                throw new ArgumentOutOfRangeException(nameof(tablePaddingOptions));
            CoberturaFileInfo[] infos = fileInfos?.ToArray() ?? Array.Empty<CoberturaFileInfo>();
            if (infos.Length == 0)
                return Array.Empty<string[]>();
            PropertyInfo[] properties = typeof(CoberturaFileInfo).GetProperties();
            string[][] table = new string[properties.Length][];
            for (int i = 0; i < table.Length; i++)
            {
                PropertyInfo property = properties[i];
                string[] columns = new string[infos.Length + 1];
                table[i] = columns;
                columns[0] = property.Name;
                for (int k = 0; k < infos.Length; k++)
                {
                    object? value = property.GetValue(infos[k]);
                    if (value == null)
                    {
                        columns[k + 1] = string.Empty;
                    }
                    else
                    {
                        switch (property.Name)
                        {
                            case "FilePath":
                                columns[0] = "File";
                                columns[k + 1] = Path.GetFileNameWithoutExtension((string)value);
                                break;
                            case "Size":
                                double size = (long)value;
                                foreach (string unit in new[] { " B", " KB", " MB", " GB" })
                                {
                                    if (size < 1000)
                                    {
                                        columns[k + 1] = Math.Round(size).ToString(numberFormat, cultureInfo) + unit;
                                        break;
                                    }
                                    size /= 1024d;
                                }
                                break;
                            default:
                                columns[k + 1] = value switch
                                {
                                    IFormattable f => f.ToString(numberFormat, cultureInfo),
                                    IConvertible c => c.ToString(cultureInfo),
                                    _ => value.ToString() ?? string.Empty,
                                };
                                break;
                        }
                    }
                }

            }
            if (tablePaddingOptions != TablePaddingOptions.None)
            {
                int[] maxWidths = table[0].Select(
                    (_, columnIndex) => table.Max(row => row[columnIndex].Length)).ToArray();
                if (tablePaddingOptions == TablePaddingOptions.PadEvenly && maxWidths.Length > 2)
                {
                    int maxWidth0 = maxWidths[0];
                    maxWidths[0] = 0;
                    int maxMaxWidth = maxWidths.Max();
                    Array.Fill(maxWidths, maxMaxWidth);
                    maxWidths[0] = maxWidth0;
                }
                table = table.Select(row => row.Select((cell, columnIndex) => columnIndex == 0
                            ? cell.PadRight(maxWidths[columnIndex])
                            : cell.PadLeft(maxWidths[columnIndex])).ToArray()).ToArray();
            }
            return table;
        }

        public static string? ToMarkDown(IEnumerable<CoberturaFileInfo> fileInfos,
                                         TablePaddingOptions tablePaddingOptions, CultureInfo? cultureInfo,
                                         string? numberFormat = "N0")
        {
            string[][] table = ToTable(fileInfos, tablePaddingOptions, cultureInfo, numberFormat);
            if (table.Length == 0)
                return null;
            List<string> rows = table.Select(row => "| " + string.Join(" | ", row) + " |").ToList();
            string[] separatorRow = table[0].Select(
                cell => string.Concat(string.Empty.PadRight(cell.Length + 1, '-') + ":")).ToArray();
            separatorRow[0] = separatorRow[0].Substring(0, separatorRow[0].Length - 1) + " ";
            rows.Insert(1, "|" + string.Join("|", separatorRow) + "|");
            return string.Join(Environment.NewLine, rows);
        }
    }
}
