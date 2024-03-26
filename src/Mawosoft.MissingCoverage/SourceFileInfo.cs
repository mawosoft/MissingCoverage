// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage;

internal sealed class SourceFileInfo(string sourceFilePath, DateTime reportTimestamp)
{
    private static int s_defaultLineCount = 500;
    private static int s_maxLineNumber = 50_000;

    public static int DefaultLineCount
    {
        get => s_defaultLineCount;
        set => s_defaultLineCount = (value > 0)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(DefaultLineCount));
    }

    public static int MaxLineNumber
    {
        get => s_maxLineNumber;
        set => s_maxLineNumber = (value > 0)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxLineNumber));
    }

    private string _sourceFilePath = !string.IsNullOrWhiteSpace(sourceFilePath)
                        ? sourceFilePath
                        : throw new ArgumentException(null, nameof(sourceFilePath));
    private LineInfo[] _lines = new LineInfo[DefaultLineCount];

    public string SourceFilePath
    {
        get => _sourceFilePath;
        set => _sourceFilePath = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException(null, nameof(SourceFilePath));
    }

    public DateTime ReportTimestamp { get; internal set; } = reportTimestamp;
    public int LastLineNumber { get; private set; }
    public ref readonly LineInfo Line(int index) => ref _lines[index];

    public override string ToString()
        => $"{SourceFilePath} ({LastLineNumber}) [{ReportTimestamp}]";

    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Array boundary check.")]
    public void AddOrMergeLine(int lineNumber, LineInfo line)
    {
        if (lineNumber <= 0)
        {
            throw new IndexOutOfRangeException();
        }
        else if (lineNumber <= LastLineNumber)
        {
            if (_lines[lineNumber].IsLine)
            {
                _lines[lineNumber].Merge(line);
            }
            else
            {
                _lines[lineNumber] = line;
            }
        }
        else if (lineNumber > MaxLineNumber)
        {
            throw new IndexOutOfRangeException($"{nameof(lineNumber)} larger than {MaxLineNumber}.");
        }
        else if (line.IsLine)
        {
            LastLineNumber = lineNumber;
            if (lineNumber >= _lines.Length)
            {
                GrowLines(lineNumber);
            }
            _lines[lineNumber] = line;
        }
    }

    public void Merge(SourceFileInfo other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Debug.Assert(SourceFilePath == other.SourceFilePath, "SourceFilePath should be identical");
        if (other.LastLineNumber >= _lines.Length)
        {
            Array.Resize(ref _lines, other.LastLineNumber + 1);
        }
        for (int i = 1; i <= other.LastLineNumber; i++)
        {
            if (other._lines[i].IsLine) AddOrMergeLine(i, other._lines[i]);
        }
    }

    public IEnumerable<(int firstLine, int lastLine)> LineSequences()
    {
        int lineNumber = 1;
        while (lineNumber <= LastLineNumber)
        {
            while (lineNumber <= LastLineNumber && !_lines[lineNumber].IsLine) { lineNumber++; }
            if (lineNumber > LastLineNumber) break;
            int firstLine = lineNumber;
            LineInfo firstInfo = _lines[lineNumber++];
            while (lineNumber <= LastLineNumber && _lines[lineNumber].Equals(firstInfo)) { lineNumber++; }
            yield return (firstLine, lineNumber - 1);
        }
    }

    private void GrowLines(int minLineNumber)
    {
        int minCapacity = minLineNumber + 1;
        if (minCapacity <= _lines.Length) return;
        int newCapacity = _lines.Length + DefaultLineCount;
        // net60 has Array.MaxLength which is lower than int.MaxValue.
        if ((uint)newCapacity > int.MaxValue) newCapacity = int.MaxValue;
        if (newCapacity < minCapacity) newCapacity = minCapacity;
        Array.Resize(ref _lines, newCapacity);
    }
}
