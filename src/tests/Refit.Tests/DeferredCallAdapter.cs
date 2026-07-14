// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Surfaces a Refit method's result as a <see cref="DeferredCall{T}"/>.</summary>
/// <typeparam name="T">The materialized result type.</typeparam>
public sealed class DeferredCallAdapter<T> : IReturnTypeAdapter<DeferredCall<T>, T>
{
    /// <inheritdoc/>
    public DeferredCall<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new(invoke);
}
