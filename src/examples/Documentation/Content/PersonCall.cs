// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation.Content;

/// <summary>Retains one deferred generated HTTP invocation.</summary>
/// <typeparam name="T">The body materialized by the HTTP call.</typeparam>
[System.Diagnostics.DebuggerDisplay("Deferred HTTP call")]
public sealed class PersonCall<T>
{
    /// <summary>The deferred call captured by the adapter.</summary>
    private readonly Func<CancellationToken, Task<T>> _invoke;

    /// <summary>Initializes a new instance of the <see cref="PersonCall{T}"/> class.</summary>
    /// <param name="invoke">The single-use generated invocation.</param>
    internal PersonCall(Func<CancellationToken, Task<T>> invoke) => _invoke = invoke;

    /// <summary>Sends the captured request once.</summary>
    /// <param name="cancellationToken">Cancellation passed to the deferred HTTP call.</param>
    /// <returns>The deserialized person.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public Task<T> InvokeAsync(CancellationToken cancellationToken) => _invoke(cancellationToken);
}
