// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface exercising cancellation-token handling.</summary>
public interface ICancellableApi
{
    /// <summary>Gets with a cancellation token.</summary>
    /// <param name="token">A token used to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [SuppressMessage("Major Code Smell", "S2360:Optional parameters should not be used", Justification = "The optional CancellationToken is the idiomatic Refit signature this test verifies.")]
    [Get("/foo")]
    Task GetWithCancellation(CancellationToken token = default);

    /// <summary>Gets with a cancellation token and a string return value.</summary>
    /// <param name="token">A token used to cancel the request.</param>
    /// <returns>The response body as a string.</returns>
    [SuppressMessage("Major Code Smell", "S2360:Optional parameters should not be used", Justification = "The optional CancellationToken is the idiomatic Refit signature this test verifies.")]
    [Get("/foo")]
    Task<string> GetWithCancellationAndReturn(CancellationToken token = default);

    /// <summary>Gets with a nullable cancellation token, which should be ignored when null.</summary>
    /// <param name="token">An optional token used to cancel the request.</param>
    /// <returns>A task that completes when the request finishes.</returns>
    [Get("/foo")]
    Task GetWithNullableCancellation(CancellationToken? token);
}
