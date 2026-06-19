// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>API surface exercising cancellation token handling.</summary>
public interface ICancellableMethods
{
    /// <summary>Gets a resource supporting cancellation.</summary>
    /// <param name="token">A token used to cancel the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Major Code Smell", "S2360:Optional parameters should not be used", Justification = "The optional CancellationToken is the idiomatic Refit signature this test verifies.")]
    [Get("/foo")]
    Task GetWithCancellation(CancellationToken token = default);

    /// <summary>Gets a resource supporting cancellation and returning content.</summary>
    /// <param name="token">A token used to cancel the request.</param>
    /// <returns>A task that resolves to the response content.</returns>
    [SuppressMessage("Major Code Smell", "S2360:Optional parameters should not be used", Justification = "The optional CancellationToken is the idiomatic Refit signature this test verifies.")]
    [Get("/foo")]
    Task<string> GetWithCancellationAndReturn(CancellationToken token = default);

    /// <summary>Gets a resource supporting a nullable cancellation token.</summary>
    /// <param name="id">The identifier of the resource to fetch.</param>
    /// <param name="token">An optional token used to cancel the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage("Major Code Smell", "S2360:Optional parameters should not be used", Justification = "The optional CancellationToken is the idiomatic Refit signature this test verifies.")]
    [Get("/foo/{id}")]
    Task GetWithNullableCancellation(int id, CancellationToken? token = default);
}
