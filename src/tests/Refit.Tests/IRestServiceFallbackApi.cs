// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface used only by the generated-factory fallback test.</summary>
public interface IRestServiceFallbackApi
{
    /// <summary>Sends a request.</summary>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> Get();
}
