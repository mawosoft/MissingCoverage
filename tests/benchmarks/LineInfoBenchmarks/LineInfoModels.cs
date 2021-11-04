// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;

namespace LineInfoBenchmarks
{
    // Note: this would be much easier with interfaces like IMerge and ILookup, but calls might
    // not get devirtualized.

    public struct LineInfo1Struct
    {
        public int LineNumber, Hits, CoveredBranches, TotalBranches;
        public LineInfo1Struct(LineInfo1Class other)
        {
            LineNumber = other.LineNumber;
            Hits = other.Hits;
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
        }
        // TODO does ref vs in have any performance impact (e.g. defensive copy on latter)?
        // See also LineInfo2Struct, BranchInfo1Struct
        public void MergeRef(ref LineInfo1Struct other)
        {
            LineNumber = other.LineNumber;
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
        public void Merge(LineInfo1Struct other)
        {
            LineNumber = other.LineNumber;
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public class LineInfo1Class
    {
        public int LineNumber, Hits, CoveredBranches, TotalBranches;
        public LineInfo1Class() { }
        public LineInfo1Class(LineInfo1Class other)
        {
            LineNumber = other.LineNumber;
            Hits = other.Hits;
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
        }
        public void Merge(LineInfo1Class other)
        {
            LineNumber = other.LineNumber;
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public struct LineInfo2Struct
    {
        public int Hits, CoveredBranches, TotalBranches;
        public LineInfo2Struct(LineInfo1Class other)
        {
            Hits = other.Hits;
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
            if (TotalBranches == 0)
            {
                // "Line exists" indicator if used in array indexed by line number.
                TotalBranches = 1;
            }
        }
        public void MergeRef(ref LineInfo2Struct other)
        {
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
            if (TotalBranches == 0)
            {
                // "Line exists" indicator if used in array indexed by line number.
                TotalBranches = 1;
            }
        }
        public void Merge(LineInfo2Struct other)
        {
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
            if (TotalBranches == 0)
            {
                // "Line exists" indicator if used in array indexed by line number.
                TotalBranches = 1;
            }
        }
    }

    public class LineInfo2Class
    {
        public int Hits, CoveredBranches, TotalBranches;
        public LineInfo2Class() { }
        public LineInfo2Class(LineInfo1Class other)
        {
            Hits = other.Hits;
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
        }
        public void Merge(LineInfo2Class other)
        {
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public struct BranchInfo1Struct
    {
        public int CoveredBranches, TotalBranches;
        public BranchInfo1Struct(LineInfo1Class other)
        {
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
        }
        // TODO ref? in? See notes on LineInfo1Struct. This however is only 2x int, byref might make it worse
        public void Merge(BranchInfo1Struct other)
        {
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public class BranchInfo1Class
    {
        public int CoveredBranches, TotalBranches;
        public BranchInfo1Class() { }
        public BranchInfo1Class(LineInfo1Class other)
        {
            CoveredBranches = other.CoveredBranches;
            TotalBranches = other.TotalBranches;
        }
        public void Merge(BranchInfo1Class other)
        {
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }
}
