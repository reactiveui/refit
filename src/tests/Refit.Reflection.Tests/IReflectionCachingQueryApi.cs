// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An API whose query object is flattened by both the generator and the reflection request builder.</summary>
public interface IReflectionCachingQueryApi
{
    /// <summary>Flattens the combined query object into the query string.</summary>
    /// <param name="query">The query object.</param>
    /// <returns>The response body.</returns>
    [Get("/list")]
    Task<string> Flatten([Query] ReflectionCachingQueryModel query);
}
