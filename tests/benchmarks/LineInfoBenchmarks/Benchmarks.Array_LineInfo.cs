// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;

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
                    // In line number indexed arrays, full capacity must be based on highest line number, not line count.
                    // However, for proper grouping of benchmarks, we have to pretend otherwise.
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
        public void Array_LineInfo1Struct(
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
        public void Array_LineInfo1Class(
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

        public IEnumerable<object[]> Args_Array_LineInfo2Struct()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_Array_LineInfo2Struct?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo2Struct>(CreateFileInfo(count), $"{count}"),
                    new ParamWrapper<int>(capacity == count ? TestDataSource.Instance[count - 1].LineNumber : capacity, capacity.ToString())
                };
            }

            static FileInfo_Array_LineInfo2Struct CreateFileInfo(int count)
            {
                int capacity = TestDataSource.Instance[count - 1].LineNumber;
                FileInfo_Array_LineInfo2Struct fileInfo = new(capacity);
                for (int i = 0; i < count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_Array_LineInfo2Struct))]
        public void Array_LineInfo2Struct(
            ParamWrapper<FileInfo_Array_LineInfo2Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo2Struct> source,
            ParamWrapper<int> capacity)
        {
            FileInfo_Array_LineInfo2Struct t = target.Value ?? new(capacity.Value);
            LineInfo2Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo2Struct line = ref sourceLines[i];
                if (line.TotalBranches != 0)
                {
                    t[i].Merge(line);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo2Class()
        {
            foreach ((string operation, int count, int capacity) in OperationCountAndCapacity())
            {
                yield return new object[]
                {
                    new ParamWrapper<FileInfo_Array_LineInfo2Class?>(operation == "add" ? null : CreateFileInfo(count), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo2Class>(CreateFileInfo(count), $"{count}"),
                    new ParamWrapper<int>(capacity == count ? TestDataSource.Instance[count - 1].LineNumber : capacity, capacity.ToString())
                };
            }

            static FileInfo_Array_LineInfo2Class CreateFileInfo(int count)
            {
                int capacity = TestDataSource.Instance[count - 1].LineNumber;
                FileInfo_Array_LineInfo2Class fileInfo = new(capacity);
                for (int i = 0; i < count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Args_Array_LineInfo2Class))]
        public void Array_LineInfo2Class(
            ParamWrapper<FileInfo_Array_LineInfo2Class?> target,
            ParamWrapper<FileInfo_Array_LineInfo2Class> source,
            ParamWrapper<int> capacity)
        {
            FileInfo_Array_LineInfo2Class t = target.Value ?? new(capacity.Value);
            LineInfo2Class[] sourceLines = source.Value.Lines;
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
