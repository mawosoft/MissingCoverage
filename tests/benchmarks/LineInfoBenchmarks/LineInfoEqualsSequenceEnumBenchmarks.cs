// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;


namespace LineInfoBenchmarks
{
    [SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals",
        Justification = "Benchmark only")]
    public struct LineInfoDefault : IEquatable<LineInfoDefault>
    {
        private const uint HitsMask = int.MaxValue;
        private const uint LineFlag = ~HitsMask;
        private uint _hits;
        public bool IsLine => (_hits & LineFlag) != 0;
        public int Hits
        {
            get => (int)(_hits & HitsMask);
            set
            {
                if (value >= 0)
                {
                    _hits = (uint)value | LineFlag;
                    return;
                }
                ThrowArgException(); // Ensure inlining /reduce inlining size
                [DoesNotReturn]
                static void ThrowArgException() => throw new ArgumentOutOfRangeException(nameof(Hits));
            }
        }
        public ushort CoveredBranches;
        public ushort TotalBranches;

        public void Merge(LineInfoDefault other)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
            => Hits.ToString() + (IsLine ? string.Empty : "?")
               + (CoveredBranches != 0 || TotalBranches != 0
                  ? $" ({CoveredBranches}/{TotalBranches})"
                  : string.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(LineInfoDefault other)
            => _hits == other._hits
               && TotalBranches == other.TotalBranches
               && CoveredBranches == other.CoveredBranches;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is LineInfoDefault line && Equals(line);
        public override int GetHashCode() => HashCode.Combine(_hits, CoveredBranches, TotalBranches);
    }

    [SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals",
        Justification = "Benchmark only")]
    [StructLayout(LayoutKind.Explicit)]
    public struct LineInfoOverlay : IEquatable<LineInfoOverlay>
    {
        private const uint HitsMask = int.MaxValue;
        private const uint LineFlag = ~HitsMask;
        [FieldOffset(0)] private uint _hits;
        public bool IsLine => (_hits & LineFlag) != 0;
        public int Hits
        {
            get => (int)(_hits & HitsMask);
            set
            {
                if (value >= 0)
                {
                    _hits = (uint)value | LineFlag;
                    return;
                }
                ThrowArgException(); // Ensure inlining /reduce inlining size
                [DoesNotReturn]
                static void ThrowArgException() => throw new ArgumentOutOfRangeException(nameof(Hits));
            }
        }
        [FieldOffset(4)] public ushort CoveredBranches;
        [FieldOffset(6)] public ushort TotalBranches;
        [FieldOffset(0)] private readonly ulong _fastEquals;

        public void Merge(LineInfoOverlay other)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
            => Hits.ToString() + (IsLine ? string.Empty : "?")
               + (CoveredBranches != 0 || TotalBranches != 0
                  ? $" ({CoveredBranches}/{TotalBranches})"
                  : string.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(LineInfoOverlay other) => _fastEquals == other._fastEquals;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is LineInfoOverlay line && Equals(line);
        public override int GetHashCode() => HashCode.Combine(_hits, CoveredBranches, TotalBranches);
    }

    public class SourceFileInfoDefault
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

        private string _sourceFilePath;
        private LineInfoDefault[] _lines;

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set => _sourceFilePath = !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException(null, nameof(SourceFilePath));
        }

        public DateTime ReportTimestamp { get; internal set; }
        public int LastLineNumber { get; private set; }
        public ref readonly LineInfoDefault Line(int index) => ref _lines[index];

        public SourceFileInfoDefault(string sourceFilePath, DateTime reportTimestamp)
        {
            _sourceFilePath = !string.IsNullOrWhiteSpace(sourceFilePath)
                            ? sourceFilePath
                            : throw new ArgumentException(null, nameof(sourceFilePath));
            ReportTimestamp = reportTimestamp;
            LastLineNumber = 0;
            _lines = new LineInfoDefault[DefaultLineCount];
        }

        public override string ToString()
            => $"{SourceFilePath} ({LastLineNumber}) [{ReportTimestamp}]";

        public void AddOrMergeLine(int lineNumber, LineInfoDefault line)
        {
            if (lineNumber <= 0)
            {
                throw new IndexOutOfRangeException();
            }
            else if (lineNumber <= LastLineNumber)
            {
                _lines[lineNumber] = line;
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

        public void Merge(SourceFileInfoDefault other)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(int firstLine, int lastLine)> LineSequences()
        {
            int lineNumber = 1;
            while (lineNumber <= LastLineNumber)
            {
                while (lineNumber <= LastLineNumber && !_lines[lineNumber].IsLine) { lineNumber++; }
                if (lineNumber > LastLineNumber) break;
                int firstLine = lineNumber;
                LineInfoDefault firstInfo = _lines[lineNumber++];
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

    public class SourceFileInfoOverlay
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

        private string _sourceFilePath;
        private LineInfoOverlay[] _lines;

        public string SourceFilePath
        {
            get => _sourceFilePath;
            set => _sourceFilePath = !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException(null, nameof(SourceFilePath));
        }

        public DateTime ReportTimestamp { get; internal set; }
        public int LastLineNumber { get; private set; }
        public ref readonly LineInfoOverlay Line(int index) => ref _lines[index];

        public SourceFileInfoOverlay(string sourceFilePath, DateTime reportTimestamp)
        {
            _sourceFilePath = !string.IsNullOrWhiteSpace(sourceFilePath)
                            ? sourceFilePath
                            : throw new ArgumentException(null, nameof(sourceFilePath));
            ReportTimestamp = reportTimestamp;
            LastLineNumber = 0;
            _lines = new LineInfoOverlay[DefaultLineCount];
        }

        public override string ToString()
            => $"{SourceFilePath} ({LastLineNumber}) [{ReportTimestamp}]";

        public void AddOrMergeLine(int lineNumber, LineInfoOverlay line)
        {
            if (lineNumber <= 0)
            {
                throw new IndexOutOfRangeException();
            }
            else if (lineNumber <= LastLineNumber)
            {
                _lines[lineNumber] = line;
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

        public void Merge(SourceFileInfoDefault other)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<(int firstLine, int lastLine)> LineSequences()
        {
            int lineNumber = 1;
            while (lineNumber <= LastLineNumber)
            {
                while (lineNumber <= LastLineNumber && !_lines[lineNumber].IsLine) { lineNumber++; }
                if (lineNumber > LastLineNumber) break;
                int firstLine = lineNumber;
                LineInfoOverlay firstInfo = _lines[lineNumber++];
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

    public class LineInfoEqualsSequenceEnumBenchmarks
    {
        public IEnumerable<ParamWrapper<SourceFileInfoDefault>> Args_SequenceEnumDefault()
        {
            TestDataStats stats = new(TestDataSource.Instance.Take(1000));
            SourceFileInfoDefault source = new("somefile", DateTime.UtcNow);
            for (int i = 0; i < stats.Count; i++)
            {
                LineInfo1Class testLine = TestDataSource.Instance[i];
                LineInfoDefault line = new()
                {
                    Hits = testLine.Hits,
                    CoveredBranches = (ushort)testLine.CoveredBranches,
                    TotalBranches = (ushort)testLine.TotalBranches,
                };
                source.AddOrMergeLine(testLine.LineNumber, line);
            }
            yield return new ParamWrapper<SourceFileInfoDefault>(source, $"{stats.Count}/{stats.MaxLineNo}");
        }

        public IEnumerable<ParamWrapper<SourceFileInfoOverlay>> Args_SequenceEnumOverlay()
        {
            SourceFileInfoOverlay source = new("somefile", DateTime.UtcNow);
            foreach (ParamWrapper<SourceFileInfoDefault> param in Args_SequenceEnumDefault())
            {
                for (int i = 1; i <= param.Value.LastLineNumber; i++)
                {
                    LineInfoDefault testLine = param.Value.Line(i);
                    if (testLine.IsLine)
                    {
                        LineInfoOverlay line = new()
                        {
                            Hits = testLine.Hits,
                            CoveredBranches = testLine.CoveredBranches,
                            TotalBranches = testLine.TotalBranches,
                        };
                        source.AddOrMergeLine(i, line);
                    }
                }
                yield return new ParamWrapper<SourceFileInfoOverlay>(source, param.DisplayText);
            }
        }


        [Benchmark(Baseline = true)]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_SequenceEnumDefault))]
        public void SequeneEnumDefault(ParamWrapper<SourceFileInfoDefault> source)
        {
            SourceFileInfoDefault s = source.Value;
            foreach ((_, _) in s.LineSequences())
            {
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_SequenceEnumOverlay))]
        public void SequenceEnumOverlay(ParamWrapper<SourceFileInfoOverlay> source)
        {
            SourceFileInfoOverlay s = source.Value;
            foreach ((_, _) in s.LineSequences())
            {
            }
        }
    }

    // Separate class due to large diff in duration
    public class LineInfoEqualsOnlyBenchmarks
    {
        public IEnumerable<object[]> Args_EqualsDefault()
        {
            // Order auf default comparison is Hits, TotalBranches, CoveredBranches
            yield return new object[] { new LineInfoDefault(), new LineInfoDefault() };
            yield return new object[] { new LineInfoDefault() { Hits = 1 }, new LineInfoDefault() { Hits = 1 } };
            yield return new object[] { new LineInfoDefault() { Hits = 1 }, new LineInfoDefault() { Hits = 2 } };
            yield return new object[]
            {
                new LineInfoDefault() { Hits = 1, TotalBranches = 4 },
                new LineInfoDefault() { Hits = 1, TotalBranches = 2 }
            };
            yield return new object[]
            {
                new LineInfoDefault() { Hits = 1, TotalBranches = 4, CoveredBranches = 2 },
                new LineInfoDefault() { Hits = 1, TotalBranches = 4 }
            };
        }

        public IEnumerable<object[]> Args_EqualsOverlay()
        {
            foreach (object[] param in Args_EqualsDefault())
            {
                List<object> retVal = new();
                foreach (LineInfoDefault testLine in param)
                {
                    LineInfoOverlay line = default;
                    if (testLine.IsLine)
                    {
                        line.Hits = testLine.Hits;
                        line.CoveredBranches = testLine.CoveredBranches;
                        line.TotalBranches = testLine.TotalBranches;
                    }
                    retVal.Add(line);

                }
                yield return retVal.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_EqualsDefault))]
        public bool EqualsDefault(LineInfoDefault left, LineInfoDefault right) => left.Equals(right);

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_EqualsOverlay))]
        public bool EqualsOverlay(LineInfoOverlay left, LineInfoOverlay right) => left.Equals(right);
    }
}
