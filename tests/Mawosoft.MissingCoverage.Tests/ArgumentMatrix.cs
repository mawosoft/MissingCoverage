// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using static Mawosoft.MissingCoverage.Tests.OptionsTestHelper;

namespace Mawosoft.MissingCoverage.Tests;

internal class ArgumentMatrix
{
    private readonly List<ArgumentColumn> _columns = new();

    public int ColumnCount => _columns.Count;
    public int RowCount => _columns.FirstOrDefault()?.Cells.Count ?? 0;
    public IEnumerable<ArgumentColumn> Columns => _columns;
    public IEnumerable<ArgumentRow> Rows
    {
        get
        {
            int n = RowCount;
            for (int i = 0; i < n; i++)
            {
                yield return GetRow(i);
            }
        }
    }

    public ArgumentRow GetRow(int index) => new(_columns.Select(c => c.Cells[index]));

    public void AddColumn(ArgumentColumn column) => InsertColumn(ColumnCount, column);

    public void AddColumnRange(IEnumerable<ArgumentColumn> columns) => InsertColumnRange(ColumnCount, columns);

    public void InsertColumn(int index, ArgumentColumn column)
    {
        int currentMax = ColumnCount > 0 ? _columns.Max(c => c.Cells.Count) : 0;
        int newCount = column.Cells.Count;
        if (newCount > currentMax)
        {
            _columns.ForEach(c => c.ResizeRows(newCount));
        }
        else
        {
            column.ResizeRows(currentMax);
        }
        _columns.Insert(index, column);
    }

    public void InsertColumnRange(int index, IEnumerable<ArgumentColumn> columns)
    {
        List<ArgumentColumn> columnList = new(columns);
        int currentMax = ColumnCount > 0 ? _columns.Max(c => c.Cells.Count) : 0;
        int newMax = columnList.Count > 0 ? columnList.Max(c => c.Cells.Count) : 0;
        if (newMax > currentMax)
        {
            _columns.ForEach(c => c.ResizeRows(newMax));
            columnList.ForEach(c => c.ResizeRows(newMax));
        }
        else
        {
            columnList.ForEach(c => c.ResizeRows(currentMax));
        }
        columnList.ForEach(c => c.ResizeRows(newMax));
        _columns.InsertRange(index, columnList);
    }

    /// <remarks>
    /// Use property names or special cases:
    /// <code>
    /// *  all unused properties except ShowHelpOnly and GlobPatterns
    /// *= all unused properties with values except GlobPatterns
    /// ** all unused properties
    /// -- options terminator
    /// </code>
    /// </remarks>
    public static ArgumentMatrix Create(string propertyNamesSpaceSeparated, ArgumentMutations mutations)
    {
        ArgumentMatrix matrix = new();
        string[] propertyNames = SplitArguments(propertyNamesSpaceSeparated);
        if (propertyNames.Length == 0) propertyNames = new[] { "*" };
        List<string> unusedPropertyNames = new(GetOptionsPropertyNames(PropertySelector.All));
        foreach (string propertyName in propertyNames)
        {
            switch (propertyName)
            {
                case "*":
                    AddColumns(GetOptionsPropertyNames(PropertySelector.NotHelpOrGlobs)
                        .Intersect(unusedPropertyNames));
                    break;
                case "*=":
                    AddColumns(GetOptionsPropertyNames(PropertySelector.NotSwitchOrGlobs)
                        .Intersect(unusedPropertyNames));
                    break;
                case "**":
                    AddColumns(unusedPropertyNames);
                    break;
                case "--":
                    matrix.AddColumn(new ArgumentColumn(new ArgumentCell("--", null)));
                    break;
                default:
                    AddColumns(new[] { propertyName });
                    break;
            }
        }
        return matrix;

        void AddColumns(IEnumerable<string> propertyNames)
        {
            // Defensive copy to avoid "collection was modified" exception
            List<string> names = new(propertyNames);
            foreach (string name in names)
            {
                matrix.AddColumn(
                    ArgumentColumn.Create(ArgumentInfo.Infos(name).PropertyInfo, mutations));
                unusedPropertyNames.Remove(name);
            }
        }
    }

    public static IEnumerable<object[]> CreateRowsAsTestData(string propertyNamesSpaceSeparated,
                                                             ArgumentMutations mutations)
        => Create(propertyNamesSpaceSeparated, mutations).Rows.Select(r => new object[] { r });

    public static IEnumerable<object[]> CreateColumnsAsTestData(string propertyNamesSpaceSeparated,
                                                                ArgumentMutations mutations)
        => Create(propertyNamesSpaceSeparated, mutations).Columns.Select(c => new object[] { c });
}
