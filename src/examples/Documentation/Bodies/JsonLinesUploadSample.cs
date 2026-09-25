// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>
/// Uploads typed JSON Lines bodies from asynchronous and synchronous sequences: an <see cref="IAsyncEnumerable{T}"/>
/// producer, a registered <see cref="JsonSerializerContext"/> for AOT execution, the typed
/// <see cref="IEnumerable{T}"/> case, the explicit escape hatch that forces a declared element type, retry guidance
/// for a single-use asynchronous body, and buffering a body outside this path when a caller needs a known length.
/// </summary>
internal static class JsonLinesUploadSample
{
    /// <summary>The base address used by every scenario's generated client.</summary>
    private const string BaseUrl = "https://imports.example";

    /// <summary>The route every typed-record scenario uploads to.</summary>
    private const string RecordsPath = "/imports/records";

    /// <summary>The route the escape-hatch and buffered scenarios upload to.</summary>
    private const string EventsPath = "/imports/events";

    /// <summary>The identifier of the first record the producer yields.</summary>
    private const int FirstRecordId = 1;

    /// <summary>The identifier of the second record the producer yields.</summary>
    private const int SecondRecordId = 2;

    /// <summary>The stock-keeping unit of the first record the producer yields.</summary>
    private const string FirstRecordSku = "SKU-1";

    /// <summary>The stock-keeping unit of the second record the producer yields.</summary>
    private const string SecondRecordSku = "SKU-2";

    /// <summary>The quantity of the first record the producer yields.</summary>
    private const int FirstRecordQuantity = 10;

    /// <summary>The quantity of the second record the producer yields.</summary>
    private const int SecondRecordQuantity = 20;

    /// <summary>The first record's expected JSON line.</summary>
    private const string FirstRecordJson = """{"id":1,"sku":"SKU-1","quantity":10}""";

    /// <summary>The second record's expected JSON line.</summary>
    private const string SecondRecordJson = """{"id":2,"sku":"SKU-2","quantity":20}""";

    /// <summary>The exact request body written for the two-record upload, matching the producer's output.</summary>
    private const string RecordsBody = $"{FirstRecordJson}\n{SecondRecordJson}";

    /// <summary>Reusable metadata for <see cref="ImportRecord"/>, backed by the registered generated context.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(ImportRecordsJsonContext.Default.Options) { TypeInfoResolver = ImportRecordsJsonContext.Default };

    /// <summary>Runs every JSON Lines upload scenario.</summary>
    /// <returns>A task that completes after every assertion.</returns>
    internal static async Task RunAsync()
    {
        await UploadAsyncProducerAsync();
        await UploadWithGeneratedContextAsync();
        await UploadTypedEnumerableAsync();
        await UploadPolymorphicEscapeHatchAsync();
        await ShowRetryGuidanceAsync();
        await ShowBufferedUploadAsync();
    }

    /// <summary>Creates settings whose serializer has generated metadata for <see cref="ImportRecord"/>.</summary>
    /// <returns>Fresh settings for one handler.</returns>
    private static RefitSettings CreateSettings() => new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Creates a client that routes through the handler, reused for the whole scenario rather than per call.</summary>
    /// <param name="http">The handler used instead of a real network connection.</param>
    /// <returns>A client owned and disposed by its scenario.</returns>
    private static HttpClient CreateClient(StubHttp http) => new(http, disposeHandler: false) { BaseAddress = new(BaseUrl) };

