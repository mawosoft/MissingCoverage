// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mawosoft.MissingCoverage
{
    internal struct LineInfo
    {
        private const uint HitsMask = int.MaxValue;
        private const uint LineFlag = ~HitsMask;

        private uint _hits;
        public bool IsLine => (_hits & LineFlag) != 0;
        public int Hits
        {
            get => (int)(_hits & HitsMask);
            set
            {
                if (value >= 0)
                {
                    _hits = (uint)value | LineFlag;
                    return;
                }
                ThrowArgException(); // Ensure inlining /reduce inlining size
                [DoesNotReturn]
                static void ThrowArgException() => throw new ArgumentOutOfRangeException(nameof(Hits));
            }
        }
        public ushort CoveredBranches;
        public ushort TotalBranches;

        public void Merge(LineInfo other)
        {
            if (!other.IsLine) return;
            if (!IsLine)
            {
                this = other;
                return;
            }
            // We don't need to remove flags for this comparison
            if (_hits < other._hits) _hits = other._hits;
            if (TotalBranches == other.TotalBranches)
            {
                if (CoveredBranches < other.CoveredBranches) CoveredBranches = other.CoveredBranches;
            }
            else if (other.TotalBranches != 0)
            {
                // This should rather not happen as it indicates that we merge reports from different
                // source file versions.
                double covered = TotalBranches == 0
                               ? 0
                               : (double)CoveredBranches / TotalBranches;
                double otherCovered = (double)other.CoveredBranches / other.TotalBranches;
                if (covered < otherCovered
                    || (covered == otherCovered && CoveredBranches < other.CoveredBranches))
                {
                    CoveredBranches = other.CoveredBranches;
                    TotalBranches = other.TotalBranches;
                }
            }
        }

        public override string ToString()
            => $"({(IsLine ? '+' : '-')}{Hits}, {CoveredBranches}/{TotalBranches})";
    }
}
