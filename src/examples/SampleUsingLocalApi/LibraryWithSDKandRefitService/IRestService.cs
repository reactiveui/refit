// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using Refit;

namespace LibraryWithSDKandRefitService;

/// <summary>A sample Refit service interface used by the local API example.</summary>
public interface IRestService
{
    /// <summary>Gets all values without any parameter.</summary>
    /// <returns>The response body.</returns>
    [Get("/api/values")]
    Task<string> GetWithNoParameterAsync();

    /// <summary>Gets a single value by identifier.</summary>
    /// <param name="id">The identifier of the value.</param>
    /// <returns>The response body.</returns>
    [Get("/api/values/{id}")]
    Task<string> GetWithParameterAsync([AliasAs("id")] int id);

    /// <summary>Posts a test object to create a new value.</summary>
    /// <param name="modelObject">The model object to post.</param>
    /// <returns>The response body.</returns>
    [Post("/api/values")]
    Task<string> PostWithTestObjectAsync([Body] ModelForTest modelObject);

    /// <summary>Updates a value by identifier with a test object.</summary>
    /// <param name="id">The identifier of the value.</param>
    /// <param name="modelObject">The model object to send.</param>
    /// <returns>The response body.</returns>
    [Put("/api/values/{id}")]
    Task<string> PutWithParametersAsync([AliasAs("id")] int id, [Body] ModelForTest modelObject);

    /// <summary>Deletes a value by identifier.</summary>
    /// <param name="id">The identifier of the value.</param>
    /// <returns>The response body.</returns>
    [Delete("/api/values/{id}")]
    Task<string> DeleteWithParametersAsync([AliasAs("id")] int id);
}
