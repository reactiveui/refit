// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Net;
using System.Text;

namespace Refit.Tests;

/// <summary>Tests for generated collection-property expansion, streaming cancellation linking and the descriptor
/// URL-encoded body overload.</summary>
public partial class GeneratedRequestRunnerTests
{
    /// <summary>Relative path shared by the formatted-collection-property fixtures.</summary>
    private const string CollectionPropertyPath = "/p";

    /// <summary>Query key shared by the formatted-collection-property fixtures.</summary>
    private const string CollectionPropertyKey = "k";

    /// <summary>The first value in the streamed JSON array body.</summary>
    private const int StreamedFirstValue = 1;

    /// <summary>The second value in the streamed JSON array body.</summary>
    private const int StreamedSecondValue = 2;

    /// <summary>The JSON array body returned by the streaming fixtures.</summary>
    private const string JsonArrayBody = "[1,2]";

    /// <summary>The media type of the streamed JSON array body.</summary>
    private const string JsonArrayMediaType = "application/json";

    /// <summary>The two-element collection shared by the formatted-collection-property fixtures.</summary>
    private static readonly string[] CollectionElements = ["a", "b"];

    /// <summary>The values expected from streaming the JSON array body.</summary>
    private static readonly int?[] ExpectedStreamedValues = [StreamedFirstValue, StreamedSecondValue];

    /// <summary>Verifies a null collection value appends nothing to the query.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task AddFormattedCollectionPropertyIgnoresNullCollection()
    {
        var result = AddFormattedCollectionProperty(null, CollectionFormat.Csv);

        await Assert.That(result).IsEqualTo(CollectionPropertyPath);
    }

    /// <summary>Verifies a customized-formatter collection joins values by the resolved delimiter.</summary>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <param name="expected">The expected relative path and query.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(CollectionFormat.Ssv, "/p?k=a%20b")]
    [Arguments(CollectionFormat.Tsv, "/p?k=a%09b")]
    [Arguments(CollectionFormat.Pipes, "/p?k=a%7Cb")]
    public async Task AddFormattedCollectionPropertyJoinsWithResolvedDelimiter(CollectionFormat collectionFormat, string expected)
    {
        var result = AddFormattedCollectionProperty(CollectionElements, collectionFormat);

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>Verifies streaming links the method and consumer cancellation tokens when both can cancel.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamAsyncLinksMethodAndConsumerTokens()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonArrayBody, Encoding.UTF8, JsonArrayMediaType)
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        using var methodTokenSource = new CancellationTokenSource();
        using var consumerTokenSource = new CancellationTokenSource();
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var values = new List<int?>();
        await foreach (var item in GeneratedRequestRunner
                           .StreamAsync<int>(client, request, settings, methodTokenSource.Token)
                           .WithCancellation(consumerTokenSource.Token))
        {
            values.Add(item);
        }

        await Assert.That(values).IsCollectionEqualTo(ExpectedStreamedValues);
    }

    /// <summary>Verifies streaming skips the linked cancellation source when the method's token cannot cancel, running
    /// against the consumer token alone.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamAsyncSkipsLinkedSourceWhenMethodTokenCannotCancel()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonArrayBody, Encoding.UTF8, JsonArrayMediaType)
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        var values = new List<int?>();
        await foreach (var item in GeneratedRequestRunner
                           .StreamAsync<int>(client, request, settings, CancellationToken.None))
        {
            values.Add(item);
        }

        await Assert.That(values).IsCollectionEqualTo(ExpectedStreamedValues);
    }

    /// <summary>Verifies streaming disposes the request and underlying response when enumeration stops early, exercising
    /// the iterator's early-disposal path rather than natural completion.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamAsyncStopsAndDisposesWhenEnumerationStopsEarly()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonArrayBody, Encoding.UTF8, JsonArrayMediaType)
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        await using var enumerator = GeneratedRequestRunner
            .StreamAsync<int>(client, request, settings, CancellationToken.None)
            .GetAsyncEnumerator();

        // Advance a single element, then let `await using` dispose the enumerator before the sequence completes.
        var moved = await enumerator.MoveNextAsync();

        await Assert.That(moved).IsTrue();
        await Assert.That(enumerator.Current).IsEqualTo(StreamedFirstValue);
    }

    /// <summary>Verifies streaming surfaces the response error through the iterator, exercising its exceptional-exit
    /// path where the request and linked source are still disposed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamAsyncThrowsForUnsuccessfulResponse()
    {
        var handler = new CapturingHandler(
            static (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("boom")
                }));
        using var client = CreateClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, RelativeResourcePath);
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());

        await Assert
            .That(async () =>
            {
                await using var enumerator = GeneratedRequestRunner
                    .StreamAsync<int>(client, request, settings, CancellationToken.None)
                    .GetAsyncEnumerator();
                await enumerator.MoveNextAsync();
            })
            .Throws<ApiException>();
    }

    /// <summary>Verifies the descriptor overload reuses already-created HTTP content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsReusesHttpContent()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        var content = new StringContent("content-body");

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, (HttpContent)content, []);

        await Assert.That(result).IsSameReferenceAs(content);
    }

    /// <summary>Verifies the descriptor overload wraps stream bodies as stream content.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateUrlEncodedBodyContentWithFieldsUsesStreamContentForStreams()
    {
        var settings = new RefitSettings(new SystemTextJsonContentSerializer());
        await using var stream = new MemoryStream(StreamBodyBytes);

        var result = GeneratedRequestRunner.CreateUrlEncodedBodyContent(settings, (Stream)stream, []);

        await Assert.That(result).IsTypeOf<StreamContent>();
        await Assert.That(await result.ReadAsStringAsync()).IsEqualTo(StreamBodyText);
    }

    /// <summary>Appends a collection property through a customized formatter and returns the built relative path.</summary>
    /// <param name="values">The collection value, or null.</param>
    /// <param name="collectionFormat">The resolved collection format.</param>
    /// <returns>The built relative path.</returns>
    private static string AddFormattedCollectionProperty(IEnumerable? values, CollectionFormat collectionFormat)
    {
        var settings = CreateSettings();
        var builder = new GeneratedQueryStringBuilder(CollectionPropertyPath);
        GeneratedRequestRunner.AddFormattedCollectionProperty(
            ref builder,
            settings,
            values,
            CollectionPropertyKey,
            collectionFormat,
            false,
            (typeof(string), typeof(string), typeof(string)));
        return builder.Build();
    }
}
