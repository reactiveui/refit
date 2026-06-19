// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with multiple cancellation tokens, used to verify Refit rejects it.</summary>
public interface IManyCancellationTokens
{
    /// <summary>Endpoint declaring two cancellation tokens.</summary>
    /// <param name="token0">First cancellation token.</param>
    /// <param name="token1">Second cancellation token.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetValue(CancellationToken token0, CancellationToken token1);
}
