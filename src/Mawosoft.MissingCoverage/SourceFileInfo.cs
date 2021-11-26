// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Diagnostics;

namespace Mawosoft.MissingCoverage
{
    internal class SourceFileInfo
    {
        public static int DefaultLineCount { get; set; } = 500;
        public static int MaxLineNumber { get; set; } = 50_000;
        public string SourceFilePath { get; set; }
        public DateTime ReportTimestamp { get; }
        public int LastLineNumber { get; private set; }

        private LineInfo[] _lines;
        public ref LineInfo this[int index] => ref _lines[index];

        public SourceFileInfo(string sourceFilePath, DateTime reportTimestamp)
        {
            SourceFilePath = sourceFilePath;
            ReportTimestamp = reportTimestamp;
            LastLineNumber = 0;
            _lines = new LineInfo[DefaultLineCount];
        }

        public void AddOrMergeLine(int lineNumber, LineInfo line)
        {
            if (lineNumber <= LastLineNumber)
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
            else
            {
                if (lineNumber > MaxLineNumber)
                {
                    throw new IndexOutOfRangeException($"{nameof(lineNumber)} larger than {MaxLineNumber}.");
                }
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
}
