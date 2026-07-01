// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;

namespace Refit.Testing;

/// <summary>
/// The response a <see cref="StubHttp"/> returns for a matched <see cref="RouteMatcher"/>. Build it with
/// the <see cref="Reply"/> factory rather than constructing it directly; the properties are exposed so a
/// test can read what a reply carries.
/// </summary>
public sealed class StubResponse
{
    /// <summary>Gets the response status code. Defaults to <see cref="HttpStatusCode.OK"/>.</summary>
    public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

    /// <summary>Gets a raw JSON response body; sets the content type to <c>application/json</c>.</summary>
    public string? Json { get; init; }

    /// <summary>Gets a plain-text response body paired with <see cref="ContentType"/> (default <c>text/plain</c>).</summary>
    public string? Text { get; init; }

    /// <summary>Gets the content type for <see cref="Text"/>.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets an explicit response content, used verbatim.</summary>
    public HttpContent? Content { get; init; }

    /// <summary>Gets a factory that produces the full response from the request, for total control.</summary>
    public Func<HttpRequestMessage, HttpResponseMessage>? Responder { get; init; }

    /// <summary>Gets an asynchronous factory that produces the full response, for responders that await. Takes precedence over <see cref="Responder"/>.</summary>
    public Func<HttpRequestMessage, Task<HttpResponseMessage>>? ResponderAsync { get; init; }

    /// <summary>Gets a factory that serializes a typed response body using the client's own content serializer.</summary>
    internal Func<IHttpContentSerializer, HttpContent>? BodyFactory { get; init; }
}
