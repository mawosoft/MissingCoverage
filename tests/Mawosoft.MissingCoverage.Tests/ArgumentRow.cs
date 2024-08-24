// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests;

internal class ArgumentRow
{
    private readonly List<ArgumentCell> _cells;
    public IReadOnlyList<ArgumentCell> Cells => _cells;
    public bool HasUnknownOption => _cells.Any(c => c.IsUnknown);
    public bool HasInvalidValue => _cells.Any(c => c.IsInvalid);

    public ArgumentRow(IEnumerable<ArgumentCell> cells) => _cells = cells.ToList();

    public ArgumentRow(ArgumentCell cell) => (_cells = []).Add(cell);

    public override string ToString() => string.Join(' ', ToArguments());

    public string[] ToArguments() => _cells.SelectMany(c => c.ToArguments()).ToArray();

    public Options ToOptions()
    {
        Options options = new();
        bool canUnion = true;
        bool globsOnly = false;
        foreach (ArgumentCell cell in _cells)
        {
            if (canUnion && !cell.IsEmpty)
            {
                if (cell.IsUnknown || cell.IsInvalid)
                {
                    canUnion = false;
                }
                else if (globsOnly)
                {
                    Options cellOptions = new();
                    cellOptions.GlobPatterns.AddRange(SplitArguments(cell.Text));
                    options.UnionWith(cellOptions);
                }
                else if (cell.IsDoubleHyphenTerminator)
                {
                    globsOnly = true;
                }
                else if (cell.PropertyName == nameof(Options.GlobPatterns))
                {
                    options.UnionWith(cell.ToOptions());
                }
                else
                {
                    // We have to union the other way around because a later duplicate has precedence,
                    // but UnionWith is intended for merging a secondary Options source like a settings
                    // file with the primary command line options.
                    Options cellOptions = cell.ToOptions();
                    cellOptions.UnionWith(options);
                    options = cellOptions;
                    canUnion = !options.ShowHelpOnly;
                }
            }
        }
        return options;
    }
}
