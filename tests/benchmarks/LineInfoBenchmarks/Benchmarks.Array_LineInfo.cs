// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public IEnumerable<object[]> Args_Array_LineInfo1Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_LineInfo1Struct?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo1Struct>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    capacity == stats.Count ? stats.MaxLineNo : capacity
                };
            }

            static FileInfo_Array_LineInfo1Struct CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_LineInfo1Struct fileInfo = new(stats.MaxLineNo);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo1Struct))]
        public void Array_LineInfo1Struct(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo1Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo1Struct> source,
            int capacity)
        {
            FileInfo_Array_LineInfo1Struct t = target.Value ?? new(capacity);
            LineInfo1Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo1Struct line = ref sourceLines[i];
                if (line.LineNumber != 0)
                {
                    t[i].MergeRef(ref line);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo1Class()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_LineInfo1Class?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo1Class>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    capacity == stats.Count ? stats.MaxLineNo : capacity
                };
            }

            static FileInfo_Array_LineInfo1Class CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_LineInfo1Class fileInfo = new(stats.MaxLineNo);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo1Class))]
        public void Array_LineInfo1Class(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo1Class?> target,
            ParamWrapper<FileInfo_Array_LineInfo1Class> source,
            int capacity)
        {
            FileInfo_Array_LineInfo1Class t = target.Value ?? new(capacity);
            LineInfo1Class?[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                if (sourceLines[i] != null)
                {
                    LineInfo1Class? ti;
                    if ((ti = t[i]) == null) t[i] = ti = new();
                    ti.Merge(sourceLines[i]!);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo2Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_LineInfo2Struct?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo2Struct>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    capacity == stats.Count ? stats.MaxLineNo : capacity
                };
            }

            static FileInfo_Array_LineInfo2Struct CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_LineInfo2Struct fileInfo = new(stats.MaxLineNo);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo2Struct))]
        public void Array_LineInfo2Struct(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo2Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo2Struct> source,
            int capacity)
        {
            FileInfo_Array_LineInfo2Struct t = target.Value ?? new(capacity);
            LineInfo2Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo2Struct line = ref sourceLines[i];
                if (line.TotalBranches != 0)
                {
                    t[i].MergeRef(ref line);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo2Class()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_LineInfo2Class?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo2Class>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    capacity == stats.Count ? stats.MaxLineNo : capacity
                };
            }

            static FileInfo_Array_LineInfo2Class CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_LineInfo2Class fileInfo = new(stats.MaxLineNo);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo2Class))]
        public void Array_LineInfo2Class(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo2Class?> target,
            ParamWrapper<FileInfo_Array_LineInfo2Class> source,
            int capacity)
        {
            FileInfo_Array_LineInfo2Class t = target.Value ?? new(capacity);
            LineInfo2Class?[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                if (sourceLines[i] != null)
                {
                    LineInfo2Class? ti;
                    if ((ti = t[i]) == null) t[i] = ti = new();
                    ti.Merge(sourceLines[i]!);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_LineInfo3Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_LineInfo3Struct?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_LineInfo3Struct>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    capacity == stats.Count ? stats.MaxLineNo : capacity
                };
            }

            static FileInfo_Array_LineInfo3Struct CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_LineInfo3Struct fileInfo = new(stats.MaxLineNo);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo[TestDataSource.Instance[i].LineNumber - 1] = new(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo3Struct))]
        public void Array_LineInfo3Struct_MergeRef(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo3Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo3Struct> source,
            int capacity)
        {
            FileInfo_Array_LineInfo3Struct t = target.Value ?? new(capacity);
            LineInfo3Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo3Struct line = ref sourceLines[i];
                if (line.TotalBranches != 0)
                {
                    t[i].MergeRef(ref line);
                }
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_LineInfo3Struct))]
        public void Array_LineInfo3Struct_Merge(
            string group,
            ParamWrapper<FileInfo_Array_LineInfo3Struct?> target,
            ParamWrapper<FileInfo_Array_LineInfo3Struct> source,
            int capacity)
        {
            FileInfo_Array_LineInfo3Struct t = target.Value ?? new(capacity);
            LineInfo3Struct[] sourceLines = source.Value.Lines;
            for (int i = 0; i < sourceLines.Length; i++)
            {
                ref LineInfo3Struct line = ref sourceLines[i];
                if (line.TotalBranches != 0)
                {
                    t[i].Merge(line);
                }
            }
        }
    }
}
