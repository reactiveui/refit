// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with multiple authorize parameters, used to verify Refit rejects it.</summary>
public interface IManyAuthorize
{
    /// <summary>Endpoint declaring two authorize parameters.</summary>
    /// <param name="token0">First bearer token.</param>
    /// <param name="token1">Second bearer token.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetValue(
        [Authorize("Bearer")] string token0,
        [Authorize("Bearer")] string token1);
}
