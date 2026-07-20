// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>A closed return-type adapter surfacing <see cref="ReflectionResult{T}"/> of <see cref="string"/>, matched via the closed-adapter path.</summary>
public sealed class ReflectionClosedResultAdapter : IReturnTypeAdapter<ReflectionResult<string>, string>
{
    /// <inheritdoc/>
    public ReflectionResult<string> Adapt(Func<CancellationToken, Task<string>> invoke) => new();
}
