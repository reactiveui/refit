// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Analyzers.Tests;

/// <summary>White-box tests for Refit interface analyzer implementation details.</summary>
public sealed class RefitInterfaceAnalyzerTests
{
    /// <summary>Verifies HTTP path extraction handles missing attribute data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task GetHttpPathReturnsEmptyForMissingAttribute() =>
        await Assert.That(RefitInterfaceAnalyzer.GetHttpPath(null)).IsEqualTo(string.Empty);
}
