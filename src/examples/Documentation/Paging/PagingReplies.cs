// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Documentation.Paging;

/// <summary>Builds the JSON replies of the local cloud service stand-ins.</summary>
internal static class PagingReplies
{
    /// <summary>Creates a successful JSON reply carrying response headers.</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="body">The body to serialize.</param>
    /// <param name="typeInfo">The generated metadata for <typeparamref name="T"/>.</param>
    /// <param name="headers">The response headers to add.</param>
    /// <returns>The reply.</returns>
    internal static HttpResponseMessage Json<T>(T body, JsonTypeInfo<T> typeInfo, params (string Name, string Value)[] headers)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body, typeInfo), Encoding.UTF8, "application/json") };
        foreach ((string name, string value) in headers)
        {
            _ = response.Headers.TryAddWithoutValidation(name, value);
        }

        return response;
    }
}
