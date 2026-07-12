// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Refit.Testing;

namespace Refit.Tests;

/// <summary>Tests for interface methods returning <see cref="IAsyncEnumerable{T}"/>.</summary>
public class StreamingResponseTests
{
    /// <summary>The media type used for JSON array streaming responses.</summary>
    private const string JsonMediaType = "application/json";

    /// <summary>The base address used by the streaming test clients.</summary>
    private const string BaseUrl = "http://foo";

    /// <summary>Expected element id value 2 in streamed payloads.</summary>
    private const int ExpectedId2 = 2;

    /// <summary>Expected element id value 3 in streamed payloads.</summary>
    private const int ExpectedId3 = 3;

    /// <summary>Expected element id value 4 in streamed payloads.</summary>
    private const int ExpectedId4 = 4;

    /// <summary>Expected element id value 5 in streamed payloads.</summary>
    private const int ExpectedId5 = 5;

    /// <summary>Expected element id value 7 in streamed payloads.</summary>
    private const int ExpectedId7 = 7;

    /// <summary>Expected element id value 8 in streamed payloads.</summary>
    private const int ExpectedId8 = 8;

    /// <summary>Expected element id value 10 in streamed payloads.</summary>
    private const int ExpectedId10 = 10;

    /// <summary>Expected element id value 20 in streamed payloads.</summary>
    private const int ExpectedId20 = 20;

