// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises handler matching, typed capture, faults and manually configured responses.</summary>
internal static class Testing
{
    /// <summary>The name shared by typed replies and response stubs.</summary>
    private const string PersonName = "Ada";

    /// <summary>The JSON body reused by raw replies and the charset reproduction.</summary>
    private const string PersonJson = "{\"id\":1,\"name\":\"Ada\"}";

    /// <summary>The name checked in captured POST bodies.</summary>
    private const string CreatedName = "Grace";

    /// <summary>The base address used by generated and reflection-capable test clients.</summary>
    private const string BaseUrl = "https://people.example";

    /// <summary>The actual response returned by the duplicate-query discrepancy.</summary>
    private const string AcceptedReply = "accepted";

    /// <summary>The configured network-failure message checked through the raw client.</summary>
    private const string FailureMessage = "Test connection failure.";

    /// <summary>The number of entries in the method-factory table.</summary>
    private const int RouteCount = 9;

    /// <summary>The identifier sent in the captured POST body.</summary>
    private const int CreatedPersonId = 2;

    /// <summary>The seed used for standalone network calculation checks.</summary>
    private const int SimulationSeed = 7;

    /// <summary>The expected default base delay, in seconds.</summary>
    private const int DefaultDelaySeconds = 2;

    /// <summary>Reusable metadata for both directions of the typed testing scenario.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(TestingJsonContext.Default.Options) { TypeInfoResolver = TestingJsonContext.Default };

    /// <summary>Runs independent testing scenarios and deterministic discrepancy assertions.</summary>
    /// <returns>A task that faults when an observed result differs from the source-backed contract.</returns>
    internal static async Task RunAsync()
    {
        await ShowClientAsync();
        await ShowCapturedEncodingAsync();
        await ShowRoutesAsync();
        await ShowMatchersAsync();
        await ShowRepliesAsync();
        await ShowVerificationAsync();
        await ShowDuplicateQueryAsync();
        await ShowAdditionalRouteAsync();
        await ShowOneShotRaceAsync();
        await ShowFaultsAsync();
        await ShowResponseStubsAsync();
#if !NATIVE_AOT
        await TestingReflection.RunAsync();
#endif
    }

