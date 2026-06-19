// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Meow.Responses;
using Refit;

namespace Meow;

/// <summary>A Refit client for The Cat API.</summary>
[Headers("x-api-key: b95bfb30-55bc-4327-bb8b-35d740f70051")]
public interface ITheCatsApi
{
    /// <summary>Searches for cat images matching the given breed.</summary>
    /// <param name="breedIdentifier">The breed identifier to search for.</param>
    /// <returns>The matching search results.</returns>
    [Get("/v1/images/search")]
    Task<IEnumerable<SearchResult>> SearchAsync([AliasAs("q")] string breedIdentifier);
}
