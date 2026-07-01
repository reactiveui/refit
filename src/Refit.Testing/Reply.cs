// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;

namespace Refit.Testing;

/// <summary>
/// Factory for the <see cref="StubResponse"/> a <see cref="StubHttp"/> route returns. <see cref="With{T}(T)"/>
/// serializes a typed object with the client's own serializer, so tests never hand-write JSON.
/// </summary>
public static class Reply
{
    /// <summary>Replies with a typed body serialized by the client's content serializer and status 200.</summary>
    /// <typeparam name="T">The response body type.</typeparam>
    /// <param name="body">The object to serialize as the response body.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse With<T>(T body) => new() { BodyFactory = serializer => serializer.ToHttpContent(body) };

    /// <summary>Replies with a typed body serialized by the client's content serializer and the given status.</summary>
    /// <typeparam name="T">The response body type.</typeparam>
    /// <param name="body">The object to serialize as the response body.</param>
    /// <param name="status">The response status code.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse With<T>(T body, HttpStatusCode status) => new() { BodyFactory = serializer => serializer.ToHttpContent(body), Status = status };

    /// <summary>Replies with a raw JSON body and status 200.</summary>
    /// <param name="body">The JSON response body.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Json(string body) => new() { Json = body };

    /// <summary>Replies with a raw JSON body and the given status.</summary>
    /// <param name="body">The JSON response body.</param>
    /// <param name="status">The response status code.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Json(string body, HttpStatusCode status) => new() { Json = body, Status = status };

    /// <summary>Replies with a plain-text body and status 200.</summary>
    /// <param name="body">The text response body.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Text(string body) => new() { Text = body };

    /// <summary>Replies with a text body of the given content type and status 200.</summary>
    /// <param name="body">The text response body.</param>
    /// <param name="contentType">The content type for the body.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Text(string body, string contentType) => new() { Text = body, ContentType = contentType };

    /// <summary>Replies with a bare status code and no body.</summary>
    /// <param name="statusCode">The response status code.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Status(HttpStatusCode statusCode) => new() { Status = statusCode };

    /// <summary>Replies with explicit response content used verbatim, and status 200.</summary>
    /// <param name="body">The response content.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse Content(HttpContent body) => new() { Content = body };

    /// <summary>Replies with a response built from the request, for total control.</summary>
    /// <param name="responder">A factory that produces the response from the request.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse From(Func<HttpRequestMessage, HttpResponseMessage> responder) => new() { Responder = responder };

    /// <summary>Replies with a response built asynchronously from the request, for responders that await.</summary>
    /// <param name="responder">An asynchronous factory that produces the response from the request.</param>
    /// <returns>A configured <see cref="StubResponse"/>.</returns>
    public static StubResponse From(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) => new() { ResponderAsync = responder };
}