    /// <summary>Creates independent handler settings with shared generated JSON metadata.</summary>
    /// <returns>Fresh settings whose serializer has metadata for the test body.</returns>
    private static RefitSettings CreateSettings() => new(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Checks consumed expectations synchronously without waiting for response completion.</summary>
    /// <param name="http">The handler whose one-shot expectations must be consumed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Verify(StubHttp http) => http.VerifyAllCalled();

    /// <summary>Creates an isolated client with the scenario's supplied handler.</summary>
    /// <param name="http">The handler used instead of a real network connection.</param>
    /// <returns>A client owned and disposed by its scenario.</returns>
    private static HttpClient CreateClient(StubHttp http) => new(http, disposeHandler: false);

    /// <summary>Checks typed generated requests, serializer adoption and buffered-body inspection.</summary>
    /// <returns>A task that completes after the typed scenario assertions.</returns>
    private static async Task ShowClientAsync()
    {
        using StubHttp http = new()
        {
            {
                Route.Get("/people/{id}"),
                Reply.With(new TestingPerson(1, PersonName))
            },
            {
                Route.Post("/people"),
                Reply.With(new TestingPerson(CreatedPersonId, CreatedName), HttpStatusCode.Created)
            },
        };
        ITestingApi api = http.CreateGeneratedClient<ITestingApi>(BaseUrl, CreateSettings());
        TestingPerson person = await api.GetAsync(1);
        SampleCheck.Equal(PersonName, person.Name);
        _ = await api.CreateAsync(new(CreatedPersonId, CreatedName));
        TestingPerson? sent = await http.LastRequestBodyAsync<TestingPerson>();
        SampleCheck.Equal(CreatedName, sent?.Name);
        SampleCheck.Equal(sent, await http.RequestBodyAsync<TestingPerson>(1));
        SampleCheck.Equal(HttpMethod.Post, http.Requests[1].Method);
        Verify(http);

        using StubHttp wiring = new();
        RefitSettings fresh = wiring.ToSettings();
        SampleCheck.Equal(wiring, fresh.HttpMessageHandlerFactory!());
        RefitSettings supplied = CreateSettings();
        SampleCheck.Equal(supplied, wiring.ToSettings(supplied));
        ITestingApi generated = wiring.CreateGeneratedClient<ITestingApi>(BaseUrl);
        SampleCheck.Equal(true, generated is not null);
        SampleCheck.Equal(wiring, supplied.HttpMessageHandlerFactory!());
    }

    /// <summary>Reproduces typed capture losing a non-UTF-8 body's declared charset.</summary>
    /// <returns>A task that completes after the original model and failed capture are asserted.</returns>
    private static async Task ShowCapturedEncodingAsync()
    {
        RefitSettings settings = CreateSettings();
        using StubHttp http = new() { { Route.Post("/encoded"), Reply.Status(HttpStatusCode.OK) } };
        _ = http.ToSettings(settings);
        using StringContent original = new(PersonJson, Encoding.Unicode, "application/json");
        TestingPerson? expected = await settings.ContentSerializer.FromHttpContentAsync<TestingPerson>(original);
        SampleCheck.Equal(PersonName, expected?.Name);
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage reply = await client.PostAsync(new Uri("https://people.example/encoded"), original);
        SampleCheck.Equal(HttpStatusCode.OK, reply.StatusCode);
        bool corrupted = false;
        try
        {
            _ = await http.LastRequestBodyAsync<TestingPerson>();
        }
        catch (JsonException)
        {
            corrupted = true;
        }

        SampleCheck.Equal(true, corrupted);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Checks method factories and fallback priority through a raw HTTP client.</summary>
    /// <returns>A task that completes after every method-specific route is sent.</returns>
    private static async Task ShowRoutesAsync()
    {
        using StubHttp http = new()
        {
            {
                Route.Fallback(),
                Reply.Status(HttpStatusCode.NotFound)
            },
            {
                Route.Get("/get"),
                Reply.Status(HttpStatusCode.OK)
            },
            {
                Route.Post("/post"),
                Reply.Status(HttpStatusCode.Created)
            },
            {
                Route.Put("/put"),
                Reply.Status(HttpStatusCode.NoContent)
            },
            {
                Route.Delete("/delete"),
                Reply.Status(HttpStatusCode.NoContent)
            },
            {
                Route.Patch("/patch"),
                Reply.Status(HttpStatusCode.NoContent)
            },
            {
                Route.Head("/head"),
                Reply.Status(HttpStatusCode.OK)
            },
            {
                Route.For(HttpMethod.Options, "/options"),
                Reply.Status(HttpStatusCode.OK)
            },
            {
                Route.Any("/any"),
                Reply.Status(HttpStatusCode.Accepted)
            },
        };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage get = await client.GetAsync(new Uri("https://people.example/get"));
        SampleCheck.Equal(HttpStatusCode.OK, get.StatusCode);
        using HttpResponseMessage missing = await client.GetAsync(new Uri("https://people.example/missing"));
        SampleCheck.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        await SendOtherMethodsAsync(client);
        Verify(http);
        EnumerateRoutes(http);
    }

    /// <summary>Sends the remaining method-factory routes after the GET and fallback checks.</summary>
    /// <param name="client">The client attached to the method-factory table.</param>
    /// <returns>A task that completes after each route returns a successful status.</returns>
    private static async Task SendOtherMethodsAsync(HttpClient client)
    {
        foreach ((HttpMethod method, string path)in new (HttpMethod, string)[]
        {
            (HttpMethod.Post, "post"),
            (HttpMethod.Put, "put"),
            (HttpMethod.Delete, "delete"),
            (HttpMethod.Patch, "patch"),
            (HttpMethod.Head, "head"),
            (HttpMethod.Options, "options"),
            (HttpMethod.Trace, "any"),
        }

        )
        {
            using HttpRequestMessage request = new(method, $"https://people.example/{path}");
            using HttpResponseMessage response = await client.SendAsync(request);
            SampleCheck.Equal(true, response.IsSuccessStatusCode);
        }
    }

    /// <summary>Checks configured route snapshots through both enumeration interfaces.</summary>
    /// <param name="http">The complete method-factory table.</param>
    private static void EnumerateRoutes(StubHttp http)
    {
        int genericCount = 0;
        foreach (RouteMatcher route in (IEnumerable<RouteMatcher>)http)
        {
            SampleCheck.Equal(true, route.Template.Length > 0);
            genericCount++;
        }

        SampleCheck.Equal(RouteCount, genericCount);
        int count = 0;
        foreach (object route in (IEnumerable)http)
        {
            SampleCheck.Equal(true, route is RouteMatcher);
            count++;
        }

        SampleCheck.Equal(RouteCount, count);
    }

    /// <summary>Checks every configured matcher condition against one reusable form request.</summary>
    /// <returns>A task that completes after the matching response is checked.</returns>
    private static async Task ShowMatchersAsync()
    {
        RouteMatcher route = new()
        {
            Method = HttpMethod.Post,
            Template = "/people/{id}",
            Query = [("mode", "short")],
            ExactQuery = "extra=1&mode=short",
            ExactQueryParams = [("mode", "short"), ("extra", "1")],
            Headers = [("X-Test", "yes"), ("Content-Type", "application/x-www-form-urlencoded")],
            Body = "name=Ada+Lovelace&extra=1",
            FormData = [("name", "Ada Lovelace")],
            Where = static request => request.RequestUri!.Host == "people.example",
            WhereAsync = static async request => (await request.Content!.ReadAsStringAsync()).Contains(PersonName, StringComparison.Ordinal),
            Reusable = true,
            Fallback = false,
        };
        using StubHttp http = new() { { route, Reply.Text("matched") } };
        using HttpClient client = CreateClient(http);
        using HttpRequestMessage request = new(HttpMethod.Post, "https://people.example/people/7?mode=short&extra=1")
        {
            Content = new FormUrlEncodedContent([new("name", "Ada Lovelace"), new("extra", "1")]),
        };
        request.Headers.Add("X-Test", "yes");
        using HttpResponseMessage response = await client.SendAsync(request);
        SampleCheck.Equal("matched", await response.Content.ReadAsStringAsync());
        Verify(http);
    }

    /// <summary>Checks raw JSON, text, bytes and both full-response responder overloads.</summary>
    /// <returns>A task that completes after every reply route is sent.</returns>
    private static async Task ShowRepliesAsync()
    {
        using StubHttp http = new()
        {
            {
                Route.Get("/json"),
                Reply.Json(PersonJson)
            },
            {
                Route.Get("/rejected"),
                Reply.Json("{\"error\":\"invalid\"}", HttpStatusCode.BadRequest)
            },
            {
                Route.Get("/text"),
                Reply.Text("hello")
            },
            {
                Route.Get("/html"),
                Reply.Text("<p>hello</p>", "text/html")
            },
            {
                Route.Get("/content"),
                Reply.Content(new ByteArrayContent([1]))
            },
            {
                Route.Get("/status"),
                Reply.Status(HttpStatusCode.NoContent)
            },
            {
                Route.Get("/sync"),
                Reply.From(static request => new(HttpStatusCode.Accepted) { Content = new StringContent(request.RequestUri!.AbsolutePath) })
            },
            {
                Route.Post("/async"),
                Reply.From(static async request => new(HttpStatusCode.OK) { Content = new StringContent(await request.Content!.ReadAsStringAsync()) })
            },
        };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage html = await client.GetAsync(new Uri("https://people.example/html"));
        SampleCheck.Equal("text/html", html.Content.Headers.ContentType?.MediaType);
        using HttpResponseMessage echoed = await client.PostAsync(new Uri("https://people.example/async"), new StringContent(PersonName));
        SampleCheck.Equal(PersonName, await echoed.Content.ReadAsStringAsync());
        foreach ((string path, HttpStatusCode status)in new (string, HttpStatusCode)[]
        {
            ("json", HttpStatusCode.OK),
            ("rejected", HttpStatusCode.BadRequest),
            ("text", HttpStatusCode.OK),
            ("content", HttpStatusCode.OK),
            ("status", HttpStatusCode.NoContent),
            ("sync", HttpStatusCode.Accepted),
        }

        )
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri($"https://people.example/{path}"));
            SampleCheck.Equal(status, response.StatusCode);
        }

        await ShowReplyPropertiesAsync();
        Verify(http);
    }

