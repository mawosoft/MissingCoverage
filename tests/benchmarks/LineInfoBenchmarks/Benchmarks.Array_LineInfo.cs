// Copyright (c) 2021 Matthias Wolf, Mawosoft.

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
        public IEnumerable<object[]> Args_Array_LineInfo1Struct()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_Array_LineInfo1Struct?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo1Struct>(CreateFileInfo(count), $"{count}"),
                    new ParamWrapper<int>(capacity == count ? TestDataSource.Instance[count - 1].LineNumber : capacity, capacity.ToString())
                };
            }

            static FileInfo_Array_LineInfo1Struct CreateFileInfo(int count)
            {
                int capacity = TestDataSource.Instance[count - 1].LineNumber;
                FileInfo_Array_LineInfo1Struct fileInfo = new(capacity);
                for (int i = 0; i < count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_Array_LineInfo1Struct))]
        public void Array_LineInfo1Struct( // TODO verify this actually works
            ParamWrapper<FileInfo_Array_LineInfo1Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo1Struct> source,
            ParamWrapper<int> capacity)
        {
            FileInfo_Array_LineInfo1Struct t = target.Value ?? new(capacity.Value);
            LineInfo1Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo1Struct line = ref sourceLines[i];
                if (line.LineNumber != 0)
                {
                    t[i].Merge(line);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo1Class()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_Array_LineInfo1Class?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo1Class>(CreateFileInfo(count), $"{count}"),
                    new ParamWrapper<int>(capacity == count ? TestDataSource.Instance[count - 1].LineNumber : capacity, capacity.ToString())
                };
            }

            static FileInfo_Array_LineInfo1Class CreateFileInfo(int count)
            {
                int capacity = TestDataSource.Instance[count - 1].LineNumber;
                FileInfo_Array_LineInfo1Class fileInfo = new(capacity);
                for (int i = 0; i < count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_Array_LineInfo1Class))]
        public void Array_LineInfo1Class( // TODO verify this actually works
            ParamWrapper<FileInfo_Array_LineInfo1Class?> target,
            ParamWrapper<FileInfo_Array_LineInfo1Class> source,
            ParamWrapper<int> capacity)
        {
            FileInfo_Array_LineInfo1Class t = target.Value ?? new(capacity.Value);
            LineInfo1Class[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                if (sourceLines[i] != null)
                {
                    if (t[i] == null) t[i] = new();
                    t[i].Merge(sourceLines[i]);
                }
            }
        }
    }
}
