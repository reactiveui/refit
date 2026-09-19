// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Documentation;

/// <summary>Provides overloaded and generic methods for reflected delegate selection.</summary>
internal interface IRequestLookupApi
{
    /// <summary>Builds a numeric-ID request without sending it.</summary>
    /// <param name="id">The numeric identifier inserted into the path.</param>
    /// <returns>A request owned by the caller.</returns>
    [Get("/lookup/{id}")]
    Task<HttpRequestMessage> BuildAsync(int id);

    /// <summary>Builds a text-ID request without sending it.</summary>
    /// <param name="id">The text identifier inserted into the path.</param>
    /// <returns>A request owned by the caller.</returns>
    [Get("/lookup/{id}")]
    Task<HttpRequestMessage> BuildAsync(string id);

    /// <summary>Builds a request carrying a generic local property without sending it.</summary>
    /// <typeparam name="T">The local property's declared type.</typeparam>
    /// <param name="value">The local value retained on the request.</param>
    /// <returns>A request owned by the caller.</returns>
    [Get("/lookup")]
    Task<HttpRequestMessage> GenericAsync<T>([Property("value")] T value);
}