    /// <summary>Checks asynchronous-responder precedence over conflicting status and body properties.</summary>
    /// <returns>A task that completes after the selected response status is checked.</returns>
    private static async Task ShowReplyPropertiesAsync()
    {
        StubResponse properties = new()
        {
            Status = HttpStatusCode.Created,
            Json = "{}",
            Text = "text",
            ContentType = "text/plain",
            Content = new StringContent("explicit"),
            Responder = static _ => new(HttpStatusCode.Accepted),
            ResponderAsync = static _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)),
        };
        SampleCheck.Equal(HttpStatusCode.Created, properties.Status);
        SampleCheck.Equal(true, properties.ResponderAsync is not null);
        using StubHttp http = new() { { Route.Get("/precedence"), properties } };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage precedence = await client.GetAsync(new Uri("https://people.example/precedence"));
        SampleCheck.Equal(HttpStatusCode.NoContent, precedence.StatusCode);
        properties.Content.Dispose();
    }

    /// <summary>Checks waiting verification, timeout failure and bodyless capture.</summary>
    /// <returns>A task that completes after both present and absent expectations are checked.</returns>
    private static async Task ShowVerificationAsync()
    {
        using StubHttp http = new() { { Route.Get("/expected"), Reply.Text("received") } };
        using HttpClient client = CreateClient(http);
        Task waiting = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(1));
        SampleCheck.Equal(false, waiting.IsCompleted);
        using HttpResponseMessage response = await client.GetAsync(new Uri("https://people.example/expected"));
        await waiting;
        await http.VerifyAllCalledAsync();
        Verify(http);
        SampleCheck.Equal(null, await http.LastRequestBodyAsync<TestingPerson>());
        using StubHttp unmet = new() { { Route.Get("/unmet"), Reply.Status(HttpStatusCode.OK) } };
        bool rejected = false;
        try
        {
            await unmet.VerifyAllCalledAsync(TimeSpan.Zero);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        SampleCheck.Equal(true, rejected);
    }

    /// <summary>Reproduces both exact-query matchers accepting an extra pair in place of a duplicate.</summary>
    /// <returns>A task that completes after both incorrect matches are asserted.</returns>
    private static async Task ShowDuplicateQueryAsync()
    {
        using StubHttp http = new()
        {
            {
                new RouteMatcher { Template = "/query", ExactQueryParams = [("a", "1"), ("a", "1")] },
                Reply.Text(AcceptedReply)
            },
            {
                new RouteMatcher { Template = "/query", ExactQuery = "a=1&a=1" },
                Reply.Text(AcceptedReply)
            },
        };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage pairs = await client.GetAsync(new Uri("https://people.example/query?a=1&b=2"));
        using HttpResponseMessage encoded = await client.GetAsync(new Uri("https://people.example/query?a=1&b=2"));
        SampleCheck.Equal(AcceptedReply, await pairs.Content.ReadAsStringAsync());
        SampleCheck.Equal(AcceptedReply, await encoded.Content.ReadAsStringAsync());
        Verify(http);
    }

    /// <summary>Reproduces immediate async verification failure after adding another expectation.</summary>
    /// <returns>A task that completes after the immediate failure and final successful send.</returns>
    private static async Task ShowAdditionalRouteAsync()
    {
        using StubHttp http = new() { { Route.Get("/first"), Reply.Status(HttpStatusCode.OK) } };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage first = await client.GetAsync(new Uri("https://people.example/first"));
        await http.VerifyAllCalledAsync();
        http.Add(Route.Get("/second"), Reply.Status(HttpStatusCode.OK));
        Task verification = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(1));
        SampleCheck.Equal(true, verification.IsCompleted);
        bool rejected = false;
        try
        {
            await verification;
        }
        catch (InvalidOperationException error)
        {
            rejected = error.Message.Contains("/second", StringComparison.Ordinal);
        }

        SampleCheck.Equal(true, rejected);
        using HttpResponseMessage second = await client.GetAsync(new Uri("https://people.example/second"));
        Verify(http);
    }

    /// <summary>Reproduces two requests receiving a single expectation's reply after both pass matching.</summary>
    /// <returns>A task that completes after the deterministic duplicate replies are asserted.</returns>
    private static async Task ShowOneShotRaceAsync()
    {
        const int concurrentRequests = 2;
        int arrivals = 0;
        using SemaphoreSlim bothMatching = new(0, concurrentRequests);
        RouteMatcher route = new()
        {
            Template = "/one-shot",
            WhereAsync = async _ =>
            {
                if (Interlocked.Increment(ref arrivals) == concurrentRequests)
                {
                    int previousCount = bothMatching.Release(concurrentRequests);
                    SampleCheck.Equal(0, previousCount);
                }

                await bothMatching.WaitAsync();
                return true;
            },
        };
        using StubHttp http = new() { { route, Reply.Text(AcceptedReply) } };
        using HttpClient client = CreateClient(http);
        Uri address = new("https://people.example/one-shot");
        Task<HttpResponseMessage> first = client.GetAsync(address);
        Task<HttpResponseMessage> second = client.GetAsync(address);
        using HttpResponseMessage firstReply = await first;
        using HttpResponseMessage secondReply = await second;
        SampleCheck.Equal(concurrentRequests, arrivals);
        SampleCheck.Equal(concurrentRequests, http.Requests.Count);
        SampleCheck.Equal(AcceptedReply, await firstReply.Content.ReadAsStringAsync());
        SampleCheck.Equal(AcceptedReply, await secondReply.Content.ReadAsStringAsync());
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Checks standalone seeded calculations and both injected failure kinds.</summary>
    /// <returns>A task that completes after the response and exception assertions.</returns>
    private static async Task ShowFaultsAsync()
    {
        NetworkBehavior defaults = new();
        NetworkBehavior behavior = new(SimulationSeed)
        {
            Delay = TimeSpan.Zero,
            Variance = 0,
            FailurePercent = 0,
            ErrorPercent = 1,
            ErrorStatusCode = HttpStatusCode.ServiceUnavailable,
            FailureFactory = static () => new HttpRequestException(FailureMessage),
        };
        SampleCheck.Equal(TimeSpan.FromSeconds(DefaultDelaySeconds), defaults.Delay);
        SampleCheck.Equal(TimeSpan.Zero, behavior.NextDelay());
        SampleCheck.Equal(false, behavior.NextIsFailure());
        SampleCheck.Equal(true, behavior.NextIsError());
        SampleCheck.Equal(FailureMessage, behavior.CreateFailure().Message);
        using HttpResponseMessage standalone = behavior.CreateErrorResponse();
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, standalone.StatusCode);

        using StubHttp http = new(behavior) { { Route.Get("/fault"), Reply.Text("normal") } };
        using HttpClient client = CreateClient(http);
        using HttpResponseMessage error = await client.GetAsync(new Uri("https://people.example/fault"));
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Verify(http);
        behavior.FailurePercent = 1;
        http.Add(Route.Get("/failure"), Reply.Text("normal"));
        bool failed = false;
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri("https://people.example/failure"));
        }
        catch (HttpRequestException cause)
        {
            failed = cause.Message == FailureMessage;
        }

        SampleCheck.Equal(true, failed);
        http.Behavior = null;
    }

    /// <summary>Checks independently configured response metadata, interface guards and typed errors.</summary>
    /// <returns>A task that completes after manually configured response states are checked.</returns>
    private static async Task ShowResponseStubsAsync()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://people.example/people/1");
        using HttpResponseMessage message = new(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(PersonJson), };
        StubApiResponse<TestingPerson> stub = new()
        {
            Content = new(1, PersonName),
            HasContent = true,
            IsSuccessfulWithContent = true,
            Headers = message.Headers,
            ContentHeaders = message.Content.Headers,
            IsSuccessStatusCode = true,
            IsSuccessful = true,
            IsReceived = true,
            StatusCode = message.StatusCode,
            ReasonPhrase = message.ReasonPhrase,
            RequestMessage = request,
            Version = message.Version,
            Error = null,
        };
        CheckResponse(stub);
        SampleCheck.Equal(false, stub.HasRequestError(out _));
        SampleCheck.Equal(false, stub.HasResponseError(out _));
        stub.Dispose();

        ApiRequestException sendError = new("Connection failed.", request, request.Method, CreateSettings());
        using StubApiResponse<TestingPerson> failed = new() { Error = sendError };
        SampleCheck.Equal(true, failed.HasRequestError(out ApiRequestException? captured));
        SampleCheck.Equal(sendError, captured);
        using HttpResponseMessage rejected = new(HttpStatusCode.BadRequest) { RequestMessage = request };
        ApiException replyError = await ApiException.Create(request, request.Method, rejected, CreateSettings());
        using StubApiResponse<TestingPerson> refused = new()
        {
            Error = replyError,
            IsReceived = true,
            StatusCode = HttpStatusCode.BadRequest,
            Headers = rejected.Headers,
            ContentHeaders = rejected.Content.Headers,
            Version = rejected.Version,
            ReasonPhrase = rejected.ReasonPhrase,
            RequestMessage = request,
        };
        SampleCheck.Equal(true, refused.HasResponseError(out ApiException? capturedReply));
        SampleCheck.Equal(replyError, capturedReply);
        using ApiResponse<TestingPerson> real = new(message, new(1, PersonName), CreateSettings());
        CheckResponse(real);
    }

    /// <summary>Checks typed content through the interface's non-null success contract.</summary>
    /// <param name="response">A real response or manually configured response stub.</param>
    private static void CheckResponse(IApiResponse<TestingPerson> response)
    {
        if (response.IsSuccessfulWithContent)
        {
            SampleCheck.Equal(PersonName, response.Content.Name);
        }
    }
}
