// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Benchmarks;

/// <summary>An open generic return-type adapter surfacing <see cref="ReflectionResult{T}"/>, matched via the generic-definition path.</summary>
/// <typeparam name="T">The wrapped result type.</typeparam>
public sealed class ReflectionResultAdapter<T> : IReturnTypeAdapter<ReflectionResult<T>, T>
{
    /// <inheritdoc/>
    public ReflectionResult<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new();
}
