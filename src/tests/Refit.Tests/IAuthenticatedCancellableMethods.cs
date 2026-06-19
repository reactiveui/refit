// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>API surface exercising authentication combined with cancellation.</summary>
public interface IAuthenticatedCancellableMethods
{
    /// <summary>Gets an authorized resource supporting cancellation.</summary>
    /// <param name="token">A token used to cancel the request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "The optional CancellationToken is the idiomatic Refit signature exercised by these tests.")]
    [Headers("Authorization: Bearer")]
    [Get("/foo")]
    Task GetWithAuthorizationAndCancellation(CancellationToken token = default);
}
