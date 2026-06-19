// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using HttpClientDiagnostics;
using Meow.Responses;
using Refit;

namespace Meow;

/// <summary>Provides access to the cats API over a configured HTTP client.</summary>
public sealed class CatsService : IDisposable
{
    /// <summary>The HTTP client used to call the cats API.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The Refit client for the cats API.</summary>
    private readonly ITheCatsApi _theCatsApi;

    /// <summary>Initializes a new instance of the <see cref="CatsService"/> class.</summary>
    /// <param name="baseUrl">The base URL of the cats API.</param>
    public CatsService(Uri baseUrl)
    {
        _httpClient = new(new HttpClientDiagnosticsHandler(new HttpClientHandler())) { BaseAddress = baseUrl };
        _theCatsApi = RestService.For<ITheCatsApi>(_httpClient);
    }

    /// <summary>Searches for cats matching the given breed.</summary>
    /// <param name="breed">The breed to search for.</param>
    /// <returns>The matching search results.</returns>
    public Task<IEnumerable<SearchResult>> SearchAsync(string breed) =>
        _theCatsApi.SearchAsync(breed);

    /// <inheritdoc/>
    public void Dispose() => _httpClient.Dispose();
}
