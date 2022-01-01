// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    public class OptionsTests
    {
        [Fact]
        public void Ctor_Defaults()
        {
            AssertOptionsMembers();
            // Track changed defaults
            Options options = new();
            Assert.False(options.ShowHelpOnly);
            Assert.Equal<int>(1, options.HitThreshold);
            Assert.Equal<int>(100, options.CoverageThreshold);
            Assert.Equal<int>(2, options.BranchThreshold);
            Assert.False(options.LatestOnly);
            Assert.False(options.NoCollapse);
            Assert.Equal<int>(50_000, options.MaxLineNumber);
            Assert.False(options.NoLogo);
            Assert.Equal<VerbosityLevel>(VerbosityLevel.Normal, options.Verbosity);
            Assert.Empty(options.GlobPatterns);

            // Defaults must be initialized via ctor: OptionValue = new(value), not OptionValue = value.
            Assert.False(options.HitThreshold.IsSet);
            Assert.False(options.CoverageThreshold.IsSet);
            Assert.False(options.BranchThreshold.IsSet);
            Assert.False(options.LatestOnly.IsSet);
            Assert.False(options.NoCollapse.IsSet);
            Assert.False(options.MaxLineNumber.IsSet);
            Assert.False(options.NoLogo.IsSet);
            Assert.False(options.Verbosity.IsSet);
        }

        #region ParseCommandLineArguments

        [Fact]
        public void ParseCmdLine_NullOrEmptyArguments_ReturnsDefault()
        {
            Options expected = new();
            Options actual = new();
            actual.ParseCommandLineArguments(null!);
            AssertOptionsEqual(expected, actual);
            actual.ParseCommandLineArguments(Array.Empty<string>());
            AssertOptionsEqual(expected, actual);
        }

        [Fact]
        public void ParseCmdLine_Help_WithForwardSlash_OnlyAsFirstArgument()
        {
            Options expected = new() { ShowHelpOnly = true };
            Options actual = new();
            actual.ParseCommandLineArguments(SplitArguments("/? --nologo"));
            AssertOptionsEqual(expected, actual);

            expected = new() { NoLogo = true };
            expected.GlobPatterns.Add("/?");
            actual = new();
            actual.ParseCommandLineArguments(SplitArguments("--nologo /?"));
            AssertOptionsEqual(expected, actual);
        }

        [Fact]
        public void ParseCmdLine_Help_StopsParsing()
        {
            Options expected = new() { ShowHelpOnly = true, CoverageThreshold = 75 };
            expected.GlobPatterns.Add("glob1");
            Options actual = new();
            actual.ParseCommandLineArguments(SplitArguments("-ct 75 glob1 -h glob2 --nologo --invalid-option"));
            AssertOptionsEqual(expected, actual);
        }

        [Theory]
        #region InlineData
        [InlineData("0", VerbosityLevel.Quiet)]
        [InlineData("1", VerbosityLevel.Minimal)]
        [InlineData("2", VerbosityLevel.Normal)]
        [InlineData("3", VerbosityLevel.Detailed)]
        [InlineData("4", VerbosityLevel.Diagnostic)]
        [InlineData("q", VerbosityLevel.Quiet)]
        [InlineData("m", VerbosityLevel.Minimal)]
        [InlineData("n", VerbosityLevel.Normal)]
        [InlineData("d", VerbosityLevel.Detailed)]
        [InlineData("diag", VerbosityLevel.Diagnostic)]
        [InlineData("quiet", VerbosityLevel.Quiet)]
        [InlineData("minimal", VerbosityLevel.Minimal)]
        [InlineData("normal", VerbosityLevel.Normal)]
        [InlineData("detailed", VerbosityLevel.Detailed)]
        [InlineData("diagnostic", VerbosityLevel.Diagnostic)]
        #endregion
        internal void ParseCmdLine_Verbosity(string actualValue, VerbosityLevel expectedValue)
        {
            Options expected = new() { Verbosity = expectedValue };
            string[] args = { "-v", actualValue };
            Options actual = new();
            actual.ParseCommandLineArguments(args);
            AssertOptionsEqual(expected, actual);
            if (!char.IsDigit(actualValue[0]))
            {
                args[1] = actualValue.ToUpperInvariant();
                actual = new();
                actual.ParseCommandLineArguments(args);
                AssertOptionsEqual(expected, actual);
                args[1] = ToMixedCase(actualValue);
                actual = new();
                actual.ParseCommandLineArguments(args);
                AssertOptionsEqual(expected, actual);
            }
        }

        [Theory]
        [MemberData(nameof(ArgumentMatrix.CreateRowsAsTestData), "* ShowHelpOnly",
            ArgumentMutations.Casing | ArgumentMutations.Alias,
            DisableDiscoveryEnumeration = true, MemberType = typeof(ArgumentMatrix))]
        internal void ParseCmdLine_SupportedAliases_CaseInsensitive(ArgumentRow argumentRow)
        {
            Options actual = new();
            actual.ParseCommandLineArguments(argumentRow.ToArguments());
            AssertOptionsEqual(argumentRow.ToOptions(), actual);
        }

        [Theory]
        [MemberData(nameof(ArgumentMatrix.CreateRowsAsTestData), "* ShowHelpOnly",
            ArgumentMutations.Hyphen | ArgumentMutations.Alias,
            DisableDiscoveryEnumeration = true, MemberType = typeof(ArgumentMatrix))]
        internal void ParseCmdLine_SupportedPrefixes(ArgumentRow argumentRow)
        {
            Options actual = new();
            actual.ParseCommandLineArguments(argumentRow.ToArguments());
            AssertOptionsEqual(argumentRow.ToOptions(), actual);
        }

        [Theory]
        [MemberData(nameof(ArgumentMatrix.CreateRowsAsTestData), "*=",
            ArgumentMutations.Delimiter,
            DisableDiscoveryEnumeration = true, MemberType = typeof(ArgumentMatrix))]
        internal void ParseCmdLine_SupportedDelimiters(ArgumentRow argumentRow)
        {
            Options actual = new();
            actual.ParseCommandLineArguments(argumentRow.ToArguments());
            AssertOptionsEqual(argumentRow.ToOptions(), actual);
        }

        [Theory]
        [MemberData(nameof(ArgumentMatrix.CreateRowsAsTestData), "*=",
            ArgumentMutations.ValidValues,
            DisableDiscoveryEnumeration = true, MemberType = typeof(ArgumentMatrix))]
        internal void ParseCmdLine_ValidValues(ArgumentRow argumentRow)
        {
            Options actual = new();
            actual.ParseCommandLineArguments(argumentRow.ToArguments());
            AssertOptionsEqual(argumentRow.ToOptions(), actual);
        }

        [Theory]
        [MemberData(nameof(GetOptionsPropertyNamesAsTestData), PropertySelector.NotSwitchOrGlobs,
            DisableDiscoveryEnumeration = true, MemberType = typeof(OptionsTestHelper))]
        public void ParseCmdLine_DuplicateOption_UsesLast(string propertyName)
        {
            ArgumentColumn column = ArgumentColumn.Create(propertyName, ArgumentMutations.ValidValues);
            Assert.False(column.Cells[^1].IsEmpty, "ArgumentColumn should not contain empty cells.");
            Assert.NotEqual(column.Cells[0].Value, column.Cells[^1].Value);
            ArgumentRow argumentRow = new(column.Cells);
            Options actual = new();
            actual.ParseCommandLineArguments(argumentRow.ToArguments());
            AssertOptionsEqual(argumentRow.ToOptions(), actual);
        }

        private static IEnumerable<ArgumentMatrix> MutateMatrix(ArgumentColumn sourceColumn)
        {
            ArgumentMatrix matrix;
            ArgumentColumn filler1 = ArgumentColumn
                .Create("NoCollapse", ArgumentMutations.ValidValues | ArgumentMutations.RepeatOnResize);
            ArgumentColumn filler2 = ArgumentColumn
                .Create("NoLogo", ArgumentMutations.ValidValues | ArgumentMutations.RepeatOnResize);
            matrix = new();
            matrix.AddColumn(sourceColumn);
            yield return matrix;
            matrix = new();
            matrix.AddColumn(sourceColumn);
            matrix.AddColumn(filler2);
            matrix.AddColumn(filler1);
            yield return matrix;
            matrix = new();
            matrix.AddColumn(filler1);
            matrix.AddColumn(sourceColumn);
            matrix.AddColumn(filler2);
            yield return matrix;
            matrix = new();
            matrix.AddColumn(filler1);
            matrix.AddColumn(filler2);
            matrix.AddColumn(sourceColumn);
            yield return matrix;
        }

        [Theory]
        [MemberData(nameof(GetOptionsPropertyNamesAsTestData), PropertySelector.NotSwitchOrGlobs,
            DisableDiscoveryEnumeration = true, MemberType = typeof(OptionsTestHelper))]
        public void ParseCmdLine_InvalidOption_Throws(string propertyName)
        {
            ArgumentColumn column = ArgumentColumn
                .Create(propertyName, ArgumentMutations.InvalidValues | ArgumentMutations.Delimiter);
            foreach (ArgumentMatrix matrix in MutateMatrix(column))
            {
                int rowIndex = -1;
                foreach (ArgumentRow row in matrix.Rows)
                {
                    rowIndex++;
                    Options actual = new();
                    ArgumentException ex = Assert.Throws<ArgumentException>(
                        () => actual.ParseCommandLineArguments(row.ToArguments()));
                    Assert.Contains("Invalid command line argument " + column.Cells[rowIndex].Text,
                                    ex.Message);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetOptionsPropertyNamesAsTestData), PropertySelector.NotSwitchOrGlobs,
            DisableDiscoveryEnumeration = true, MemberType = typeof(OptionsTestHelper))]
        public void ParseCmdLine_MissingOptionValue_Throws(string propertyName)
        {
            ArgumentColumn column = ArgumentColumn
                .Create(propertyName, ArgumentMutations.MissingValue | ArgumentMutations.Delimiter);
            foreach (ArgumentMatrix matrix in MutateMatrix(column))
            {
                int rowIndex = -1;
                string next = matrix.Columns.Last() == column ? string.Empty : " -nologo";
                foreach (ArgumentRow row in matrix.Rows)
                {
                    rowIndex++;
                    Options actual = new();
                    ArgumentException ex = Assert.Throws<ArgumentException>(
                        () => actual.ParseCommandLineArguments(row.ToArguments()));
                    Assert.Contains("Invalid command line argument " + column.Cells[rowIndex].Text + next,
                                    ex.Message);
                }
            }
        }

        [Fact]
        public void ParseCmdLine_UnknownOption_Throws()
        {
            // ArgumentColumn can only decorate known aliases. Instead of extending ArgumentColumn, we use
            // HitThreshold (-ht) to create the decorations and replace the alias in each cell with an
            // unknown one.
            ArgumentColumn column = ArgumentColumn
                .Create(nameof(Options.HitThreshold), ArgumentMutations.Hyphen | ArgumentMutations.Delimiter);
            List<ArgumentCell> cells = new(column.Cells.Count);
            foreach (ArgumentCell cell in column.Cells)
            {
                cells.Add(new ArgumentCell(cell.Text.Replace("ht", "foo", StringComparison.Ordinal), null));
            }
            column = new(cells);
            foreach (ArgumentMatrix matrix in MutateMatrix(column))
            {
                int rowIndex = -1;
                foreach (ArgumentRow row in matrix.Rows)
                {
                    rowIndex++;
                    Options actual = new();
                    ArgumentException ex = Assert.Throws<ArgumentException>(
                        () => actual.ParseCommandLineArguments(row.ToArguments()));
                    string text = column.Cells[rowIndex].Text;
                    int pos = text.IndexOf(' ');
                    if (pos >= 0) text = text.Substring(0, pos);
                    Assert.Contains("Unknown command line argument " + text, ex.Message);
                }
            }
        }

        [Theory]
        [InlineData("glob1 glob2", "glob1 glob2")]
        [InlineData("glob1 Glob1", "glob1 Glob1")]
        [InlineData("glob1 glob2 glob1", "glob1 glob2")]
        public void ParseCmdLine_GlobPatterns_SkipsDuplicatesCasesensitive(string argumentGlobs,
                                                                           string expectedGlobs)
        {
            Options expected = new();
            Options actual = new();
            expected.GlobPatterns.AddRange(SplitArguments(expectedGlobs));
            actual.ParseCommandLineArguments(SplitArguments(argumentGlobs));
            AssertOptionsEqual(expected, actual);
        }

        [Fact]
        public void ParseCmdLine_GlobPatterns_CanMixWithOptions()
        {
            PropertyInfo globInfo = typeof(Options).GetProperty(nameof(Options.GlobPatterns))!;
            Assert.NotNull(globInfo);
            ArgumentMatrix source =
                ArgumentMatrix.Create("*",
                    ArgumentMutations.Hyphen | ArgumentMutations.Delimiter | ArgumentMutations.RepeatOnResize);
            ArgumentMatrix matrix = new();
            int id = 1;
            foreach (ArgumentColumn column in source.Columns)
            {
                string glob = $"{(id % 2 == 0 ? "/" : "")}foo/bar{id++}";
                matrix.AddColumn(column);
                matrix.AddColumn
                    (new ArgumentColumn(
                        new ArgumentCell(globInfo, globInfo.Name, glob, glob))
                    {
                        RepeatOnResize = true
                    });
            }
            foreach (ArgumentRow row in matrix.Rows)
            {
                Options actual = new();
                actual.ParseCommandLineArguments(row.ToArguments());
                AssertOptionsEqual(row.ToOptions(), actual);
            }
        }

        [Fact]
        public void ParseCmdLine_GlobPatterns_EveryThingAfterSoloDoubleHyphen()
        {
            string cmdLinePart1 = "-ct 75 glob1 --no-collapse";
            Options expected = new() { CoverageThreshold = 75, NoCollapse = true };
            expected.GlobPatterns.Add("glob1");
            string cmdLinePart2 = "glob2 --nologo --invalid-option";
            expected.GlobPatterns.AddRange(SplitArguments(cmdLinePart2));
            Options actual = new();
            actual.ParseCommandLineArguments(SplitArguments(cmdLinePart1 + " -- " + cmdLinePart2));
            AssertOptionsEqual(expected, actual);
        }

        #endregion

        [Theory]
        [MemberData(nameof(GetOptionsPropertyNamesAsTestData), PropertySelector.All,
            DisableDiscoveryEnumeration = true, MemberType = typeof(OptionsTestHelper))]
        public void Clone_Succeeds(string propertyName)
        {
            PropertyInfo property = typeof(Options)
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!;
            Assert.NotNull(property);
            // Paranoia keeps source and expected separated.
            Options source = new();
            Options expected = new();
            // We can't use "case typeof(X):", but fortunately we don't have any nullable reference
            // types here, thus pattern matching will work on all properties.
            // If that changes, we can add "case null when propInfo.PropertyType == typeof(X):".
            switch (property.GetValue(source))
            {
                case bool:
                    property.SetValue(source, true);
                    property.SetValue(expected, true);
                    break;
                case OptionValue<bool>:
                    property.SetValue(source, (OptionValue<bool>)true);
                    property.SetValue(expected, (OptionValue<bool>)true);
                    break;
                case OptionValue<int>:
                    property.SetValue(source, (OptionValue<int>)10);
                    property.SetValue(expected, (OptionValue<int>)10);
                    break;
                case OptionValue<VerbosityLevel>:
                    property.SetValue(source, (OptionValue<VerbosityLevel>)VerbosityLevel.Detailed);
                    property.SetValue(expected, (OptionValue<VerbosityLevel>)VerbosityLevel.Detailed);
                    break;
                case List<string>:
                    Assert.Equal(nameof(Options.GlobPatterns), property.Name);
                    source.GlobPatterns.AddRange(new[] { "glob1", "glob2" });
                    expected.GlobPatterns.AddRange(new[] { "glob1", "glob2" });
                    break;
                default:
                    // Unexpected type; this gives the full type name
                    Assert.Null(property.PropertyType);
                    break;
            }
            Options actual = source.Clone();
            AssertOptionsEqual(expected, actual);
        }

        [Fact]
        public void UnionWith_IgnoresShowHelpOnly()
        {
            Options target = new();
            Options source = new();
            foreach ((bool t, bool s) in new[] { (true, false), (false, true) })
            {
                Assert.NotEqual(t, s);
                target.ShowHelpOnly = t;
                source.ShowHelpOnly = s;
                target.UnionWith(source);
                Assert.Equal(t, target.ShowHelpOnly);
            }
        }

        [Theory]
        [InlineData("", "glob1 glob2", "glob1 glob2")]
        [InlineData("glob1 glob2", "glob1 glob3", "glob1 glob2 glob3")]
        [InlineData("glob1 glob2", "Glob1 glob2", "glob1 glob2 Glob1")]
        public void UnionWith_MergesGlobPatterns_Casesensitive(string targetGlobs,
                                                               string sourceGlobs,
                                                               string expectedGlobs)
        {
            Options target = new();
            Options source = new();
            Options expected = new();
            target.GlobPatterns.AddRange(SplitArguments(targetGlobs));
            source.GlobPatterns.AddRange(SplitArguments(sourceGlobs));
            expected.GlobPatterns.AddRange(SplitArguments(expectedGlobs));
            target.UnionWith(source);
            AssertOptionsEqual(expected, target);
        }

        [Theory]
        [MemberData(nameof(GetOptionsPropertyNamesAsTestData), PropertySelector.OptionValueT,
            DisableDiscoveryEnumeration = true, MemberType = typeof(OptionsTestHelper))]
        public void UnionWith_OptionValue_HonorsIsSet(string propertyName)
        {
            PropertyInfo property = typeof(Options)
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!;
            Assert.NotNull(property);
            Options source = new();
            Options target = new();
            Options expected = new();
            foreach (bool targetIsSet in new[] { true, false })
                foreach (bool sourceIsSet in new[] { true, false })
                {
                    // See notes on switch in Clone_Succeeds().
                    switch (property.GetValue(source))
                    {
                        case OptionValue<bool>:
                            property.SetValue(source,
                                sourceIsSet ? (OptionValue<bool>)false : new OptionValue<bool>(false));
                            property.SetValue(target,
                                targetIsSet ? (OptionValue<bool>)true : new OptionValue<bool>(true));
                            property.SetValue(expected,
                                sourceIsSet && !targetIsSet
                                    ? property.GetValue(source)
                                    : property.GetValue(target));
                            break;
                        case OptionValue<int>:
                            property.SetValue(source,
                                sourceIsSet ? (OptionValue<int>)9 : new OptionValue<int>(9));
                            property.SetValue(target,
                                targetIsSet ? (OptionValue<int>)10 : new OptionValue<int>(10));
                            property.SetValue(expected,
                                sourceIsSet && !targetIsSet
                                    ? property.GetValue(source)
                                    : property.GetValue(target));
                            break;
                        case OptionValue<VerbosityLevel>:
                            property.SetValue(source,
                                sourceIsSet
                                    ? (OptionValue<VerbosityLevel>)VerbosityLevel.Detailed
                                    : new OptionValue<VerbosityLevel>(VerbosityLevel.Detailed));
                            property.SetValue(target,
                                targetIsSet
                                    ? (OptionValue<VerbosityLevel>)VerbosityLevel.Minimal
                                    : new OptionValue<VerbosityLevel>(VerbosityLevel.Minimal));
                            property.SetValue(expected,
                                sourceIsSet && !targetIsSet
                                    ? property.GetValue(source)
                                    : property.GetValue(target));
                            break;
                        default:
                            // Unexpected type; this gives the full type name
                            Assert.Null(property.PropertyType);
                            break;
                    }
                    target.UnionWith(source);
                    AssertOptionsEqual(expected, target);
                }
        }
    }
}
