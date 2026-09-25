// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises streamed replies, stalled bodies, upload capture policies and simulated time without real waits.</summary>
internal static class TestingStreaming
{
    /// <summary>The base address used by the generated test clients.</summary>
    private const string BaseUrl = "https://people.example";

    /// <summary>The route of the streaming method.</summary>
    private const string LivePath = "/people/live";

    /// <summary>The first streamed person's name.</summary>
    private const string FirstName = "Ada";

    /// <summary>The second streamed person's name.</summary>
    private const string SecondName = "Grace";

    /// <summary>The second streamed person's identifier.</summary>
    private const int SecondId = 2;

    /// <summary>The number of people each streamed body carries.</summary>
    private const int StreamedPeople = 2;

    /// <summary>The seed for the network simulation, which draws no faults here.</summary>
    private const int SimulationSeed = 7;

    /// <summary>The simulated network delay, in seconds.</summary>
    private const int DelaySeconds = 2;

    /// <summary>The verification timeout, in seconds.</summary>
    private const int VerifyTimeoutSeconds = 5;

    /// <summary>A capture limit large enough for one serialized person.</summary>
    private const int RoomyCaptureBytes = 1024;

    /// <summary>A capture limit too small for one serialized person.</summary>
    private const int TinyCaptureBytes = 4;

    /// <summary>The size of the raw read buffer used by the stalled-body scenario.</summary>
    private const int ReadBufferBytes = 64;

    /// <summary>Reusable metadata for the streamed and uploaded people.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(TestingJsonContext.Default.Options) { TypeInfoResolver = TestingJsonContext.Default };

    /// <summary>Runs each deterministic streaming and time-control scenario.</summary>
    /// <returns>A task that faults when an observed result differs from the documented behavior.</returns>
    internal static async Task RunAsync()
    {
        await ShowEarlyItemsAsync();
        await ShowCancellationAsync();
        await ShowDisconnectAsync();
        await ShowStalledBodyAsync();
        await ShowFixedStreamsAsync();
        await ShowUploadAsync();
        await ShowLiveUploadAsync();
        await ShowBoundedCaptureAsync();
        await ShowSimulatedDelayAsync();
        await ShowVerificationTimeoutAsync();
    }

    /// <summary>Creates settings whose serializer has generated metadata for the people.</summary>
    /// <returns>Fresh settings for one handler.</returns>
    private static RefitSettings CreateSettings() => new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Creates a raw client for a handler that the scenario owns and disposes.</summary>
    /// <param name="http">The handler used instead of a real network connection.</param>
    /// <returns>A client that leaves the handler undisposed.</returns>
    private static HttpClient CreateClient(StubHttp http) => new(http, disposeHandler: false);

