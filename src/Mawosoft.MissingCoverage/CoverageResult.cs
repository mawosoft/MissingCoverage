// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mawosoft.MissingCoverage
{
    internal class LineInfo
    {
        public string? FilePath;
        public int LineNumber;
        public int Hits;
        public bool Branch;
        public int CoveredConditions;
        public int TotalConditions;
    }

    internal class CoverageResult
    {

    }
}
