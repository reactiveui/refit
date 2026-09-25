// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Runtime.CompilerServices;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>
/// Uploading many records to a bulk endpoint as JSON Lines (one JSON object per line), without first building a list
/// of every record in memory.
/// </summary>
internal static class JsonLinesUploadSample
{
    /// <summary>Runs every JSON Lines upload scenario.</summary>
    /// <returns>A task that completes after every scenario has checked its result.</returns>
    internal static async Task RunAsync()
    {
        await UploadRecordsAsTheyAreProducedAsync();
        await UploadWithSourceGeneratedJsonAsync();
        await UploadAListYouAlreadyHaveAsync();
        await UploadABaseTypeWithItsDiscriminatorAsync();
        await RetryWithAFreshSequenceAsync();
        await SendAKnownContentLengthAsync();
    }

    /// <summary>
    /// Produces records one at a time. A real producer would read rows from a database or a file; here each record
    /// is made up on the spot.
    /// </summary>
    /// <param name="cancellationToken">
    /// Refit passes the request's cancellation token in here, so cancelling the upload also stops the producer.
    /// </param>
    /// <returns>The records, each one yielded as soon as it is ready.</returns>
    private static async IAsyncEnumerable<ImportRecord> ProduceRecordsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int id = 1; id <= 2; id++)
        {
            await Task.Delay(10, cancellationToken); // Pretend the next row takes a moment to read.
            yield return new ImportRecord(id, $"SKU-{id}", id * 10);
        }
    }

    /// <summary>Problem: I have records coming from a slow source and want to upload them without waiting for all of them.</summary>
    /// <returns>A task that completes after the upload is checked.</returns>
    private static async Task UploadRecordsAsTheyAreProducedAsync()
    {
        string? uploaded = null;
        using StubHttp http = new()
        {
            {
                Route.Post("/imports/records"),
                Reply.From(async request =>
                {
                    uploaded = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>("https://api.example.com");
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));

        // Refit writes each record as the producer yields it. Nothing is collected into a list first.
        await api.ImportRecordsAsync(ProduceRecordsAsync(), cancellation.Token);

        Console.Write(uploaded);

        // {"id":1,"sku":"SKU-1","quantity":10}
        // {"id":2,"sku":"SKU-2","quantity":20}
        if (uploaded != "{\"id\":1,\"sku\":\"SKU-1\",\"quantity\":10}\n{\"id\":2,\"sku\":\"SKU-2\",\"quantity\":20}\n")
        {
            throw new InvalidOperationException($"Unexpected upload: {uploaded}");
        }
    }

    /// <summary>Problem: my app is trimmed or Native AOT, so JSON must come from source-generated metadata.</summary>
    /// <returns>A task that completes after the upload is checked.</returns>
    private static async Task UploadWithSourceGeneratedJsonAsync()
    {
        using StubHttp http = new() { { Route.Post("/imports/records"), Reply.Status(HttpStatusCode.Accepted) } };
        using HttpClient httpClient = new(http, disposeHandler: false) { BaseAddress = new Uri("https://api.example.com") };

        // Each record is written as ImportRecord, using the metadata ImportRecordsJsonContext generated for it.
        IJsonLinesUploadApi api = RestService.ForGenerated<IJsonLinesUploadApi>(httpClient, ImportRecordsJsonContext.Default);
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));
        await api.ImportRecordsAsync(ProduceRecordsAsync(), cancellation.Token);

        await http.VerifyAllCalledAsync();
    }

    /// <summary>Problem: I already have the records in an array or list and just want them sent as JSON Lines.</summary>
    /// <returns>A task that completes after the upload is checked.</returns>
    private static async Task UploadAListYouAlreadyHaveAsync()
    {
        using StubHttp http = new() { { Route.Post("/imports/records"), Reply.Status(HttpStatusCode.Accepted) } };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>("https://api.example.com");

        ImportRecord[] records = [new ImportRecord(1, "SKU-1", 10), new ImportRecord(2, "SKU-2", 20)];
        await api.ImportRecordBatchAsync(records);

        await http.VerifyAllCalledAsync();
    }

    /// <summary>
    /// Problem: my records share a base type, and the server needs each line to say which kind it is.
    /// Refit only writes the declared type on its own when that type can't have subclasses, so here we build the
    /// content ourselves and pass it as the body.
    /// </summary>
    /// <returns>A task that completes after the upload is checked.</returns>
    private static async Task UploadABaseTypeWithItsDiscriminatorAsync()
    {
        string? uploaded = null;
        using StubHttp http = new()
        {
            {
                Route.Post("/imports/events"),
                Reply.From(async request =>
                {
                    uploaded = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        RefitSettings settings = new();
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>("https://api.example.com", settings);

        AuditEvent[] events = [new LoginEvent("ada", "10.0.0.1")];

        // JsonLinesContent<AuditEvent> writes every line as AuditEvent, so the "kind" discriminator is included.
        using JsonLinesContent<AuditEvent> content = new(events, settings.ContentSerializer);
        await api.ImportRawAsync(content);

        Console.WriteLine(uploaded); // {"kind":"login","ipAddress":"10.0.0.1","actorId":"ada"}
        if (uploaded is null || !uploaded.Contains("\"kind\":\"login\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected upload: {uploaded}");
        }
    }

    /// <summary>
    /// Problem: the server was busy and I want to try the upload again.
    /// An async upload can only be sent once, because the records are never kept in memory. To retry, call the method
    /// again with a fresh sequence.
    /// </summary>
    /// <returns>A task that completes after the retry is checked.</returns>
    private static async Task RetryWithAFreshSequenceAsync()
    {
        // The first attempt gets "503 Service Unavailable"; the second is accepted.
        using StubHttp http = new()
        {
            { Route.Post("/imports/records"), Reply.Status(HttpStatusCode.ServiceUnavailable) },
            { Route.Post("/imports/records"), Reply.Status(HttpStatusCode.Accepted) },
        };
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>("https://api.example.com");
        using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(30));

        try
        {
            await api.ImportRecordsAsync(ProduceRecordsAsync(), cancellation.Token);
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            // Start the producer again from the beginning, instead of trying to resend the first request.
            await api.ImportRecordsAsync(ProduceRecordsAsync(), cancellation.Token);
        }

        await http.VerifyAllCalledAsync();
    }

    /// <summary>
    /// Problem: the server insists on a Content-Length header, so the size must be known before sending.
    /// Streaming never knows the size up front, so collect the records and buffer the body yourself.
    /// </summary>
    /// <returns>A task that completes after the upload is checked.</returns>
    private static async Task SendAKnownContentLengthAsync()
    {
        long? sentLength = null;
        using StubHttp http = new()
        {
            {
                Route.Post("/imports/records"),
                Reply.From(request =>
                {
                    sentLength = request.Content!.Headers.ContentLength;
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        RefitSettings settings = new();
        IJsonLinesUploadApi api = http.CreateGeneratedClient<IJsonLinesUploadApi>("https://api.example.com", settings);

        List<ImportRecord> records = [];
        await foreach (ImportRecord record in ProduceRecordsAsync())
        {
            records.Add(record);
        }

        using JsonLinesContent<ImportRecord> content = new(records, settings.ContentSerializer);
        await content.LoadIntoBufferAsync(); // Serializes everything now, so the length is known.
        await api.ImportRecordContentAsync(content);

        Console.WriteLine(sentLength); // 73
        if (sentLength is null or 0)
        {
            throw new InvalidOperationException("The upload had no Content-Length.");
        }
    }
}
