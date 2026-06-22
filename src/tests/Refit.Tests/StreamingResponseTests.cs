// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;

namespace Refit.Tests;

/// <summary>Tests for interface methods returning <see cref="IAsyncEnumerable{T}"/>.</summary>
public class StreamingResponseTests
{
    /// <summary>Verifies a JSON array response is streamed element by element.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonArrayElements()
    {
        var fixture = CreateFixture("application/json", "[{\"id\":1},{\"id\":2},{\"id\":3}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetArray())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, 2, 3]);
    }

    /// <summary>Verifies a newline-delimited JSON response is streamed line by line.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonLinesElements()
    {
        var fixture = CreateFixture("application/x-ndjson", "{\"id\":1}\n{\"id\":2}\n\n{\"id\":3}\n");

        var ids = new List<int>();
        await foreach (var item in fixture.GetLines())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, 2, 3]);
    }

    /// <summary>Verifies the alternate JSON Lines media type is also streamed line by line.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonLinesForAlternateMediaType()
    {
        var fixture = CreateFixture("application/jsonl", "{\"id\":10}\n{\"id\":20}\n");

        var ids = new List<int>();
        await foreach (var item in fixture.GetLines())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([10, 20]);
    }

    /// <summary>Verifies an HTTP error response throws when the stream is enumerated.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorResponseThrowsOnEnumeration()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp.When("*").Respond(HttpStatusCode.InternalServerError);
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        var fixture = RestService.For<IStreamingApi>("http://foo", settings);

        await Assert
            .That(async () =>
            {
                await foreach (var item in fixture.GetArray())
                {
                    _ = item;
                }
            })
            .Throws<ApiException>();
    }

    /// <summary>Verifies a serializer without streaming support throws a clear error.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonStreamingSerializerThrowsNotSupported()
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .When("*")
            .Respond(_ => new(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\":1}]", System.Text.Encoding.UTF8, "application/json"),
            });

        var settings = new RefitSettings(new NonStreamingContentSerializer())
        {
            HttpMessageHandlerFactory = () => mockHttp,
        };
        var fixture = RestService.For<IStreamingApi>("http://foo", settings);

        await Assert
            .That(async () =>
            {
                await foreach (var item in fixture.GetArray())
                {
                    _ = item;
                }
            })
            .Throws<NotSupportedException>();
    }

    /// <summary>Verifies enumeration stops when the supplied cancellation token is cancelled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HonorsCancellationToken()
    {
        var fixture = CreateFixture("application/json", "[{\"id\":1},{\"id\":2},{\"id\":3}]");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert
            .That(async () =>
            {
                await foreach (var item in fixture.GetArrayCancellable(cts.Token))
                {
                    _ = item;
                }
            })
            .Throws<OperationCanceledException>();
    }

    /// <summary>Creates a streaming fixture backed by a mock handler returning the given payload.</summary>
    /// <param name="mediaType">The response media type.</param>
    /// <param name="payload">The response body.</param>
    /// <returns>The streaming API fixture.</returns>
    private static IStreamingApi CreateFixture(string mediaType, string payload)
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .When("*")
            .Respond(_ => new(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, mediaType),
            });

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => mockHttp };
        return RestService.For<IStreamingApi>("http://foo", settings);
    }
}
