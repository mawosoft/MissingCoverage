// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage.Tests;

[CollectionDefinition(nameof(NoParallelTests), DisableParallelization = true)]
[Collection(nameof(NoParallelTests))]
public partial class NoParallelTests
{
}
