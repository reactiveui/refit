// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose query parameters are flattened by a hand-written <see cref="IQueryConverter{T}"/>.</summary>
public interface IConverterApi
{
    /// <summary>Flattens an object-valued dictionary through a converter.</summary>
    /// <param name="filter">The filter dictionary.</param>
    /// <returns>The response body.</returns>
    [Get("/search")]
    Task<string> Search([QueryConverter(typeof(DictionaryObjectQueryConverter))] IDictionary<string, object> filter);

    /// <summary>Flattens an object-valued dictionary under a parameter-level prefix.</summary>
    /// <param name="filter">The filter dictionary.</param>
    /// <returns>The response body.</returns>
    [Get("/prefixed")]
    Task<string> SearchPrefixed(
        [Query(".", "f")][QueryConverter(typeof(DictionaryObjectQueryConverter))] IDictionary<string, object> filter);

    /// <summary>Flattens a POCO through the System.Text.Json interop converter.</summary>
    /// <param name="filter">The filter object.</param>
    /// <returns>The response body.</returns>
    [Get("/stj")]
    Task<string> SearchStj([QueryConverter(typeof(SystemTextJsonQueryConverter<StjFilter>))] StjFilter filter);
}