    /// <summary>
    /// Produces records asynchronously, one at a time. A real producer would read rows from a database cursor or a
    /// paged API instead of yielding immediately; <see cref="Task.Yield"/> only stands in for that latency here.
    /// </summary>
    /// <param name="cancellationToken">The token the caller's send flows into this producer, checked between rows.</param>
    /// <returns>The two records, yielded as they become available.</returns>
    private static async IAsyncEnumerable<ImportRecord> ProduceRecordsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new(FirstRecordId, FirstRecordSku, FirstRecordQuantity);
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new(SecondRecordId, SecondRecordSku, SecondRecordQuantity);
    }

    /// <summary>Uploads records from an asynchronous producer, passing the caller's token to both the send and the producer.</summary>
    /// <returns>A task that completes after the upload is answered and its body is checked.</returns>
    private static async Task UploadAsyncProducerAsync()
    {
        using CancellationTokenSource cancellation = new();
        using StubHttp http = new() { { new() { Method = HttpMethod.Post, Template = RecordsPath, Body = RecordsBody }, Reply.Status(HttpStatusCode.Accepted) } };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>(BaseUrl, CreateSettings());

        await api.ImportRecordsAsync(ProduceRecordsAsync(cancellation.Token), cancellation.Token);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Creates the generated client straight from the registered context, skipping reflection entirely.</summary>
    /// <returns>A task that completes after the context-backed upload is answered.</returns>
    private static async Task UploadWithGeneratedContextAsync()
    {
        using StubHttp http = new() { { new() { Method = HttpMethod.Post, Template = RecordsPath, Body = RecordsBody }, Reply.Status(HttpStatusCode.Accepted) } };
        using HttpClient client = CreateClient(http);
        IJsonLinesUploadApi api = RestService.ForGenerated<IJsonLinesUploadApi>(client, ImportRecordsJsonContext.Default);

        await api.ImportRecordsAsync(ProduceRecordsAsync(CancellationToken.None), CancellationToken.None);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>
    /// Uploads a materialized batch. <see cref="ImportRecord"/> is sealed, so the generator already writes it typed
    /// from a plain <see cref="IEnumerable{T}"/>: this is the same bytes as an untyped upload, without the escape hatch.
    /// </summary>
    /// <returns>A task that completes after the batch upload is answered.</returns>
    private static async Task UploadTypedEnumerableAsync()
    {
        using StubHttp http = new() { { new() { Method = HttpMethod.Post, Template = RecordsPath, Body = RecordsBody }, Reply.Status(HttpStatusCode.Accepted) } };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>(BaseUrl, CreateSettings());

        ImportRecord[] batch =
        [
            new(FirstRecordId, FirstRecordSku, FirstRecordQuantity),
            new(SecondRecordId, SecondRecordSku, SecondRecordQuantity),
        ];
        await api.ImportRecordBatchAsync(batch);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>
    /// Forces the declared element type for a non-sealed type. <see cref="AuditEvent"/> is abstract, so the
    /// generator cannot write it typed automatically; building <see cref="JsonLinesContent{T}"/> explicitly and
    /// passing it as the body is the escape hatch, and it keeps the discriminator the type declares.
    /// </summary>
    /// <returns>A task that completes after the discriminated event is checked and the upload is answered.</returns>
    private static async Task UploadPolymorphicEscapeHatchAsync()
    {
        RefitSettings settings = RefitSettings.CamelCase();
        string? uploaded = null;
        using StubHttp http = new()
        {
            {
                Route.Post(EventsPath),
                Reply.From(async request =>
                {
                    uploaded = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>(BaseUrl, settings);

        AuditEvent[] events = [new LoginEvent("a-1", "10.0.0.1")];
        using JsonLinesContent<AuditEvent> content = new(events, settings.ContentSerializer);
        await api.ImportRawAsync(content);

        Console.WriteLine(uploaded); // The camelCase JSON line, carrying the "login" discriminator.
        SampleCheck.Equal(true, uploaded?.Contains("\"kind\":\"login\"", StringComparison.Ordinal));
        await http.VerifyAllCalledAsync();
    }

    /// <summary>
    /// Shows that a single-use asynchronous body cannot be resent, and the supported retry: build a new request from
    /// a fresh sequence rather than resending the same content.
    /// </summary>
    /// <returns>A task that completes after the rejected resend and the successful retry are checked.</returns>
    private static async Task ShowRetryGuidanceAsync()
    {
        RefitSettings settings = CreateSettings();
        using JsonLinesContent<ImportRecord> sentOnce = new(ProduceRecordsAsync(CancellationToken.None), settings.ContentSerializer);
        await using MemoryStream firstSend = new();
        await sentOnce.CopyToAsync(firstSend, CancellationToken.None);
        bool rejected = false;
        try
        {
            // Never resend the same async-source content: nothing is buffered to replay it. A real send, not just a
            // second read, is what actually re-enumerates the producer, so this uses CopyToAsync directly rather
            // than a caching read like ReadAsStringAsync.
            await using MemoryStream secondSend = new();
            await sentOnce.CopyToAsync(secondSend, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        SampleCheck.Equal(true, rejected);

        using StubHttp http = new()
        {
            { new() { Method = HttpMethod.Post, Template = RecordsPath, Body = RecordsBody }, Reply.Status(HttpStatusCode.ServiceUnavailable) },
            { new() { Method = HttpMethod.Post, Template = RecordsPath, Body = RecordsBody }, Reply.Status(HttpStatusCode.Accepted) },
        };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>(BaseUrl, settings);
        bool succeeded = await RetryOnceAsync(api);

        SampleCheck.Equal(true, succeeded);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Calls the upload once, and retries exactly once with a fresh producer if the first reply fails.</summary>
    /// <param name="api">The client whose second configured reply succeeds.</param>
    /// <returns><see langword="true"/> once an attempt succeeds.</returns>
    private static async Task<bool> RetryOnceAsync(IJsonLinesUploadApi api)
    {
        try
        {
            // Every attempt calls the method again with a fresh producer, never the exhausted content.
            await api.ImportRecordsAsync(ProduceRecordsAsync(CancellationToken.None), CancellationToken.None);
            return true;
        }
        catch (ApiException)
        {
            // The first reply failed; retry once with a fresh sequence.
            await api.ImportRecordsAsync(ProduceRecordsAsync(CancellationToken.None), CancellationToken.None);
            return true;
        }
    }

    /// <summary>
    /// Buffers a body outside the lazy JSON Lines path for a caller that needs a known Content-Length: materializes
    /// the sequence, then loads the content into a buffer before sending.
    /// </summary>
    /// <returns>A task that completes after the buffered upload's known length and answer are checked.</returns>
    private static async Task ShowBufferedUploadAsync()
    {
        RefitSettings settings = CreateSettings();
        List<ImportRecord> materialized = [];
        await foreach (ImportRecord record in ProduceRecordsAsync(CancellationToken.None))
        {
            materialized.Add(record);
        }

        using JsonLinesContent<ImportRecord> content = new(materialized, settings.ContentSerializer);
        await content.LoadIntoBufferAsync(CancellationToken.None);
        Console.WriteLine(content.Headers.ContentLength); // A known length now that the body is buffered.
        SampleCheck.Equal(true, content.Headers.ContentLength > 0);

        using StubHttp http = new() { { Route.Post(EventsPath), Reply.Status(HttpStatusCode.Accepted) } };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>(BaseUrl, settings);
        await api.ImportRawAsync(content);
        await http.VerifyAllCalledAsync();
    }
}
