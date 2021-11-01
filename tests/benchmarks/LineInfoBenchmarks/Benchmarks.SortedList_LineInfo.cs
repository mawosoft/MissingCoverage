﻿// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;
using Perfolizer.Horology;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public IEnumerable<object[]> Args_SortedList_LineInfo1Struct()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_SortedList_LineInfo1Struct?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_SortedList_LineInfo1Struct>(CreateFileInfo(count), $"{count}"),
                    capacity
                };
            }

            static FileInfo_SortedList_LineInfo1Struct CreateFileInfo(int count)
            {
                FileInfo_SortedList_LineInfo1Struct fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedList_LineInfo1Struct))]
        public void SortedList_LineInfo1Struct(
            ParamWrapper<FileInfo_SortedList_LineInfo1Struct?> target,
            ParamWrapper<FileInfo_SortedList_LineInfo1Struct> source,
            int capacity)
        {
            FileInfo_SortedList_LineInfo1Struct t = target.Value ?? new(capacity);
            FileInfo_SortedList_LineInfo1Struct s = source.Value;
            SortedList<int, LineInfo1Struct> targetLines = t.Lines;
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

        public IEnumerable<object[]> Args_SortedList_LineInfo1Class()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_SortedList_LineInfo1Class?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_SortedList_LineInfo1Class>(CreateFileInfo(count), $"{count}"),
                    capacity
                };
            }

            static FileInfo_SortedList_LineInfo1Class CreateFileInfo(int count)
            {
                FileInfo_SortedList_LineInfo1Class fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedList_LineInfo1Class))]
        public void SortedList_LineInfo1Class(
            ParamWrapper<FileInfo_SortedList_LineInfo1Class?> target,
            ParamWrapper<FileInfo_SortedList_LineInfo1Class> source,
            int capacity)
        {
            FileInfo_SortedList_LineInfo1Class t = target.Value ?? new(capacity);
            FileInfo_SortedList_LineInfo1Class s = source.Value;
            SortedList<int, LineInfo1Class> targetLines = t.Lines;
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

        public IEnumerable<object[]> Args_SortedList_LineInfo2Struct()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_SortedList_LineInfo2Struct?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_SortedList_LineInfo2Struct>(CreateFileInfo(count), $"{count}"),
                    capacity
                };
            }

            static FileInfo_SortedList_LineInfo2Struct CreateFileInfo(int count)
            {
                FileInfo_SortedList_LineInfo2Struct fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedList_LineInfo2Struct))]
        public void SortedList_LineInfo2Struct(
            ParamWrapper<FileInfo_SortedList_LineInfo2Struct?> target,
            ParamWrapper<FileInfo_SortedList_LineInfo2Struct> source,
            int capacity)
        {
            FileInfo_SortedList_LineInfo2Struct t = target.Value ?? new(capacity);
            FileInfo_SortedList_LineInfo2Struct s = source.Value;
            SortedList<int, LineInfo2Struct> targetLines = t.Lines;
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

        public IEnumerable<object[]> Args_SortedList_LineInfo2Class()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_SortedList_LineInfo2Class?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_SortedList_LineInfo2Class>(CreateFileInfo(count), $"{count}"),
                    capacity
                };
            }

            static FileInfo_SortedList_LineInfo2Class CreateFileInfo(int count)
            {
                FileInfo_SortedList_LineInfo2Class fileInfo = new(count);
                for (int i = 0; i < count; i++)
                {
                    fileInfo.Lines.Add(TestDataSource.Instance[i].LineNumber, new(TestDataSource.Instance[i]));
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_SortedList_LineInfo2Class))]
        public void SortedList_LineInfo2Class(
            ParamWrapper<FileInfo_SortedList_LineInfo2Class?> target,
            ParamWrapper<FileInfo_SortedList_LineInfo2Class> source,
            int capacity)
        {
            FileInfo_SortedList_LineInfo2Class t = target.Value ?? new(capacity);
            FileInfo_SortedList_LineInfo2Class s = source.Value;
            SortedList<int, LineInfo2Class> targetLines = t.Lines;
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
