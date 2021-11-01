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
        public void Merge(in LineInfo1Struct other)
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
        }
        public void Merge(in LineInfo2Struct other)
        {
            Hits = Math.Max(Hits, other.Hits);
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public class LineInfo2Class
    {
        public int Hits, CoveredBranches, TotalBranches;
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
        public void Merge(in BranchInfo1Struct other)
        {
            CoveredBranches = Math.Max(CoveredBranches, other.CoveredBranches);
            TotalBranches = Math.Max(TotalBranches, other.TotalBranches);
        }
    }

    public class BranchInfo1Class
    {
        public int CoveredBranches, TotalBranches;
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
