// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests
{
    internal class ArgumentColumn
    {
        private List<ArgumentCell> _cells;

        public PropertyInfo? PropertyInfo => _cells.FirstOrDefault()?.PropertyInfo;
        public string? PropertyName => PropertyInfo?.Name;
        public IReadOnlyList<ArgumentCell> Cells => _cells;
        public bool RepeatOnResize { get; set; } = true;

        public ArgumentColumn(IEnumerable<ArgumentCell> cells) => _cells = new(cells);

        public ArgumentColumn(ArgumentCell cell) => (_cells = new()).Add(cell);

        public void ResizeRows(int count)
        {
            if (count != _cells.Count)
            {
                if (RepeatOnResize)
                {
                    _cells = RingOfRows().Take(count).ToList();
                }
                else
                {
                    _cells = FillWithEmpty().Take(count).ToList();
                }
            }

            IEnumerable<ArgumentCell> RingOfRows()
            {
                using RingEnumerator<ArgumentCell> enumerator = new(_cells);
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }

            IEnumerable<ArgumentCell> FillWithEmpty()
            {
                using IEnumerator<ArgumentCell> enumerator = _cells.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
                while (true)
                {
                    yield return new ArgumentCell("", null);
                }
            }
        }

        public static ArgumentColumn Create(string propertyName, ArgumentMutations mutations) => Create(
            typeof(Options).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!, mutations);

        public static ArgumentColumn Create(PropertyInfo propertyInfo, ArgumentMutations mutations)
        {
            ArgumentInfo argInfo = ArgumentInfo.Infos(propertyInfo.Name);
            string[] oneEmptyString = new[] { "" };

            IEnumerable<string> aliases = argInfo.Aliases.Count != 0
                ? (mutations.HasFlag(ArgumentMutations.Alias)
                    ? argInfo.Aliases : argInfo.Aliases.Take(1))
                : oneEmptyString;

            IEnumerable<string> hyphens = argInfo.Aliases.Count != 0
                ? (mutations.HasFlag(ArgumentMutations.Hyphen)
                    ? new[] { "-", "--" } : new[] { "-" })
                : oneEmptyString;

            int casingCount = mutations.HasFlag(ArgumentMutations.Casing) ? 3 : 1;

            // Use full permutations for hyphen, alias, and casing
            List<string> prefixedAliases = new();
            foreach (string alias in aliases)
            {
                if (alias.Length == 0)
                {
                    prefixedAliases.Add(alias);
                }
                else
                {
                    foreach (string hyphen in hyphens)
                    {
                        for (int i = 0; i < casingCount; i++)
                        {
                            switch (i)
                            {
                                case 0:
                                    prefixedAliases.Add(hyphen + alias);
                                    break;
                                case 1:
                                    prefixedAliases.Add(hyphen + alias.ToUpperInvariant());
                                    break;
                                case 2:
                                    prefixedAliases.Add(hyphen + ToMixedCase(alias));
                                    break;
                            }
                        }
                    }
                }
            }

            IEnumerable<string> delimiters = argInfo.Aliases.Count != 0 && !argInfo.IsSwitch
                ? (mutations.HasFlag(ArgumentMutations.Delimiter)
                    ? new[] { " ", ":", "=", ": ", "= " } : new[] { " " })
                : oneEmptyString;

            List<(string, object?)> valuesAndTexts = new();
            if (mutations.HasFlag(ArgumentMutations.InvalidValues) && !argInfo.IsSwitch)
            {
                valuesAndTexts.AddRange(argInfo.InvalidValues);
            }
            if (mutations.HasFlag(ArgumentMutations.MissingValue) && !argInfo.IsSwitch)
            {
                valuesAndTexts.Add(("", null));
            }
            if (mutations.HasFlag(ArgumentMutations.ValidValues))
            {
                valuesAndTexts.AddRange(argInfo.ValidValues);
            }
            if (valuesAndTexts.Count == 0)
            {
                valuesAndTexts.Add(argInfo.ValidValues[0]);
            }

            int maxMutations =
                new[] { prefixedAliases.Count, delimiters.Count(), valuesAndTexts.Count }.Max();

            List<ArgumentCell> cells = new();
            RingEnumerator<string> aliasEnumerator = new(prefixedAliases);
            RingEnumerator<string> delimiterEnumerator = new(delimiters);
            RingEnumerator<(string, object?)> valueAndTextEnumerator = new(valuesAndTexts);

            for (int i = 0; i < maxMutations; i++)
            {
                aliasEnumerator.MoveNext();
                delimiterEnumerator.MoveNext();
                valueAndTextEnumerator.MoveNext();
                (string text, object? value) = valueAndTextEnumerator.Current;
                string alias = aliasEnumerator.Current;
                if (alias.Length != 0 && !argInfo.IsSwitch)
                {
                    alias += delimiterEnumerator.Current;
                }
                // TrimEnd for space or space-ending delimiter + empty (missing) value text
                cells.Add(new ArgumentCell(propertyInfo, propertyInfo.Name, (alias + text).TrimEnd(), value));

            }

            return new ArgumentColumn(cells)
            {
                RepeatOnResize = mutations.HasFlag(ArgumentMutations.RepeatOnResize)
            };
        }

    }
}
