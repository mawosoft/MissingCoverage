// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public IEnumerable<object[]> Args_SortedDictionary_LineInfo1Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo1Struct?>(operation == "add" ? null : CreateFileInfo(stats.Count), operation),
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo1Struct>(CreateFileInfo(stats.Count), $"{stats.Count}"),
                    capacity
                };
            }

            static FileInfo_SortedDictionary_LineInfo1Struct CreateFileInfo(int count)
            {
                FileInfo_SortedDictionary_LineInfo1Struct fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedDictionary_LineInfo1Struct))]
        public void SortedDictionary_LineInfo1Struct(
            string group,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo1Struct?> target,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo1Struct> source,
            int capacity)
        {
            FileInfo_SortedDictionary_LineInfo1Struct t = target.Value ?? new(capacity);
            FileInfo_SortedDictionary_LineInfo1Struct s = source.Value;
            SortedDictionary<int, LineInfo1Struct> targetLines = t.Lines;
            foreach (LineInfo1Struct line in s.Lines.Values)
            {
                int lineNumber = line.LineNumber;
                if (targetLines.TryGetValue(lineNumber, out LineInfo1Struct existing))
                {
                    existing.Merge(line);
                    targetLines[lineNumber] = existing;
                }
                else
                {
                    targetLines.Add(lineNumber, line);
                }
            }
        }

        public IEnumerable<object[]> Args_SortedDictionary_LineInfo1Class()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo1Class?>(operation == "add" ? null : CreateFileInfo(stats.Count), operation),
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo1Class>(CreateFileInfo(stats.Count), $"{stats.Count}"),
                    capacity
                };
            }

            static FileInfo_SortedDictionary_LineInfo1Class CreateFileInfo(int count)
            {
                FileInfo_SortedDictionary_LineInfo1Class fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedDictionary_LineInfo1Class))]
        public void SortedDictionary_LineInfo1Class(
            string group,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo1Class?> target,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo1Class> source,
            int capacity)
        {
            FileInfo_SortedDictionary_LineInfo1Class t = target.Value ?? new(capacity);
            FileInfo_SortedDictionary_LineInfo1Class s = source.Value;
            SortedDictionary<int, LineInfo1Class> targetLines = t.Lines;
            foreach (LineInfo1Class line in s.Lines.Values)
            {
                int lineNumber = line.LineNumber;
                if (targetLines.TryGetValue(lineNumber, out LineInfo1Class? existing))
                {
                    existing.Merge(line);
                }
                else
                {
                    targetLines.Add(lineNumber, line);
                }
            }
        }

        public IEnumerable<object[]> Args_SortedDictionary_LineInfo2Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo2Struct?>(operation == "add" ? null : CreateFileInfo(stats.Count), operation),
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo2Struct>(CreateFileInfo(stats.Count), $"{stats.Count}"),
                    capacity
                };
            }

            static FileInfo_SortedDictionary_LineInfo2Struct CreateFileInfo(int count)
            {
                FileInfo_SortedDictionary_LineInfo2Struct fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedDictionary_LineInfo2Struct))]
        public void SortedDictionary_LineInfo2Struct(
            string group,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo2Struct?> target,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo2Struct> source,
            int capacity)
        {
            FileInfo_SortedDictionary_LineInfo2Struct t = target.Value ?? new(capacity);
            FileInfo_SortedDictionary_LineInfo2Struct s = source.Value;
            SortedDictionary<int, LineInfo2Struct> targetLines = t.Lines;
            foreach (KeyValuePair<int, LineInfo2Struct> line in s.Lines)
            {
                int lineNumber = line.Key;
                if (targetLines.TryGetValue(lineNumber, out LineInfo2Struct existing))
                {
                    existing.Merge(line.Value);
                    targetLines[lineNumber] = existing;
                }
                else
                {
                    targetLines.Add(lineNumber, line.Value);
                }
            }
        }

        public IEnumerable<object[]> Args_SortedDictionary_LineInfo2Class()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo2Class?>(operation == "add" ? null : CreateFileInfo(stats.Count), operation),
                    new ParamWrapper<FileInfo_SortedDictionary_LineInfo2Class>(CreateFileInfo(stats.Count), $"{stats.Count}"),
                    capacity
                };
            }

            static FileInfo_SortedDictionary_LineInfo2Class CreateFileInfo(int count)
            {
                FileInfo_SortedDictionary_LineInfo2Class fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedDictionary_LineInfo2Class))]
        public void SortedDictionary_LineInfo2Class(
            string group,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo2Class?> target,
            ParamWrapper<FileInfo_SortedDictionary_LineInfo2Class> source,
            int capacity)
        {
            FileInfo_SortedDictionary_LineInfo2Class t = target.Value ?? new(capacity);
            FileInfo_SortedDictionary_LineInfo2Class s = source.Value;
            SortedDictionary<int, LineInfo2Class> targetLines = t.Lines;
            foreach (KeyValuePair<int, LineInfo2Class> line in s.Lines)
            {
                int lineNumber = line.Key;
                if (targetLines.TryGetValue(lineNumber, out LineInfo2Class? existing))
                {
                    existing.Merge(line.Value);
                }
                else
                {
                    targetLines.Add(lineNumber, line.Value);
                }
            }
        }
    }
}
