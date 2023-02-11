// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Mawosoft.MissingCoverage.Tests
{

    internal static class OptionsTestHelper
    {
        public enum PropertySelector
        {
            All,
            OptionValueT,
            Switch,
            NotHelpOrGlobs,
            NotSwitchOrGlobs,
        }

        [SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.",
            Justification = "Consistency and number may become >1 in the future.")]
        // Track any changes. Call this anywhere we make assumptions about member fields and properties.
        // Returns list of all instance fields and properties, public or not.
        public static void AssertOptionsMembers()
        {
            List<FieldInfo> fields = new(typeof(Options)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            List<PropertyInfo> properties = new(typeof(Options)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public));
            Assert.Equal(10, fields.Count);
            Assert.Equal(fields.Count, properties.Count);
            Assert.Equal(8, properties.FindAll(p => OptionsPropertyIs(p, PropertySelector.OptionValueT)).Count);
            Assert.Equal(1, properties.FindAll(p => !p.PropertyType.IsValueType).Count);
            PropertyInfo p = Assert.Single(properties.FindAll(p => p.Name == "GlobPatterns"));
            Assert.Equal(typeof(List<string>), p.PropertyType);
        }

        public static void AssertOptionsEqual(Options expected, Options actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);
            AssertOptionsMembers();
            IEnumerable<PropertyInfo> properties = GetOptionsProperties(PropertySelector.All);
            foreach (PropertyInfo prop in properties)
            {
                if (prop.PropertyType.IsValueType)
                {
                    Assert.Equal((prop.Name, prop.GetValue(expected)), (prop.Name, prop.GetValue(actual)));
                }
                else
                {
                    List<string> expectedList = Assert.IsType<List<string>>(prop.GetValue(expected));
                    List<string> actualList = Assert.IsType<List<string>>(prop.GetValue(actual));
                    // While Assert.Equal() properly compares the list items, the mismatch reporting sucks.
                    // The lists here aren't supposed to contain many items, thus compare as joined string
                    // to get better reporting. For a more versatile solution see:
                    // https://github.com/dotnet/runtime/blob/main/src/libraries/Common/tests/TestUtilities/System/AssertExtensions.cs
                    Assert.Equal(string.Join(' ', expectedList), string.Join(' ', actualList));
                }
            }
        }

        public static bool OptionsPropertyIs(PropertyInfo property, PropertySelector selector)
        {
            Assert.NotNull(property);
            Assert.Equal(typeof(Options), property.DeclaringType);
            return selector switch
            {
                PropertySelector.All => true,
                PropertySelector.OptionValueT
                    => property.PropertyType.IsGenericType
                       && property.PropertyType.GetGenericTypeDefinition() == typeof(OptionValue<>),
                PropertySelector.Switch
                    => property.PropertyType == typeof(bool)
                       || property.PropertyType == typeof(OptionValue<bool>),
                PropertySelector.NotHelpOrGlobs
                    => property.Name is not (nameof(Options.ShowHelpOnly) or nameof(Options.GlobPatterns)),
                PropertySelector.NotSwitchOrGlobs
                => property.PropertyType != typeof(bool)
                   && property.PropertyType != typeof(OptionValue<bool>)
                   && property.Name != nameof(Options.GlobPatterns),
                _ => false,
            };
        }

        public static bool OptionsPropertyIs(string property, PropertySelector selector)
            => OptionsPropertyIs(typeof(Options).GetProperty(property)!, selector);

        public static IEnumerable<PropertyInfo> GetOptionsProperties(PropertySelector selector)
            => typeof(Options).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                              .Where(p => OptionsPropertyIs(p, selector));

        public static IEnumerable<string> GetOptionsPropertyNames(PropertySelector selector)
            => typeof(Options).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                              .Where(p => OptionsPropertyIs(p, selector))
                              .Select(p => p.Name);

        public static IEnumerable<object[]> GetOptionsPropertyNamesAsTestData(PropertySelector selector)
            => GetOptionsPropertyNames(selector).Select(p => new object[] { p });

        public static string[] SplitArguments(string argumentLine)
            => argumentLine.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

        public static string ToMixedCase(string s, int start = 1)
        {
            Span<char> chars = s.ToLowerInvariant().ToCharArray();
            if (chars.Length > 0 && chars.Length <= start)
            {
                start = chars.Length - 1;
            }
            for (int i = start; i < chars.Length; i += 2)
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
            }
            return new string(chars);
        }
    }
}
