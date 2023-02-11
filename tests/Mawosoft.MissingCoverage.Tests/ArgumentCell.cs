// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Reflection;

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class ArgumentCell
    {
        public PropertyInfo? PropertyInfo { get; }
        public string PropertyName { get; }

        // Complete option with prefixed alias, delimiter, and option value.
        // Can be intentionally invalid, unknown, or empty.
        public string Text { get; }

        // Value of Options property. For switches, this is always 'true' as OptionValue<bool> or bool.
        // For Globpatterns, this is the same as Text. For invalid or unknown options, this is null.
        // Also Null for speciality cases, like -- terminator or empty cell.
        public object? Value { get; }

        public bool IsEmpty => Value == null && Text.Length == 0;
        public bool IsDoubleHyphenTerminator => PropertyName == "--";
        public bool IsUnknown => PropertyInfo == null && !(IsEmpty || IsDoubleHyphenTerminator);
        public bool IsInvalid => Value == null && !(IsEmpty || IsDoubleHyphenTerminator);

        public ArgumentCell(PropertyInfo? propertyInfo, string propertyName, string text, object? value)
        {
            PropertyInfo = propertyInfo;
            PropertyName = propertyName;
            Text = text;
            Value = value;
        }

        public ArgumentCell(string text, object? value)
        {
            PropertyInfo = null;
            PropertyName = text == "--" ? text : string.Empty;
            Text = text;
            Value = value;
        }

        public override string ToString() => Text;

        public string[] ToArguments() => SplitArguments(Text);

        public Options ToOptions()
        {
            Options options = new();
            if (PropertyInfo != null && Value != null)
            {
                if (Value is string glob && PropertyInfo.GetValue(options) is List<string> globs)
                {
                    globs.Add(glob);
                }
                else
                {
                    PropertyInfo.SetValue(options, Value);
                }
            }
            return options;
        }
    }
}