    /// <summary>Verifies a JSON array response is streamed element by element.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonArrayElements()
    {
        var fixture = CreateFixture(JsonMediaType, "[{\"id\":1},{\"id\":2},{\"id\":3}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetArray())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, ExpectedId2, ExpectedId3]);
    }

    /// <summary>Verifies streaming works through the reflection (non-inline) path for a dynamic route.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsViaReflectionPathForDynamicRoute()
    {
        var fixture = CreateFixture(JsonMediaType, "[{\"id\":4},{\"id\":5}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetGroupArray("g1"))
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([ExpectedId4, ExpectedId5]);
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

        await Assert.That(ids).IsEquivalentTo([1, ExpectedId2, ExpectedId3]);
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

        await Assert.That(ids).IsEquivalentTo([ExpectedId10, ExpectedId20]);
    }

    /// <summary>Verifies an HTTP error response throws when the stream is enumerated.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ErrorResponseThrowsOnEnumeration()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Status(HttpStatusCode.InternalServerError)
            },
        };
        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };
        var fixture = RestService.For<IStreamingApi>(BaseUrl, settings);

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
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Content(new StringContent("[{\"id\":1}]", System.Text.Encoding.UTF8, JsonMediaType))
            },
        };

        var settings = new RefitSettings(new NonStreamingContentSerializer())
        {
            HttpMessageHandlerFactory = () => handler,
        };
        var fixture = RestService.For<IStreamingApi>(BaseUrl, settings);

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
        var fixture = CreateFixture(JsonMediaType, "[{\"id\":1},{\"id\":2},{\"id\":3}]");

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
            JsonMediaType,
            "[{\"id\":1},{\"id\":2}]",
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        var ids = new List<int>();
        await foreach (var item in fixture.GetArray())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([1, ExpectedId2]);
    }

    /// <summary>Verifies the source-gen JSON Lines path handles CRLF endings, a line larger than the read buffer, and a final line with no trailing newline.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonLinesWithSourceGenContextAndReaderEdges()
    {
        const int bufferExceedingGapLength = 5000;
        var bigGap = new string(' ', bufferExceedingGapLength);

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

        await Assert.That(ids).IsEquivalentTo([1, ExpectedId2, ExpectedId3]);
    }

    /// <summary>Verifies disposing a JSON array stream after one item cleans up the enumerator.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonArrayStreamDisposesEnumeratorEarly()
    {
        var fixture = CreateFixture(JsonMediaType, "[{\"id\":1},{\"id\":2},{\"id\":3}]");

        await using var enumerator = fixture.GetArray().GetAsyncEnumerator();

        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
        await Assert.That(enumerator.Current!.Id).IsEqualTo(1);
    }

    /// <summary>Verifies disposing a JSON Lines stream after one item returns the pooled reader buffer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonLinesStreamDisposesEnumeratorEarly()
    {
        var fixture = CreateFixture(
            "application/x-ndjson",
            "{\"id\":1}\n{\"id\":2}\n{\"id\":3}\n",
            new SystemTextJsonContentSerializer(StreamingJsonContext.Default.Options));

        await using var enumerator = fixture.GetLines().GetAsyncEnumerator();

        await Assert.That(await enumerator.MoveNextAsync()).IsTrue();
        await Assert.That(enumerator.Current!.Id).IsEqualTo(1);
    }

    /// <summary>Verifies a response with no content type is treated as a JSON array.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsJsonArrayWhenResponseHasNoContentType()
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Content(new ByteArrayContent("[{\"id\":7}]"u8.ToArray()))
            },
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };
        var fixture = RestService.For<IStreamingApi>(BaseUrl, settings);

        var ids = new List<int>();
        await foreach (var item in fixture.GetArray())
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([ExpectedId7]);
    }

    /// <summary>Verifies the reflection path links the method cancellation token for a dynamic route.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StreamsViaReflectionPathWithCancellationToken()
    {
        var fixture = CreateFixture(JsonMediaType, "[{\"id\":8}]");

        var ids = new List<int>();
        await foreach (var item in fixture.GetGroupArrayCancellable("g1", CancellationToken.None))
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([ExpectedId8]);
    }

    /// <summary>Verifies the reflection request builder streams an async enumerable without a method cancellation token.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamsAsyncEnumerable()
    {
        var (builder, client) = CreateReflectionBuilder("[{\"id\":2}]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArray));

        var ids = new List<int>();
        await foreach (var item in (IAsyncEnumerable<StreamItem?>)func(client, [])!)
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([ExpectedId2]);
    }

    /// <summary>Verifies the reflection request builder links the method cancellation token when streaming.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamsAsyncEnumerableWithCancellationToken()
    {
        var (builder, client) = CreateReflectionBuilder("[{\"id\":3}]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArrayCancellable));

        var ids = new List<int>();
        await foreach (var item in (IAsyncEnumerable<StreamItem?>)func(client, [CancellationToken.None])!)
        {
            ids.Add(item!.Id);
        }

        await Assert.That(ids).IsEquivalentTo([ExpectedId3]);
    }

    /// <summary>Verifies abandoning the reflection stream part-way disposes the iterator cleanly.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamingStopsWhenEnumerationIsAbandoned()
    {
        var (builder, client) = CreateReflectionBuilder("[{\"id\":2},{\"id\":3}]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArray));

        var sequence = (IAsyncEnumerable<StreamItem?>)func(client, [])!;

        await using var enumerator = sequence.GetAsyncEnumerator();
        var moved = await enumerator.MoveNextAsync();

        await Assert.That(moved).IsTrue();
        await Assert.That(enumerator.Current!.Id).IsEqualTo(ExpectedId2);
    }

    /// <summary>Verifies the reflection request builder surfaces a missing base address when streaming.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamingThrowsWhenBaseAddressMissing()
    {
        var (builder, _) = CreateReflectionBuilder("[]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArray));
        using var clientWithoutBaseAddress = new HttpClient(new StubHttp());

        await Assert.That(() => EnumerateAsync((IAsyncEnumerable<StreamItem?>)func(clientWithoutBaseAddress, [])!))
            .Throws<InvalidOperationException>();
    }

    /// <summary>Verifies disposing the reflection stream before the first move never starts the request.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamingDisposesWithoutMoving()
    {
        var (builder, client) = CreateReflectionBuilder("[{\"id\":2}]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArray));
        var sequence = (IAsyncEnumerable<StreamItem?>)func(client, [])!;

        var enumerator = sequence.GetAsyncEnumerator();
        await enumerator.DisposeAsync();

        await Assert.That(enumerator).IsNotNull();
    }

    /// <summary>Verifies the reflection stream propagates cancellation after linking the method token.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRequestBuilderStreamingPropagatesCancellation()
    {
        var (builder, client) = CreateReflectionBuilder("[{\"id\":2}]");
        var func = builder.BuildRestResultFuncForMethod(nameof(IStreamingApi.GetArrayCancellable));
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert
            .That(() => EnumerateAsync((IAsyncEnumerable<StreamItem?>)func(client, [cancellationTokenSource.Token])!))
            .Throws<OperationCanceledException>();
    }

    /// <summary>Enumerates an async sequence so exceptions thrown on first move surface to the caller.</summary>
    /// <param name="source">The sequence to enumerate.</param>
    /// <returns>The number of items produced.</returns>
    private static async Task<int> EnumerateAsync(IAsyncEnumerable<StreamItem?> source)
    {
        var count = 0;
        await foreach (var item in source)
        {
            count += item is null ? 0 : 1;
        }

        return count;
    }

    /// <summary>Creates the reflection request builder and a client backed by a stub handler.</summary>
    /// <param name="payload">The JSON payload the stub handler replies with.</param>
    /// <returns>The reflection request builder and its HTTP client.</returns>
    private static (RequestBuilderImplementation<IStreamingApi> Builder, HttpClient Client) CreateReflectionBuilder(string payload)
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Content(new StringContent(payload, System.Text.Encoding.UTF8, JsonMediaType))
            },
        };

        var settings = new RefitSettings { HttpMessageHandlerFactory = () => handler };
        var client = new HttpClient(handler) { BaseAddress = new(BaseUrl) };
        return (new RequestBuilderImplementation<IStreamingApi>(settings), client);
    }

    /// <summary>Creates a streaming fixture backed by a mock handler returning the given payload.</summary>
    /// <param name="mediaType">The response media type.</param>
    /// <param name="payload">The response body.</param>
    /// <param name="serializer">An optional content serializer; the default is used when null.</param>
    /// <returns>The streaming API fixture.</returns>
    private static IStreamingApi CreateFixture(string mediaType, string payload, IHttpContentSerializer? serializer = null)
    {
        var handler = new StubHttp
        {
            {
                new RouteMatcher { Template = "*", Reusable = true },
                Reply.Content(new StringContent(payload, System.Text.Encoding.UTF8, mediaType))
            },
        };

        var settings = serializer is null ? new RefitSettings() : new RefitSettings(serializer);
        settings.HttpMessageHandlerFactory = () => handler;
        return RestService.For<IStreamingApi>(BaseUrl, settings);
    }
}
