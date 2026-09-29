// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.NativeAotPackageSmoke;

/// <summary>A call that sends its request only when <see cref="InvokeAsync"/> runs.</summary>
/// <typeparam name="T">The reply type.</typeparam>
public sealed class DeferredCall<T>
{
    private readonly Func<CancellationToken, Task<T>> _invoke;

    /// <summary>Initializes a new instance of the <see cref="DeferredCall{T}"/> class.</summary>
    /// <param name="invoke">Sends the request and reads the reply.</param>
    internal DeferredCall(Func<CancellationToken, Task<T>> invoke) => _invoke = invoke;

    /// <summary>Sends the request and reads the reply.</summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The reply.</returns>
    public Task<T> InvokeAsync(CancellationToken cancellationToken) => _invoke(cancellationToken);
}

/// <summary>Adapts a generated call into a <see cref="DeferredCall{T}"/>.</summary>
/// <typeparam name="T">The reply type.</typeparam>
public sealed class DeferredCallAdapter<T> : IReturnTypeAdapter<DeferredCall<T>, T>
{
    /// <inheritdoc/>
    public DeferredCall<T> Adapt(Func<CancellationToken, Task<T>> invoke) => new(invoke);
}
