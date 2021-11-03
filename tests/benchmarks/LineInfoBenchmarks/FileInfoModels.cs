// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;

namespace LineInfoBenchmarks
{
    public class FileInfo_Dictionary_LineInfo1Struct
    {
        public Dictionary<int, LineInfo1Struct> Lines;
        public FileInfo_Dictionary_LineInfo1Struct(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_Dictionary_LineInfo1Class
    {
        public Dictionary<int, LineInfo1Class> Lines;
        public FileInfo_Dictionary_LineInfo1Class(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_Dictionary_LineInfo2Struct
    {
        public Dictionary<int, LineInfo2Struct> Lines;
        public FileInfo_Dictionary_LineInfo2Struct(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_Dictionary_LineInfo2Class
    {
        public Dictionary<int, LineInfo2Class> Lines;
        public FileInfo_Dictionary_LineInfo2Class(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_SortedDictionary_LineInfo1Struct
    {
        public SortedDictionary<int, LineInfo1Struct> Lines;
        public FileInfo_SortedDictionary_LineInfo1Struct(int capactity) => Lines = new();
    }

    public class FileInfo_SortedDictionary_LineInfo1Class
    {
        public SortedDictionary<int, LineInfo1Class> Lines;
        public FileInfo_SortedDictionary_LineInfo1Class(int capactity) => Lines = new();
    }

    public class FileInfo_SortedDictionary_LineInfo2Struct
    {
        public SortedDictionary<int, LineInfo2Struct> Lines;
        public FileInfo_SortedDictionary_LineInfo2Struct(int capactity) => Lines = new();
    }

    public class FileInfo_SortedDictionary_LineInfo2Class
    {
        public SortedDictionary<int, LineInfo2Class> Lines;
        public FileInfo_SortedDictionary_LineInfo2Class(int capactity) => Lines = new();
    }

    public class FileInfo_SortedList_LineInfo1Struct
    {
        public SortedList<int, LineInfo1Struct> Lines;
        public FileInfo_SortedList_LineInfo1Struct(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_SortedList_LineInfo1Class
    {
        public SortedList<int, LineInfo1Class> Lines;
        public FileInfo_SortedList_LineInfo1Class(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_SortedList_LineInfo2Struct
    {
        public SortedList<int, LineInfo2Struct> Lines;
        public FileInfo_SortedList_LineInfo2Struct(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_SortedList_LineInfo2Class
    {
        public SortedList<int, LineInfo2Class> Lines;
        public FileInfo_SortedList_LineInfo2Class(int capactity) => Lines = new(capactity);
    }

    public class FileInfo_Array_LineInfo1Struct
    {
        public LineInfo1Struct[] Lines;
        public FileInfo_Array_LineInfo1Struct(int capactity) => Lines = new LineInfo1Struct[capactity];
        public ref LineInfo1Struct this[int index] { get { ArrayExtensions.EnsureSize(ref Lines, index); return ref Lines[index]; } }
    }

    public class FileInfo_Array_LineInfo1Class
    {
        public LineInfo1Class?[] Lines;
        public FileInfo_Array_LineInfo1Class(int capactity) => Lines = new LineInfo1Class[capactity];
        public LineInfo1Class? this[int index]
        {
            get { ArrayExtensions.EnsureSize(ref Lines, index); return Lines[index]; }
            set { ArrayExtensions.EnsureSize(ref Lines, index); Lines[index] = value; }
        }
    }

    public class FileInfo_Array_LineInfo2Struct
    {
        public LineInfo2Struct[] Lines;
        public FileInfo_Array_LineInfo2Struct(int capactity) => Lines = new LineInfo2Struct[capactity];
        public ref LineInfo2Struct this[int index] { get { ArrayExtensions.EnsureSize(ref Lines, index); return ref Lines[index]; } }
    }

    public class FileInfo_Array_LineInfo2Class
    {
        public LineInfo2Class?[] Lines;
        public FileInfo_Array_LineInfo2Class(int capactity) => Lines = new LineInfo2Class[capactity];
        public LineInfo2Class? this[int index]
        {
            get { ArrayExtensions.EnsureSize(ref Lines, index); return Lines[index]; }
            set { ArrayExtensions.EnsureSize(ref Lines, index); Lines[index] = value; }
        }
    }

    public class FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct
    {
        public const int IsLine = 1 << ((sizeof(int) * 8) - 1);
        public const int IsBranch = 1 << ((sizeof(int) * 8) - 2);
        public const int FlagMask = IsLine | IsBranch;
        public const int HitsMask = ~FlagMask;
        public int[] Lines;
        public Dictionary<int, BranchInfo1Struct> Branches;
        public FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Struct(int linesCapactity, int branchesCapacity)
        {
            Lines = new int[linesCapactity];
            Branches = new(branchesCapacity);
        }
        public void CreateLine(LineInfo1Class other)
        {
            int index = other.LineNumber - 1;
            int hits = other.Hits | IsLine;
            if (other.TotalBranches != 0)
            {
                hits |= IsBranch;
                Branches.Add(index + 1, new(other));
            }
            ArrayExtensions.EnsureSize(ref Lines, index);
            Lines[index] = hits;
        }
        public void MergeLine(int index, int otherHits, BranchInfo1Struct otherBranchInfo)
        {
            if ((otherHits & IsLine) == 0) return;
            ArrayExtensions.EnsureSize(ref Lines, index);
            int hits = Lines[index] | IsLine;
            BranchInfo1Struct branchInfo;
            if ((hits & IsBranch) != 0)
            {
                if ((otherHits & IsBranch) != 0)
                {
                    branchInfo = Branches[index + 1];
                    branchInfo.Merge(otherBranchInfo);
                    Branches[index + 1] = branchInfo;
                }
            }
            else
            {
                if ((otherHits & IsBranch) != 0)
                {
                    Branches[index + 1] = otherBranchInfo;
                    hits |= IsBranch;
                }
            }
            Lines[index] = (hits & FlagMask) | Math.Max(hits & HitsMask, otherHits & HitsMask);
        }
    }

    public class FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class
    {
        public const int IsLine = 1 << ((sizeof(int) * 8) - 1);
        public const int IsBranch = 1 << ((sizeof(int) * 8) - 2);
        public const int FlagMask = IsLine | IsBranch;
        public const int HitsMask = ~FlagMask;
        public int[] Lines;
        public Dictionary<int, BranchInfo1Class> Branches;
        public FileInfo_Array_HitsFlagsInt_Dictionary_BranchInfo1Class(int linesCapactity, int branchesCapacity)
        {
            Lines = new int[linesCapactity];
            Branches = new(branchesCapacity);
        }
        public void CreateLine(LineInfo1Class other)
        {
            int index = other.LineNumber - 1;
            int hits = other.Hits | IsLine;
            if (other.TotalBranches != 0)
            {
                hits |= IsBranch;
                Branches.Add(index + 1, new(other));
            }
            ArrayExtensions.EnsureSize(ref Lines, index);
            Lines[index] = hits;
        }
        public void MergeLine(int index, int otherHits, BranchInfo1Class? otherBranchInfo)
        {
            if ((otherHits & IsLine) == 0) return;
            ArrayExtensions.EnsureSize(ref Lines, index);
            int hits = Lines[index] | IsLine;
            if ((hits & IsBranch) != 0)
            {
                if ((otherHits & IsBranch) != 0)
                {
                    Branches[index + 1].Merge(otherBranchInfo!);
                }
            }
            else
            {
                if ((otherHits & IsBranch) != 0)
                {
                    BranchInfo1Class branchInfo = new();
                    branchInfo.Merge(otherBranchInfo!);
                    Branches[index + 1] = branchInfo;
                    hits |= IsBranch;
                }
            }
            Lines[index] = (hits & FlagMask) | Math.Max(hits & HitsMask, otherHits & HitsMask);
        }
    }
}
