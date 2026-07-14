// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit surface whose route binds a nested property chain of its request object.</summary>
public interface INestedRouteApi
{
    /// <summary>Gets an item identified by a nested request-object property.</summary>
    /// <param name="request">The request whose nested property supplies the path value.</param>
    /// <returns>The response body.</returns>
    [Get("/items/{request.inner.value}")]
    Task<string> GetByNestedValue(NestedRouteRequest request);
}
