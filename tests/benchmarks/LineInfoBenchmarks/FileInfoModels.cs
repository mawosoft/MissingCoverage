// Copyright (c) 2021 Matthias Wolf, Mawosoft.

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
        public LineInfo1Class[] Lines;
        public FileInfo_Array_LineInfo1Class(int capactity) => Lines = new LineInfo1Class[capactity];
        public ref LineInfo1Class this[int index] { get { ArrayExtensions.EnsureSize(ref Lines, index); return ref Lines[index]; } }
    }

    public class FileInfo_Array_LineInfo2Struct
    {
        public LineInfo2Struct[] Lines;
        public FileInfo_Array_LineInfo2Struct(int capactity) => Lines = new LineInfo2Struct[capactity];
        public ref LineInfo2Struct this[int index] { get { ArrayExtensions.EnsureSize(ref Lines, index); return ref Lines[index]; } }
    }

    public class FileInfo_Array_LineInfo2Class
    {
        public LineInfo2Class[] Lines;
        public FileInfo_Array_LineInfo2Class(int capactity) => Lines = new LineInfo2Class[capactity];
        public ref LineInfo2Class this[int index] { get { ArrayExtensions.EnsureSize(ref Lines, index); return ref Lines[index]; } }
    }
}
