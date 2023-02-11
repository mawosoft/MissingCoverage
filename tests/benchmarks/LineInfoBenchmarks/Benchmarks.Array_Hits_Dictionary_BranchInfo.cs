// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace LineInfoBenchmarks
{
    public partial class Benchmarks
    {
        public IEnumerable<object[]> Args_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    new ParamWrapper<(int capacity, int branchCapacity)>((capacity == stats.Count ? stats.MaxLineNo : capacity, branchCapacity), $"{(capacity == stats.Count ? stats.MaxLineNo : capacity)}/{branchCapacity}")
                };
            }

            static FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct fileInfo = new(stats.MaxLineNo, stats.Branches);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo.CreateLine(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct))]
        public void Array_HitsFlagsInt_Dictionary_BranchInfo1Struct(
            string group,
            ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct?> target,
            ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct> source,
            ParamWrapper<(int capacity, int branchCapacity)> capacity)
        {
            FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct t =
                target.Value ?? new(capacity.Value.capacity, capacity.Value.branchCapacity);
            FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct s = source.Value;
            for (int i = 0; i < s.Lines.Length; i++)
            {
                int hits = s.Lines[i];
                if ((hits & FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct.IsLine) != 0)
                {
                    t.MergeLine(i, hits,
                        (hits & FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct.IsBranch) == 0
                        ? default
                        : s.Branches[i + 1]);
                }
            }
        }

        public IEnumerable<object[]> Args_Array_HitsFlagsInt_Dictionary_BranchInfo1Class()
        {
            foreach ((string group, string operation, TestDataStats stats, int capacity, int branchCapacity) in GenericArgumentsSource())
            {
                yield return new object[]
                {
                    group,
                    new ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class?>(operation == "add" ? null : CreateFileInfo(stats), operation),
                    new ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class>(CreateFileInfo(stats), $"{stats.Count}/{stats.MaxLineNo}"),
                    new ParamWrapper<(int capacity, int branchCapacity)>((capacity == stats.Count ? stats.MaxLineNo : capacity, branchCapacity), $"{(capacity == stats.Count ? stats.MaxLineNo : capacity)}/{branchCapacity}")
                };
            }

            static FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class CreateFileInfo(TestDataStats stats)
            {
                FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class fileInfo = new(stats.MaxLineNo, stats.Branches);
                for (int i = 0; i < stats.Count; i++)
                {
                    fileInfo.CreateLine(TestDataSource.Instance[i]);
                }
                return fileInfo;
            }
        }

        [Benchmark]
        [BenchmarkCategory("OfInterest")]
        [ArgumentsSource(nameof(Args_Array_HitsFlagsInt_Dictionary_BranchInfo1Class))]
        public void Array_HitsFlagsInt_Dictionary_BranchInfo1Class(
            string group,
            ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class?> target,
            ParamWrapper<FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class> source,
            ParamWrapper<(int capacity, int branchCapacity)> capacity)
        {
            FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class t =
                target.Value ?? new(capacity.Value.capacity, capacity.Value.branchCapacity);
            FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class s = source.Value;
            for (int i = 0; i < s.Lines.Length; i++)
            {
                int hits = s.Lines[i];
                if ((hits & FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class.IsLine) != 0)
                {
                    t.MergeLine(i, hits,
                        (hits & FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class.IsBranch) == 0
                        ? null
                        : s.Branches[i + 1]);
                }
            }
        }
    }
}
