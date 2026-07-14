// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit API surface exercising query-object property type classification.</summary>
public interface IQueryObjectTypesApi
{
    /// <summary>Searches using a flattened query object.</summary>
    /// <param name="filters">The query object whose properties are flattened into the query string.</param>
    /// <returns>The response body.</returns>
    [Get("/search")]
    Task<string> Search([Query] QueryObjectWithClassifiedTypes filters);
}
