// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using ReactiveUI.Primitives;

namespace Refit.Documentation.RuntimeHelpers;

/// <summary>Sends built requests through every public generated dispatch entry point.</summary>
internal static class DispatchSample
{
    /// <summary>The shared local HTTP handler reused for every dispatch example.</summary>
    private static readonly DispatchHandler Handler = new();

    /// <summary>The shared client used by the generated and handwritten requests.</summary>
    private static readonly HttpClient Client = new(Handler) { BaseAddress = new("https://example.test/") };

    /// <summary>Checks task, wrapper, cold observable, streaming, and normal generated-interface results.</summary>
    /// <returns>Completion of the local request demonstrations.</returns>
    internal static async Task RunAsync()
    {
        DispatchHandler handler = Handler;
        HttpClient client = Client;
        RefitSettings settings = BodySample.CreateSettings();

        const string itemsPath = "/items";
        int beforeVoid = handler.RequestCount;
        await GeneratedRequestRunner.SendVoidAsync(client, new(HttpMethod.Get, "/ping"), settings, bufferBody: false, CancellationToken.None);
        Check.Require(handler.RequestCount == beforeVoid + 1, "Void dispatch sends exactly one request.");
        int result = await GeneratedRequestRunner.SendAsync<int, int>(
            client,
            new(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);
        await CheckDisposedResponseAsync(handler.LastResponse!.Content);
        using ApiResponse<int>? wrapped = await GeneratedRequestRunner.SendAsync<ApiResponse<int>, int>(
            client,
            new(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: true,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);

        Check.Require(result == SampleValues.Count, "Task entry points deserialize the response.");
        Check.Require(wrapped is { Content: SampleValues.Count, IsSuccessful: true }, "API wrapper entry points retain the response.");
        await CheckRawResultsAsync(client, settings);

        int before = handler.RequestCount;
        IObservable<int> observable = GeneratedRequestRunner.SendObservable<int, int>(
            client,
            static () => new HttpRequestMessage(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: false,
            CancellationToken.None);
        Check.Require(handler.RequestCount == before, "Creating a cold observable does not send a request.");
        int first = await observable.ToTask();
        int second = await observable.ToTask();

        Check.Require(first == SampleValues.Count && second == SampleValues.Count, "Observable subscriptions deserialize their result.");
        Check.Require(handler.RequestCount == before + SampleValues.LastElement, "Each subscription sends a fresh request.");

        HttpRequestMessage streamRequest = new(HttpMethod.Get, "/stream");
        GeneratedRequestRunner.SetRequestTimeout(streamRequest, SampleValues.TimeoutMilliseconds);
        int sum = 0;
        List<int> streamed = [];
        await foreach (int item in GeneratedRequestRunner.StreamAsync<int>(client, streamRequest, settings, CancellationToken.None).WithCancellation(CancellationToken.None))
        {
            sum += item;
            streamed.Add(item);
        }

        Check.Require(sum == SampleValues.PayloadLength, "Streaming dispatch reads each JSON array element.");
        Check.Require(streamed.SequenceEqual(SampleValues.Items), "Streaming dispatch preserves each item and its order.");
        IHelperApi api = RestService.ForGenerated<IHelperApi>(client, settings);
        Check.Require(await api.GetAsync(1) == SampleValues.Count, "Normal applications use the generated interface.");
    }

    /// <summary>Checks result types that transfer ownership and the buffered request-body flag.</summary>
    /// <param name="client">The client using the local handler.</param>
    /// <param name="settings">The generated-metadata serializer settings.</param>
    /// <returns>Completion after every raw body is read and disposed by its caller.</returns>
    private static async Task CheckRawResultsAsync(HttpClient client, RefitSettings settings)
    {
        const string itemsPath = "/items";
        using HttpResponseMessage? response = await GeneratedRequestRunner.SendAsync<HttpResponseMessage, HttpResponseMessage>(
            client,
            new(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);
        Check.Require(response is not null && response.IsSuccessStatusCode && await response.Content.ReadAsStringAsync() == "12", "Raw response results remain readable for their caller.");
        using HttpContent? content = await GeneratedRequestRunner.SendAsync<HttpContent, HttpContent>(
            client,
            new(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);
        Check.Require(content is not null && await content.ReadAsStringAsync() == "12", "Raw content results remain readable for their caller.");
        await using Stream? stream = await GeneratedRequestRunner.SendAsync<Stream, Stream>(
            client,
            new(HttpMethod.Get, itemsPath),
            settings,
            isApiResponse: false,
            shouldDisposeResponse: false,
            bufferBody: false,
            CancellationToken.None);
        Check.Require(stream is not null, "Raw stream dispatch returns the response body.");
        using StreamReader reader = new(stream!);
        Check.Require(await reader.ReadToEndAsync() == "12", "Raw streams retain their complete payload.");
        string? text = await GeneratedRequestRunner.SendAsync<string, string>(
            client,
            new(HttpMethod.Post, itemsPath) { Content = new StringContent("buffer me") },
            settings,
            isApiResponse: false,
            shouldDisposeResponse: true,
            bufferBody: true,
            CancellationToken.None);
        Check.Require(text == "12", "String results return the response text after sending buffered request content.");
    }

    /// <summary>Checks that fully consumed result dispatch disposes its response content.</summary>
    /// <param name="content">The local handler's response content after value dispatch.</param>
    /// <returns>Completion after the disposal guard is observed.</returns>
    private static async Task CheckDisposedResponseAsync(HttpContent content)
    {
        bool disposed = false;
        try
        {
            _ = await content.ReadAsStringAsync();
        }
        catch (ObjectDisposedException)
        {
            disposed = true;
        }

        Check.Require(disposed, "Dispatch with shouldDisposeResponse true closes the fully consumed response content.");
    }
}
