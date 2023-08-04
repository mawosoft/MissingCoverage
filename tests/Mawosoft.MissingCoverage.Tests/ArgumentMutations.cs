// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;

namespace Mawosoft.MissingCoverage.Tests;

[Flags]
public enum ArgumentMutations
{
    Hyphen = 1,
    Alias = 2,
    Casing = 4,
    Delimiter = 8,
    ValidValues = 16,
    InvalidValues = 32,
    MissingValue = 64,
    RepeatOnResize = 128,
}
