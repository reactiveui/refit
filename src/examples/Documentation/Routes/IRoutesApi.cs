// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Builds requests beneath the v1 prefix without sending them.</summary>
[PathPrefix("/v1")]
internal interface IRoutesApi
{
    /// <summary>Builds a GET request with the person identifier in its path.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people/{id}")]
    Task<HttpRequestMessage> GetAsync(int id);

    /// <summary>Builds a POST request with a serialized person body.</summary>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Post("/people")]
    Task<HttpRequestMessage> PostAsync([Body] Person person);

    /// <summary>Builds a PUT request with both a person path and body.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Put("/people/{id}")]
    Task<HttpRequestMessage> PutAsync(int id, [Body] Person person);

    /// <summary>Builds a PATCH request with both a person path and body.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <param name="person">The person model serialized using the configured generated JSON metadata.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Patch("/people/{id}")]
    Task<HttpRequestMessage> PatchAsync(int id, [Body] Person person);

    /// <summary>Builds a DELETE request for the selected person.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Delete("/people/{id}")]
    Task<HttpRequestMessage> DeleteAsync(int id);

    /// <summary>Builds a HEAD request for the selected person.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Head("/people/{id}")]
    Task<HttpRequestMessage> HeadAsync(int id);

    /// <summary>Builds an OPTIONS request for the people collection.</summary>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Options("/people")]
    Task<HttpRequestMessage> OptionsAsync();

    /// <summary>Omits the optional path segment when the identifier is null.</summary>
    /// <param name="id">The person identifier inserted into the route; an optional null identifier omits its segment.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/people/{id?}")]
    Task<HttpRequestMessage> OptionalAsync(int? id);

    /// <summary>Preserves slashes within the catch-all path while escaping segment text.</summary>
    /// <param name="path">The route path; catch-all file routes preserve its internal slashes.</param>
    /// <returns>The built, unsent request, which the caller must dispose.</returns>
    [Get("/files/{**path}")]
    Task<HttpRequestMessage> FileAsync(string path);
}
