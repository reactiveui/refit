// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose path and query parameters exercise the per-type URL parameter formatter registry.</summary>
public interface IFormatterMapApi
{
    /// <summary>Binds a formatter-rendered value and a plain string into the path.</summary>
    /// <param name="temp">The value rendered through the per-type formatter.</param>
    /// <param name="city">A plain string path value with no registry entry.</param>
    /// <returns>The response body.</returns>
    [Get("/weather/{temp}/{city}")]
    Task<string> GetByPath(Temperature temp, string city);

    /// <summary>Binds a formatter-rendered value and a plain integer into the query string.</summary>
    /// <param name="temp">The value rendered through the per-type formatter.</param>
    /// <param name="count">A plain integer query value with no registry entry.</param>
    /// <returns>The response body.</returns>
    [Get("/weather")]
    Task<string> GetByQuery(Temperature temp, int count);
}
