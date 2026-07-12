// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface with an authorization parameter, exercised through the reflection builder.</summary>
public interface IAuthorizeHeaderApi
{
    /// <summary>Sends a request carrying a bearer token.</summary>
    /// <param name="token">The bearer token value.</param>
    /// <returns>The response body.</returns>
    [Get("/secure")]
    Task<string> Get([Authorize("Bearer")] string token);
}
