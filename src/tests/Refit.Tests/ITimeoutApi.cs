// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>API surface exercising the per-call <see cref="TimeoutAttribute"/> across void, value, and cancellable
/// methods for both the reflection and source-generated request paths.</summary>
public interface ITimeoutApi
{
    /// <summary>Gets a resource with a short per-call timeout and no response body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/")]
    [Timeout(50)]
    Task GetVoidShortTimeout();

    /// <summary>Gets a resource with a short per-call timeout and a string response body.</summary>
    /// <returns>A task that resolves to the response content.</returns>
    [Get("/")]
    [Timeout(50)]
    Task<string> GetStringShortTimeout();

    /// <summary>Gets a resource with a long per-call timeout and no response body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Get("/")]
    [Timeout(60_000)]
    Task GetVoidLongTimeout();

    /// <summary>Gets a resource with a long per-call timeout and a string response body.</summary>
    /// <returns>A task that resolves to the response content.</returns>
    [Get("/")]
    [Timeout(60_000)]
    Task<string> GetStringLongTimeout();

    /// <summary>Gets a resource with a long per-call timeout alongside a caller cancellation token.</summary>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>A task that resolves to the response content.</returns>
    [Get("/")]
    [Timeout(60_000)]
    Task<string> GetStringLongTimeoutCancellable(CancellationToken cancellationToken);
}