    /// <summary>Checks that the first person arrives before the second one is released.</summary>
    /// <returns>A task that completes after both people and the end of the body are observed.</returns>
    private static async Task ShowEarlyItemsAsync()
    {
        StreamSource source = new(StreamingContentFormat.JsonLines);
        using StubHttp http = new() { { Route.Get(LivePath), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());
        await using IAsyncEnumerator<TestingPerson> people = api.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

        source.Release(new TestingPerson(1, FirstName));
        SampleCheck.Equal(true, await people.MoveNextAsync());
        SampleCheck.Equal(FirstName, people.Current.Name);

        ValueTask<bool> second = people.MoveNextAsync();
        SampleCheck.Equal(false, second.IsCompleted); // Grace has not been released yet.

        source.Release(new TestingPerson(SecondId, SecondName));
        SampleCheck.Equal(true, await second);
        SampleCheck.Equal(SecondName, people.Current.Name);

        source.Complete();
        SampleCheck.Equal(false, await people.MoveNextAsync());
        await source.Closed; // Reaching the end disposed the response.
        SampleCheck.Equal(StreamedPeople, source.ReadChunks);
    }

    /// <summary>Checks that cancelling a stalled read closes the response.</summary>
    /// <returns>A task that completes after the cancellation and the closed body are observed.</returns>
    private static async Task ShowCancellationAsync()
    {
        StreamSource source = new();
        using StubHttp http = new() { { Route.Get(LivePath), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());
        using CancellationTokenSource cancellation = new();
        await using IAsyncEnumerator<TestingPerson> people = api.WatchAsync(cancellation.Token).GetAsyncEnumerator();

        source.Release(new TestingPerson(1, FirstName));
        SampleCheck.Equal(true, await people.MoveNextAsync());
        ValueTask<bool> stalled = people.MoveNextAsync();
        SampleCheck.Equal(false, stalled.IsCompleted);

        await cancellation.CancelAsync();
        bool cancelled = false;
        try
        {
            _ = await stalled;
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        SampleCheck.Equal(true, cancelled);
        await source.Closed; // The client disposed the response when the read was cancelled.
        SampleCheck.Equal(true, source.IsClosed);
    }

    /// <summary>Checks that a connection dropped after the first chunk delivers that chunk and then fails.</summary>
    /// <returns>A task that completes after the delivered person and the failure are observed.</returns>
    private static async Task ShowDisconnectAsync()
    {
        StreamSource source = new();
        source.Release(new TestingPerson(1, FirstName));
        source.Disconnect();
        using StubHttp http = new() { { Route.Get(LivePath), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());

        List<string> names = [];
        bool dropped = false;
        try
        {
            await foreach (TestingPerson person in api.WatchAsync(CancellationToken.None))
            {
                names.Add(person.Name);
            }
        }
        catch (HttpIOException error)
        {
            dropped = error.HttpRequestError == HttpRequestError.ResponseEnded;
        }

        SampleCheck.Equal(FirstName, string.Join(",", names));
        SampleCheck.Equal(true, dropped);
        SampleCheck.Equal(true, source.IsClosed);
    }

    /// <summary>Checks a response that sends its headers and then stalls its body until the reader gives up.</summary>
    /// <returns>A task that completes after the headers, the stalled read and the closed body are observed.</returns>
    private static async Task ShowStalledBodyAsync()
    {
        StreamSource source = new(StreamingContentFormat.ServerSentEvents);
        using StubHttp http = new() { { Route.Get("/events"), Reply.Stream(source) } };
        using HttpClient client = CreateClient(http);
        using CancellationTokenSource cancellation = new();

        HttpResponseMessage response = await client.GetAsync(new Uri($"{BaseUrl}/events"), HttpCompletionOption.ResponseHeadersRead);
        try
        {
            SampleCheck.Equal(HttpStatusCode.OK, response.StatusCode);
            SampleCheck.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

            Stream body = await response.Content.ReadAsStreamAsync();
            ValueTask<int> read = body.ReadAsync(new byte[ReadBufferBytes], cancellation.Token);
            SampleCheck.Equal(false, read.IsCompleted); // Headers arrived; the body has not.

            await cancellation.CancelAsync();
            bool cancelled = false;
            try
            {
                _ = await read;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            SampleCheck.Equal(true, cancelled);
            SampleCheck.Equal(false, source.IsClosed);
        }
        finally
        {
            response.Dispose();
        }

        SampleCheck.Equal(true, source.IsClosed); // Disposing the response closed the body.
    }

    /// <summary>Checks the fixed JSON Lines and server-sent events replies, which send one chunk per person.</summary>
    /// <returns>A task that completes after both formats are read.</returns>
    private static async Task ShowFixedStreamsAsync()
    {
        TestingPerson[] people = [new(1, FirstName), new(SecondId, SecondName)];
        using StubHttp http = new() { { Route.Get(LivePath), Reply.JsonLines(people) }, { Route.Get(LivePath), Reply.ServerSentEvents(people) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());

        foreach (string format in new[] { "JSON Lines", "server-sent events" })
        {
            List<string> names = [];
            await foreach (TestingPerson person in api.WatchAsync(CancellationToken.None))
            {
                names.Add(person.Name);
            }

            Console.WriteLine($"{format}: {string.Join(", ", names)}"); // Ada, Grace
            SampleCheck.Equal($"{FirstName},{SecondName}", string.Join(",", names));
        }

        await http.VerifyAllCalledAsync();
    }

    /// <summary>Checks that disabling capture lets the reply code read an upload the handler never buffered.</summary>
    /// <returns>A task that completes after the default and disabled capture policies are compared.</returns>
    private static async Task ShowUploadAsync()
    {
        int pulled = 0;
        IEnumerable<TestingPerson> Upload()
        {
            pulled++;
            yield return new(1, FirstName);
            pulled++;
            yield return new(SecondId, SecondName);
        }

        int pulledBeforeReply = -1;
        string uploaded = string.Empty;
        using StubHttp http = new()
        {
            {
                Route.Post("/people/import"),
                Reply.From(async request =>
                {
                    pulledBeforeReply = pulled;
                    uploaded = await request.Content!.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        http.RequestCapture = RequestCapture.None;
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());

        await api.ImportAsync(Upload());
        SampleCheck.Equal(0, pulledBeforeReply); // The handler left the upload for the reply code.
        SampleCheck.Equal(StreamedPeople, pulled);
        SampleCheck.Equal(StreamedPeople, uploaded.Split('\n').Length);
        SampleCheck.Equal(null, await http.LastRequestBodyAsync<TestingPerson>()); // Nothing was recorded.

        // The default policy buffers the whole upload before any reply code runs.
        pulled = 0;
        http.RequestCapture = RequestCapture.Full;
        http.Add(Route.Post("/people/import"), Reply.From(request =>
        {
            pulledBeforeReply = pulled;
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }));
        await api.ImportAsync(Upload());
        SampleCheck.Equal(StreamedPeople, pulledBeforeReply);
    }

    /// <summary>Produces people asynchronously, one at a time, so the request body is written as each becomes available.</summary>
    /// <param name="cancellationToken">The token the caller's send flows into this producer.</param>
    /// <returns>The people, yielded as they become available.</returns>
    private static async IAsyncEnumerable<TestingPerson> ProduceLiveAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new(1, FirstName);
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new(SecondId, SecondName);
    }

    /// <summary>
    /// Checks reading an asynchronous JSON Lines upload line by line as it arrives, using the cancellable
    /// <c>Reply.From</c> responder overload with capture disabled so the responder reads the live, unbuffered
    /// request stream instead of a body <see cref="StubHttp"/> already buffered.
    /// </summary>
    /// <returns>A task that completes after every line is read in arrival order and the upload is answered.</returns>
    private static async Task ShowLiveUploadAsync()
    {
        List<string> received = [];
        using StubHttp http = new()
        {
            {
                Route.Post("/people/import-live"),
                Reply.From(async (request, cancellationToken) =>
                {
                    await using Stream body = await request.Content!.ReadAsStreamAsync(cancellationToken);
                    using StreamReader reader = new(body);
                    string? line;
                    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                    {
                        received.Add(line);
                    }

                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        http.RequestCapture = RequestCapture.None;
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>(BaseUrl, CreateSettings());

        await api.ImportLiveAsync(ProduceLiveAsync(CancellationToken.None), CancellationToken.None);

        SampleCheck.Equal(StreamedPeople, received.Count);
        SampleCheck.Equal(true, received[0].Contains(FirstName, StringComparison.Ordinal));
        SampleCheck.Equal(true, received[1].Contains(SecondName, StringComparison.Ordinal));
    }

    /// <summary>Checks typed inspection under a byte limit, including an upload larger than the limit.</summary>
    /// <returns>A task that completes after the recorded and truncated bodies are checked.</returns>
    private static async Task ShowBoundedCaptureAsync()
    {
        using StubHttp http = new() { { Route.Post("/people"), Reply.From(EchoAsync) }, { Route.Post("/people"), Reply.From(EchoAsync) } };
        ITestingApi api = http.CreateGeneratedClient<ITestingApi>(BaseUrl, CreateSettings());

        http.RequestCapture = RequestCapture.Bounded(RoomyCaptureBytes);
        _ = await api.CreateAsync(new(SecondId, SecondName));
        TestingPerson? sent = await http.LastRequestBodyAsync<TestingPerson>();
        SampleCheck.Equal(SecondName, sent?.Name);

        http.RequestCapture = RequestCapture.Bounded(TinyCaptureBytes);
        _ = await api.CreateAsync(new(SecondId, SecondName));
        bool truncated = false;
        try
        {
            _ = await http.LastRequestBodyAsync<TestingPerson>();
        }
        catch (InvalidOperationException)
        {
            truncated = true;
        }

        SampleCheck.Equal(true, truncated);
    }

    /// <summary>Returns the request body as the JSON reply, reading it the way a server would.</summary>
    /// <param name="request">The request whose body is read.</param>
    /// <returns>A created response carrying the same JSON.</returns>
    private static async Task<HttpResponseMessage> EchoAsync(HttpRequestMessage request)
    {
        string json = await request.Content!.ReadAsStringAsync();
        return new(HttpStatusCode.Created) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    /// <summary>Checks that simulated latency waits for a fake clock instead of real time.</summary>
    /// <returns>A task that completes after the delayed reply arrives.</returns>
    private static async Task ShowSimulatedDelayAsync()
    {
        FakeTimeProvider clock = new();
        NetworkBehavior behavior = new(SimulationSeed) { Delay = TimeSpan.FromSeconds(DelaySeconds), Variance = 0, FailurePercent = 0 };
        using StubHttp http = new(behavior) { { Route.Get("/slow"), Reply.Text("done") } };
        http.TimeProvider = clock;
        using HttpClient client = CreateClient(http);

        Task<HttpResponseMessage> pending = client.GetAsync(new Uri($"{BaseUrl}/slow"));
        clock.Advance(TimeSpan.FromSeconds(DelaySeconds - 1));
        SampleCheck.Equal(false, pending.IsCompleted); // One simulated second is still outstanding.

        clock.Advance(TimeSpan.FromSeconds(1));
        using HttpResponseMessage response = await pending;
        SampleCheck.Equal("done", await response.Content.ReadAsStringAsync());
    }

    /// <summary>Checks that the verification timeout also follows the fake clock.</summary>
    /// <returns>A task that completes after the timed-out verification is observed.</returns>
    private static async Task ShowVerificationTimeoutAsync()
    {
        FakeTimeProvider clock = new();
        using StubHttp http = new() { { Route.Get("/expected"), Reply.Status(HttpStatusCode.OK) } };
        http.TimeProvider = clock;

        Task verification = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(VerifyTimeoutSeconds));
        SampleCheck.Equal(false, verification.IsCompleted);

        clock.Advance(TimeSpan.FromSeconds(VerifyTimeoutSeconds));
        bool timedOut = false;
        try
        {
            await verification;
        }
        catch (InvalidOperationException error)
        {
            timedOut = error.Message.Contains("/expected", StringComparison.Ordinal);
        }

        SampleCheck.Equal(true, timedOut);
    }
}
