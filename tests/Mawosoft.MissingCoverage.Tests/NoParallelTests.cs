// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.MissingCoverage.Tests;

[CollectionDefinition(nameof(NoParallelTests), DisableParallelization = true)]
[Collection(nameof(NoParallelTests))]
public partial class NoParallelTests
{
}
