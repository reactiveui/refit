// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>
/// Adapts a deferred asynchronous HTTP call producing a <typeparamref name="TResult"/> into the return type
/// <typeparamref name="TReturn"/> surfaced by a Refit interface method. Implement this to teach Refit a new
/// return shape (for example <c>IObservable&lt;T&gt;</c> or <c>Result&lt;T&gt;</c>).
/// </summary>
/// <typeparam name="TReturn">The return type the interface method exposes.</typeparam>
/// <typeparam name="TResult">The result the HTTP call materializes — the deserialized response body type.</typeparam>
/// <remarks>
/// The source generator discovers implementors at compile time by their closed <typeparamref name="TReturn"/>
/// and emits a direct call, needing no reflection or registration. The opt-in reflection request builder resolves
/// adapters registered in <see cref="RefitSettings.ReturnTypeAdapters"/>. Implementations must expose a public
/// parameterless constructor; a generic adapter's single type parameter is treated as the wrapped result type
/// (for example <c>Adapter&lt;T&gt; : IReturnTypeAdapter&lt;Wrapper&lt;T&gt;, T&gt;</c>).
/// </remarks>
public interface IReturnTypeAdapter<TReturn, TResult>
{
    /// <summary>Adapts the deferred HTTP call into the surfaced return value.</summary>
    /// <param name="invoke">Sends the request and yields the materialized result. Cold shapes may invoke it lazily
    /// on each subscription; the reflection builder rebuilds the request per invocation, while a generated call
    /// captures the request built once, so it is single-use.</param>
    /// <returns>The surfaced return value.</returns>
    TReturn Adapt(Func<CancellationToken, Task<TResult>> invoke);
}
