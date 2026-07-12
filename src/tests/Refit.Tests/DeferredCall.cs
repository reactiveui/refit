// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A deferred HTTP call surfaced by <see cref="DeferredCallAdapter{T}"/>; the request runs only when invoked.</summary>
/// <typeparam name="T">The materialized result type.</typeparam>
public sealed class DeferredCall<T>
{
    /// <summary>The deferred HTTP call.</summary>
    private readonly Func<CancellationToken, Task<T>> _invoke;

    /// <summary>Initializes a new instance of the <see cref="DeferredCall{T}"/> class.</summary>
    /// <param name="invoke">The deferred HTTP call.</param>
    public DeferredCall(Func<CancellationToken, Task<T>> invoke) => _invoke = invoke;

    /// <summary>Runs the deferred HTTP call.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The materialized result.</returns>
    public Task<T> InvokeAsync(CancellationToken cancellationToken) => _invoke(cancellationToken);
}
