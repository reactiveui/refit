// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for <see cref="RefitSettings.RequestBodySerialization"/> (synchronous fast-path body serialization).</summary>
public class SynchronousBodySerializationTests
{
    /// <summary>The JSON media type expected on serialized request bodies.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>Verifies the buffered mode serializes the body into a buffered JSON content with a Content-Length.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferedInlinePathSerializesBufferedJson()
    {
        var capture = await PostAsync(RequestBodySerializationMode.Buffered, api => api.PostItem(new() { Id = 42 }));

        await Assert.That(capture.MediaType).IsEqualTo(JsonMediaType);
        await Assert.That(capture.ContentLength).IsNotNull();
        await Assert.That(capture.Body).Contains("42", StringComparison.Ordinal);
    }

    /// <summary>Verifies the buffered mode works through the reflection path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BufferedReflectionPathSerializesBufferedJson()
    {
        var capture = await PostAsync(RequestBodySerializationMode.Buffered, api => api.PostItemReflected(new StreamItem { Id = 7 }));

        await Assert.That(capture.MediaType).IsEqualTo(JsonMediaType);
        await Assert.That(capture.ContentLength).IsNotNull();
        await Assert.That(capture.Body).Contains("7", StringComparison.Ordinal);
    }

    /// <summary>Verifies the streamed mode serializes the body without a Content-Length through the inline path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamedInlinePathSerializesWithoutContentLength()
    {
        var capture = await PostAsync(RequestBodySerializationMode.Streamed, api => api.PostItem(new() { Id = 99 }));

        await Assert.That(capture.MediaType).IsEqualTo(JsonMediaType);
        await Assert.That(capture.ContentLength).IsNull();
        await Assert.That(capture.Body).Contains("99", StringComparison.Ordinal);
    }

    /// <summary>Verifies the streamed mode works through the reflection path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamedReflectionPathSerializesWithoutContentLength()
    {
        var capture = await PostAsync(RequestBodySerializationMode.Streamed, api => api.PostItemReflected(new StreamItem { Id = 13 }));

        await Assert.That(capture.MediaType).IsEqualTo(JsonMediaType);
        await Assert.That(capture.ContentLength).IsNull();
        await Assert.That(capture.Body).Contains("13", StringComparison.Ordinal);
    }

    /// <summary>Verifies buffered serialization uses the source-generated metadata fast path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceGenBufferedSerializesBody()
    {
        var capture = await PostAsync(
            RequestBodySerializationMode.Buffered,
            api => api.PostItem(new() { Id = 21 }),
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        await Assert.That(capture.ContentLength).IsNotNull();
        await Assert.That(capture.Body).Contains("21", StringComparison.Ordinal);
    }

    /// <summary>Verifies streamed serialization uses the source-generated metadata fast path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceGenStreamedSerializesBody()
    {
        var capture = await PostAsync(
            RequestBodySerializationMode.Streamed,
            api => api.PostItem(new() { Id = 34 }),
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        await Assert.That(capture.ContentLength).IsNull();
        await Assert.That(capture.Body).Contains("34", StringComparison.Ordinal);
    }

    /// <summary>Posts through a sync-body fixture and captures the request content details seen by the handler.</summary>
    /// <param name="mode">The request-body serialization mode to use.</param>
    /// <param name="call">The interface call to invoke.</param>
    /// <param name="serializer">An optional content serializer; the default is used when null.</param>
    /// <returns>The captured body, media type, and content length.</returns>
    private static async Task<(string? Body, string? MediaType, long? ContentLength)> PostAsync(
        RequestBodySerializationMode mode,
        Func<ISyncBodyApi, Task> call,
        IHttpContentSerializer? serializer = null)
    {
        string? body = null;
        string? mediaType = null;
        long? contentLength = null;
        var handler = new StubHttp
        {
            {
                new RouteMatcher
                {
                    Template = "*",
                    Reusable = true
                },
                Reply.From(async request =>
                {
                    mediaType = request.Content!.Headers.ContentType?.MediaType;
                    contentLength = request.Content!.Headers.ContentLength;
                    body = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK);
                })
            },
        };

        var settings = serializer is null ? new RefitSettings() : new RefitSettings(serializer);
        settings.RequestBodySerialization = mode;
        settings.HttpMessageHandlerFactory = () => handler;

        var fixture = RestService.For<ISyncBodyApi>("http://foo", settings);
        await call(fixture);
        return (body, mediaType, contentLength);
    }
}
