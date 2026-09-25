// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises streamed replies, stalled bodies, upload capture policies and simulated time without real waits.</summary>
internal static class TestingStreaming
{
    /// <summary>Reusable metadata for the streamed and uploaded people.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(TestingJsonContext.Default.Options) { TypeInfoResolver = TestingJsonContext.Default };

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
    private static RefitSettings CreateSettings() => new RefitSettings(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Problem: How do you prove a streamed reply hands you the first item before the second one has even arrived?</summary>
    /// <returns>A task that completes after both people and the end of the body are observed.</returns>
    private static async Task ShowEarlyItemsAsync()
    {
        StreamSource source = new StreamSource(StreamingContentFormat.JsonLines);
        using StubHttp http = new StubHttp { { Route.Get("/people/live"), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());
        await using IAsyncEnumerator<TestingPerson> people = api.WatchAsync(CancellationToken.None).GetAsyncEnumerator();

        source.Release(new TestingPerson(1, "Ada"));
        bool gotFirst = await people.MoveNextAsync();
        string firstName = people.Current.Name; // "Ada"

        ValueTask<bool> second = people.MoveNextAsync();
        bool secondReadyEarly = second.IsCompleted; // false: Grace has not been released yet

        source.Release(new TestingPerson(2, "Grace"));
        bool gotSecond = await second;
        string secondName = people.Current.Name; // "Grace"

        source.Complete();
        bool gotThird = await people.MoveNextAsync(); // false: the source is complete
        await source.Closed; // reaching the end disposed the response

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(true, gotFirst);
        SampleCheck.Equal("Ada", firstName);
        SampleCheck.Equal(false, secondReadyEarly);
        SampleCheck.Equal(true, gotSecond);
        SampleCheck.Equal("Grace", secondName);
        SampleCheck.Equal(false, gotThird);
        SampleCheck.Equal(2, source.ReadChunks);
    }

    /// <summary>Problem: What happens to a stalled stream read when you cancel it?</summary>
    /// <returns>A task that completes after the cancellation and the closed body are observed.</returns>
    private static async Task ShowCancellationAsync()
    {
        StreamSource source = new StreamSource();
        using StubHttp http = new StubHttp { { Route.Get("/people/live"), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        await using IAsyncEnumerator<TestingPerson> people = api.WatchAsync(cancellation.Token).GetAsyncEnumerator();

        source.Release(new TestingPerson(1, "Ada"));
        bool gotFirst = await people.MoveNextAsync();
        Task<bool> stalled = people.MoveNextAsync().AsTask();
        bool stalledEarly = stalled.IsCompleted; // false: no second person has arrived

        await cancellation.CancelAsync();
        try
        {
            await stalled; // throws OperationCanceledException
        }
        catch (OperationCanceledException error)
        {
            Console.WriteLine(error.Message); // "A task was canceled."
        }

        await source.Closed; // the client disposed the response when the read was cancelled

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(true, gotFirst);
        SampleCheck.Equal(false, stalledEarly);
        SampleCheck.Equal(true, stalled.IsCanceled);
        SampleCheck.Equal(true, source.IsClosed);
    }

    /// <summary>Problem: How do you simulate a connection dropping mid-stream after some items already arrived?</summary>
    /// <returns>A task that completes after the delivered person and the failure are observed.</returns>
    private static async Task ShowDisconnectAsync()
    {
        StreamSource source = new StreamSource();
        source.Release(new TestingPerson(1, "Ada"));
        source.Disconnect();
        using StubHttp http = new StubHttp { { Route.Get("/people/live"), Reply.Stream(source) } };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());

        List<string> names = [];
        try
        {
            await foreach (TestingPerson person in api.WatchAsync(CancellationToken.None))
            {
                names.Add(person.Name); // "Ada" arrives before the drop
            }
        }
        catch (HttpIOException error)
        {
            Console.WriteLine(error.Message); // "Refit.Testing simulated disconnect. (ResponseEnded)"
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Ada", string.Join(",", names));
        SampleCheck.Equal(true, source.IsClosed);

        StreamSource verifySource = new StreamSource();
        verifySource.Release(new TestingPerson(1, "Ada"));
        verifySource.Disconnect();
        using StubHttp verifyHttp = new StubHttp { { Route.Get("/people/live"), Reply.Stream(verifySource) } };
        ITestingStreamingApi verifyApi = verifyHttp.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());
        bool dropped = false;
        try
        {
            await foreach (TestingPerson person in verifyApi.WatchAsync(CancellationToken.None))
            {
                _ = person;
            }
        }
        catch (HttpIOException error)
        {
            dropped = error.HttpRequestError == HttpRequestError.ResponseEnded;
        }

        SampleCheck.Equal(true, dropped);
    }

    /// <summary>Problem: How do you prove headers arrived while the body is still stalled?</summary>
    /// <returns>A task that completes after the headers, the stalled read and the closed body are observed.</returns>
    private static async Task ShowStalledBodyAsync()
    {
        StreamSource source = new StreamSource(StreamingContentFormat.ServerSentEvents);
        using StubHttp http = new StubHttp { { Route.Get("/events"), Reply.Stream(source) } };
        using HttpClient httpClient = new HttpClient(http, disposeHandler: false);
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://api.example.com/events"), HttpCompletionOption.ResponseHeadersRead);
        string? contentType = response.Content.Headers.ContentType?.MediaType; // "text/event-stream": headers arrived
        Stream body = await response.Content.ReadAsStreamAsync();
        Task<int> read = body.ReadAsync(new byte[64], cancellation.Token).AsTask();
        bool readCompletedEarly = read.IsCompleted; // false: the body itself has not arrived

        await cancellation.CancelAsync();
        try
        {
            await read; // never reached: the read is cancelled first
        }
        catch (OperationCanceledException error)
        {
            Console.WriteLine(error.Message); // "A task was canceled."
        }

        bool closedBeforeDispose = source.IsClosed; // false: cancelling the read did not close the body
        response.Dispose();
        bool closedAfterDispose = source.IsClosed; // true: disposing the response closed the body

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.OK, response.StatusCode);
        SampleCheck.Equal("text/event-stream", contentType);
        SampleCheck.Equal(false, readCompletedEarly);
        SampleCheck.Equal(true, read.IsCanceled);
        SampleCheck.Equal(false, closedBeforeDispose);
        SampleCheck.Equal(true, closedAfterDispose);
    }

    /// <summary>Problem: How do you reply with a ready-made JSON Lines or server-sent-events stream instead of controlling it by hand?</summary>
    /// <returns>A task that completes after both formats are read.</returns>
    private static async Task ShowFixedStreamsAsync()
    {
        TestingPerson[] people = [new TestingPerson(1, "Ada"), new TestingPerson(2, "Grace")];
        using StubHttp http = new StubHttp
        {
            {
                Route.Get("/people/live"),
                Reply.JsonLines(people)
            },
            {
                Route.Get("/people/live"),
                Reply.ServerSentEvents(people)
            },
        };
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());

        List<string> jsonLinesNames = [];
        await foreach (TestingPerson person in api.WatchAsync(CancellationToken.None))
        {
            jsonLinesNames.Add(person.Name);
        }

        List<string> serverSentEventNames = [];
        await foreach (TestingPerson person in api.WatchAsync(CancellationToken.None))
        {
            serverSentEventNames.Add(person.Name); // Ada, Grace: same people, delivered as server-sent events this time
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Ada,Grace", string.Join(",", jsonLinesNames));
        SampleCheck.Equal("Ada,Grace", string.Join(",", serverSentEventNames));
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Problem: How do you let your reply code read an upload stream directly, instead of one StubHttp already buffered?</summary>
    /// <returns>A task that completes after both capture policies are compared.</returns>
    private static async Task ShowUploadAsync()
    {
        int pulled = 0;
        IEnumerable<TestingPerson> UploadPeople()
        {
            pulled++;
            yield return new TestingPerson(1, "Ada");
            pulled++;
            yield return new TestingPerson(2, "Grace");
        }

        int pulledBeforeReply = -1;
        using StubHttp http = new StubHttp
        {
            {
                Route.Post("/people/import"),
                Reply.From(async request =>
                {
                    pulledBeforeReply = pulled;
                    await request.Content!.ReadAsStringAsync(); // read the whole upload
                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        http.RequestCapture = RequestCapture.None; // stops StubHttp from reading the body before the reply above does
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());

        await api.ImportAsync(UploadPeople());
        int pulledWithNoCapture = pulledBeforeReply; // 0: the handler left the whole upload for the reply code
        int totalPulled = pulled; // 2
        TestingPerson? recorded = await http.LastRequestBodyAsync<TestingPerson>(); // null: RequestCapture.None recorded nothing

        pulled = 0; // the second half repeats the upload with RequestCapture.Full
        http.RequestCapture = RequestCapture.Full; // buffers the whole upload before any reply code runs
        http.Add(Route.Post("/people/import"), Reply.From(request =>
        {
            pulledBeforeReply = pulled;
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }));
        await api.ImportAsync(UploadPeople());
        int pulledWithFullCapture = pulledBeforeReply; // 2: everything was pulled before the reply ran

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(0, pulledWithNoCapture);
        SampleCheck.Equal(2, totalPulled);
        SampleCheck.Equal(null, recorded);
        SampleCheck.Equal(2, pulledWithFullCapture);
    }

    /// <summary>Problem: How do you read an uploaded JSON Lines body, one line at a time, from inside a responder?</summary>
    /// <returns>A task that completes after every uploaded line is read.</returns>
    private static async Task ShowLiveUploadAsync()
    {
        static async IAsyncEnumerable<TestingPerson> UploadPeopleAsync()
        {
            yield return new TestingPerson(1, "Ada");
            yield return new TestingPerson(2, "Grace");
        }

        List<string> lines = [];
        using StubHttp http = new StubHttp
        {
            {
                Route.Post("/people/import-live"),
                Reply.From(async (request, cancellationToken) =>
                {
                    await using Stream body = await request.Content!.ReadAsStreamAsync(cancellationToken);
                    using StreamReader reader = new StreamReader(body);
                    string? line;
                    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                    {
                        lines.Add(line);
                    }

                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };

        // RequestCapture.None stops StubHttp from reading the body itself, so the responder above is the
        // first (and only) code to read the uploaded stream.
        http.RequestCapture = RequestCapture.None;
        ITestingStreamingApi api = http.CreateGeneratedClient<ITestingStreamingApi>("https://api.example.com", CreateSettings());

        await api.ImportLiveAsync(UploadPeopleAsync(), CancellationToken.None); // lines now has one JSON object per uploaded person

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(2, lines.Count);
        SampleCheck.Equal(true, lines[0].Contains("Ada", StringComparison.Ordinal));
        SampleCheck.Equal(true, lines[1].Contains("Grace", StringComparison.Ordinal));
    }

    /// <summary>Problem: How do you cap how many bytes of a request body StubHttp captures for inspection?</summary>
    /// <returns>A task that completes after both the recorded and the truncated bodies are checked.</returns>
    private static async Task ShowBoundedCaptureAsync()
    {
        // Reading the body is what fills a bounded capture, so the reply echoes the uploaded person back.
        StubResponse echo = Reply.From(static async request =>
        {
            string json = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });

        // Each route answers once, and this sample sends two requests.
        using StubHttp http = new StubHttp
        {
            { Route.Post("/people"), echo },
            { Route.Post("/people"), echo },
        };
        ITestingApi api = http.CreateGeneratedClient<ITestingApi>("https://api.example.com", CreateSettings());

        http.RequestCapture = RequestCapture.Bounded(1024); // roomy enough for one serialized person
        await api.CreateAsync(new TestingPerson(2, "Grace"));
        TestingPerson? recorded = await http.LastRequestBodyAsync<TestingPerson>(); // recorded?.Name == "Grace"

        http.RequestCapture = RequestCapture.Bounded(4); // far too small for one serialized person
        await api.CreateAsync(new TestingPerson(2, "Grace"));
        try
        {
            await http.LastRequestBodyAsync<TestingPerson>(); // throws: the capture was truncated
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine(error.Message); // "The request body exceeded the capture limit of 4 bytes; raise RequestCapture.Bounded or use RequestCapture.Full."
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Grace", recorded?.Name);
        bool truncated = false;
        try
        {
            await http.LastRequestBodyAsync<TestingPerson>(); // re-runs the same truncated read to confirm it still throws
        }
        catch (InvalidOperationException)
        {
            truncated = true;
        }

        SampleCheck.Equal(true, truncated);
    }

    /// <summary>Problem: How do you make simulated network delay respond to a fake clock instead of real time?</summary>
    /// <returns>A task that completes after the delayed reply arrives.</returns>
    private static async Task ShowSimulatedDelayAsync()
    {
        FakeTimeProvider clock = new FakeTimeProvider();
        NetworkBehavior behavior = new NetworkBehavior(7) // 7 is the random seed, so the random choices repeat on every run
        {
            Delay = TimeSpan.FromSeconds(2),
            Variance = 0,
            FailurePercent = 0,
        };
        using StubHttp http = new StubHttp(behavior) { { Route.Get("/slow"), Reply.Text("done") } };
        http.TimeProvider = clock;
        using HttpClient httpClient = new HttpClient(http, disposeHandler: false);

        Task<HttpResponseMessage> pending = httpClient.GetAsync(new Uri("https://api.example.com/slow"));
        clock.Advance(TimeSpan.FromSeconds(1));
        bool stillWaiting = pending.IsCompleted; // false: one simulated second is still outstanding

        clock.Advance(TimeSpan.FromSeconds(1));
        using HttpResponseMessage response = await pending; // arrives once the fake clock has advanced the full 2-second delay
        string body = await response.Content.ReadAsStringAsync(); // "done"

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(false, stillWaiting);
        SampleCheck.Equal("done", body);
    }

    /// <summary>Problem: Does the verification timeout also follow a fake clock instead of real time?</summary>
    /// <returns>A task that completes after the timed-out verification is observed.</returns>
    private static async Task ShowVerificationTimeoutAsync()
    {
        FakeTimeProvider clock = new FakeTimeProvider();
        using StubHttp http = new StubHttp { { Route.Get("/expected"), Reply.Status(HttpStatusCode.OK) } };
        http.TimeProvider = clock;

        Task verification = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(5));
        bool doneEarly = verification.IsCompleted; // false: nothing has called /expected yet

        clock.Advance(TimeSpan.FromSeconds(5));
        try
        {
            await verification; // throws once the simulated clock reaches the 5-second timeout
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine(error.Message); // "1 expected request(s) were not made:\n  - GET /expected"
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(false, doneEarly);
        SampleCheck.Equal(true, verification.IsFaulted);
        SampleCheck.Equal(true, verification.Exception?.InnerException?.Message.Contains("/expected", StringComparison.Ordinal));
    }
}
