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

    /// <summary>Verifies streaming works through the reflection (non-inline) path for a dynamic route.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsViaReflectionPathForDynamicRoute()
    {
        var fixture = CreateFixture("application/json", "[{\"id\":4},{\"id\":5}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetGroupArray("g1"))
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([4, 5]);
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

    /// <summary>Verifies the source-generated metadata path streams a JSON array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonArrayWithSourceGenContext()
    {
        var fixture = CreateFixture(
            "application/json",
            "[{\"id\":1},{\"id\":2}]",
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        var ids = new List<int>();
        await foreach (var item in fixture.GetArray())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, 2]);
    }

    /// <summary>Verifies the source-gen JSON Lines path handles CRLF endings, a line larger than the read buffer, and a final line with no trailing newline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonLinesWithSourceGenContextAndReaderEdges()
    {
        var bigGap = new string(' ', 5000);

        // Includes a whitespace-only line (skipped), a line larger than the read buffer, CRLF endings, and no final newline.
        var payload = "{\"id\":1}\r\n   \r\n{" + bigGap + "\"id\":2}\r\n{\"id\":3}";
        var fixture = CreateFixture(
            "application/x-jsonlines",
            payload,
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        var ids = new List<int>();
        await foreach (var item in fixture.GetLines())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, 2, 3]);
    }

    /// <summary>Verifies the reflection path links the method cancellation token for a dynamic route.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsViaReflectionPathWithCancellationToken()
    {
        var fixture = CreateFixture("application/json", "[{\"id\":8}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetGroupArrayCancellable("g1", CancellationToken.None))
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([8]);
    }

    /// <summary>Creates a streaming fixture backed by a mock handler returning the given payload.</summary>
    /// <param name="mediaType">The response media type.</param>
    /// <param name="payload">The response body.</param>
    /// <param name="serializer">An optional content serializer; the default is used when null.</param>
    /// <returns>The streaming API fixture.</returns>
    private static IStreamingApi CreateFixture(string mediaType, string payload, IHttpContentSerializer? serializer = null)
    {
        var mockHttp = new MockHttpMessageHandler();
        _ = mockHttp
            .When("*")
            .Respond(_ => new(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, mediaType),
            });

        var settings = serializer is null ? new RefitSettings() : new RefitSettings(serializer);
        settings.HttpMessageHandlerFactory = () => mockHttp;
        return RestService.For<IStreamingApi>("http://foo", settings);
    }
}
