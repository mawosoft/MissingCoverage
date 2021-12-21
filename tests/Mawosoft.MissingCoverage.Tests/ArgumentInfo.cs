// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Xunit;

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class ArgumentInfo
    {
        public static ArgumentInfo Infos(string propertyName) => s_argumentInfos[propertyName];

        public PropertyInfo PropertyInfo { get; }
        public string PropertyName => PropertyInfo.Name;
        public List<string> Aliases { get; }
        public List<(string text, object? value)> ValidValues { get; }
        public List<(string text, object? value)> InvalidValues { get; }
        public bool IsSwitch => OptionsPropertyIs(PropertyInfo, PropertySelector.Switch);

        public ArgumentInfo(string propertyName,
                            string aliases,
                            string validValues = "",
                            string invalidValues = "")
        {
            PropertyInfo = typeof(Options).GetProperty(propertyName)!;
            Assert.NotNull(PropertyInfo);
            Aliases = new(SplitArguments(aliases));
            InvalidValues = new(
                new List<string>(SplitArguments(invalidValues)).ConvertAll(s => (s, (object?)null)));
            ValidValues = new();
            string[] valid = SplitArguments(validValues);
            Options options = new();
            /// See notes on switch in <see cref="OptionsTests.Clone_Succeeds"/>
            switch (PropertyInfo.GetValue(options))
            {
                case bool:
                    Assert.Equal(string.Empty, validValues);
                    Assert.Equal(string.Empty, invalidValues);
                    ValidValues.Add(("", true));
                    break;
                case OptionValue<bool>:
                    Assert.Equal(string.Empty, validValues);
                    Assert.Equal(string.Empty, invalidValues);
                    ValidValues.Add(("", (OptionValue<bool>)true));
                    break;
                case OptionValue<int>:
                    ValidValues = SplitArguments(validValues.Length != 0 ? validValues : "0 1 21")
                        .Select(v => (v, (object?)(OptionValue<int>)int.Parse(v)))
                        .ToList();
                    if (InvalidValues.Count == 0)
                    {
                        InvalidValues.Add(("-1", null));
                        InvalidValues.Add(("12bad", null));
                    }
                    break;
                case OptionValue<VerbosityLevel>:
                    Assert.Equal(string.Empty, validValues);
                    ValidValues.Add(("1", (OptionValue<VerbosityLevel>)VerbosityLevel.Minimal));
                    ValidValues.Add(("quiet", (OptionValue<VerbosityLevel>)VerbosityLevel.Quiet));
                    ValidValues.Add(("n", (OptionValue<VerbosityLevel>)VerbosityLevel.Normal));
                    if (InvalidValues.Count == 0)
                    {
                        InvalidValues.Add(("7", null));
                        InvalidValues.Add(("foo", null));
                    }
                    break;
                case List<string>:
                    ValidValues = SplitArguments(validValues.Length != 0 ? validValues : "glob/**/pattern")
                        .Select(v => (v, (object?)v))
                        .ToList();
                    Assert.Equal(string.Empty, invalidValues);
                    break;
                default:
                    // Unexpected type; this gives the full type name
                    Assert.Null(PropertyInfo.PropertyType);
                    break;
            }
        }

        static ArgumentInfo()
        {
            AssertOptionsMembers();
            s_argumentInfos = new(new KeyValuePair<string, ArgumentInfo>[]
            {
                // Intentionally not using nameof() here. If the name changes, other things might have changed as well.
                new ArgumentInfo("HitThreshold", "ht hit-threshold hitthreshold").KVP,
                new ArgumentInfo("CoverageThreshold", "ct coverage-threshold coveragethreshold", "0 70 100", "101 -1 bad").KVP,
                new ArgumentInfo("BranchThreshold", "bt branch-threshold branchthreshold").KVP,
                new ArgumentInfo("LatestOnly", "lo latest-only latestonly").KVP,
                new ArgumentInfo("NoCollapse", "no-collapse nocollapse").KVP,
                new ArgumentInfo("MaxLineNumber", "max-linenumber maxlinenumber", "1 1000 2457", "0 -1 bad").KVP,
                new ArgumentInfo("NoLogo", "nologo no-logo").KVP,
                new ArgumentInfo("Verbosity", "v verbosity").KVP,
                // We provide a default here, but GlobPatterns should be handled separately
                new ArgumentInfo("GlobPatterns", "").KVP,
                // Putting help last (it stops parsing) in case a user simply uses all descriptors
                new ArgumentInfo("ShowHelpOnly", "h help ?").KVP,
            });

        }

        private KeyValuePair<string, ArgumentInfo> KVP => new(PropertyName, this);


        private static readonly Dictionary<string, ArgumentInfo> s_argumentInfos;
    }
}
