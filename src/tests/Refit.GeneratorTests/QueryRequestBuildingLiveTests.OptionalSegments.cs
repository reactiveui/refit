// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.GeneratorTests;

/// <summary>Live parity coverage for optional <c>{name?}</c> URL path segments (issue #591).</summary>
public sealed partial class QueryRequestBuildingLiveTests
{
    /// <summary>Verifies optional <c>{name?}</c> path segments generate inline and match the reflection builder: a
    /// present value renders normally while a null value drops the segment and its preceding slash.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [RequiresUnreferencedCode("Loads a generated assembly and reflects over generated types and members.")]
    [RequiresDynamicCode("Compares generated request building against the reflection request builder.")]
    public async Task OptionalPathSegmentsMatchReflection()
    {
        using var harness = LiveQueryHarness.Create();

        await harness.AssertParityAsync("TrailingOptional", ["dev", "msg"], "/base/push/dev/msg");
        await harness.AssertParityAsync("TrailingOptional", ["dev", null], "/base/push/dev");
    }
}
