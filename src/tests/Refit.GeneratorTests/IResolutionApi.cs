// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.LiveResolution;

/// <summary>Exercises rooted and relative generated request paths.</summary>
internal interface IResolutionApi
{
    /// <summary>Gets a rooted resource.</summary>
    /// <param name="id">The resource identifier.</param>
    /// <returns>The response body.</returns>
    [Get("/rooted/{id}")]
    Task<string> Rooted(string id);

    /// <summary>Gets a relative resource.</summary>
    /// <param name="id">The resource identifier.</param>
    /// <returns>The response body.</returns>
    [Get("relative/{id}")]
    Task<string> Relative(string id);
}
