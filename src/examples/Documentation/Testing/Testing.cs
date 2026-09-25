// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.Net;
using System.Text;
using System.Text.Json;
using Refit.Testing;

namespace Refit.Documentation;

/// <summary>Exercises handler matching, typed capture, faults and manually configured responses.</summary>
internal static class Testing
{
    /// <summary>Reusable metadata for both directions of the typed testing scenario.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(TestingJsonContext.Default.Options) { TypeInfoResolver = TestingJsonContext.Default };

    /// <summary>Runs independent testing scenarios and deterministic discrepancy assertions.</summary>
    /// <returns>A task that faults when an observed result differs from the source-backed contract.</returns>
    internal static async Task RunAsync()
    {
        await ShowClientAsync();
        await ShowSettingsWiringAsync();
        await ShowRoutesAsync();
        await ShowMatchersAsync();
        await ShowDuplicateQueryAsync();
        await ShowRepliesAsync();
        await ShowReplyPropertiesAsync();
        await ShowVerificationAsync();
        await ShowCapturedEncodingAsync();
        await ShowAdditionalRouteAsync();
        await ShowOneShotRaceAsync();
        await ShowNetworkBehaviorMembersAsync();
        await ShowFaultsAsync();
        await ShowResponseStubsAsync();
#if !NATIVE_AOT
        await TestingReflection.RunAsync();
#endif
    }

    /// <summary>Creates independent handler settings with shared generated JSON metadata.</summary>
    /// <returns>Fresh settings whose serializer has metadata for the test body.</returns>
    private static RefitSettings CreateSettings() => new RefitSettings(new SystemTextJsonContentSerializer(JsonOptions));

    /// <summary>Checks consumed expectations synchronously without waiting for response completion.</summary>
    /// <param name="http">The handler whose one-shot expectations must be consumed.</param>
    private static void Verify(StubHttp http) => http.VerifyAllCalled();

    /// <summary>Creates an isolated client with the scenario's supplied handler.</summary>
    /// <param name="http">The handler used instead of a real network connection.</param>
    /// <returns>A client owned and disposed by its scenario.</returns>
    private static HttpClient CreateClient(StubHttp http) => new HttpClient(http, disposeHandler: false);

    /// <summary>Problem: How do you send a generated Refit request through a stub and inspect what the client actually sent?</summary>
    /// <returns>A task that completes after the typed reply and the captured request body are checked.</returns>
    private static async Task ShowClientAsync()
    {
        using StubHttp http = new StubHttp
        {
            {
                Route.Get("/people/{id}"),
                Reply.With(new TestingPerson(1, "Ada"))
            },
            {
                Route.Post("/people"),
                Reply.With(new TestingPerson(2, "Grace"), HttpStatusCode.Created)
            },
        };
        ITestingApi api = http.CreateGeneratedClient<ITestingApi>("https://api.example.com", CreateSettings());

        TestingPerson person = await api.GetAsync(1); // person.Name == "Ada"
        TestingPerson created = await api.CreateAsync(new TestingPerson(2, "Grace")); // created.Name == "Grace"
        TestingPerson? sent = await http.LastRequestBodyAsync<TestingPerson>(); // sent?.Name == "Grace"

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Ada", person.Name);
        SampleCheck.Equal("Grace", created.Name);
        SampleCheck.Equal("Grace", sent?.Name);
        SampleCheck.Equal(sent, await http.RequestBodyAsync<TestingPerson>(1));
        SampleCheck.Equal(HttpMethod.Post, http.Requests[1].Method);
        Verify(http);
    }

    /// <summary>Problem: How do you get a RefitSettings that already points at a stub instead of the real network?</summary>
    /// <returns>A task that completes after both settings-wiring paths are checked.</returns>
    private static Task ShowSettingsWiringAsync()
    {
        using StubHttp http = new StubHttp();
        RefitSettings fresh = http.ToSettings(); // fresh.HttpMessageHandlerFactory!() == http
        RefitSettings supplied = CreateSettings();
        RefitSettings wired = http.ToSettings(supplied); // wired is the same instance as supplied
        ITestingApi api = http.CreateGeneratedClient<ITestingApi>("https://api.example.com");

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(http, fresh.HttpMessageHandlerFactory!());
        SampleCheck.Equal(supplied, wired);
        SampleCheck.Equal(http, supplied.HttpMessageHandlerFactory!());
        SampleCheck.Equal(true, api is not null);
        return Task.CompletedTask;
    }

    /// <summary>Problem: Why does inspecting a captured body with LastRequestBodyAsync throw for a body that was not sent as UTF-8?</summary>
    /// <returns>A task that completes after the original body and the failed capture are checked.</returns>
    private static async Task ShowCapturedEncodingAsync()
    {
        RefitSettings settings = CreateSettings();
        using StubHttp http = new StubHttp { { Route.Post("/encoded"), Reply.Status(HttpStatusCode.OK) } };
        settings = http.ToSettings(settings);
        using StringContent original = new StringContent("{\"id\":1,\"name\":\"Ada\"}", Encoding.Unicode, "application/json");
        TestingPerson? expected = await settings.ContentSerializer.FromHttpContentAsync<TestingPerson>(original); // expected?.Name == "Ada"
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage reply = await httpClient.PostAsync(new Uri("https://api.example.com/encoded"), original); // reply.StatusCode == OK

        bool corrupted = false;
        try
        {
            await http.LastRequestBodyAsync<TestingPerson>(); // throws: StubHttp always re-reads captured bytes as UTF-8
        }
        catch (JsonException)
        {
            corrupted = true;
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("Ada", expected?.Name);
        SampleCheck.Equal(HttpStatusCode.OK, reply.StatusCode);
        SampleCheck.Equal(true, corrupted);
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Problem: How do you match a request by HTTP method, and catch anything unmatched with a fallback route?</summary>
    /// <returns>A task that completes after every method-specific route and the fallback are sent.</returns>
    private static async Task ShowRoutesAsync()
    {
        using StubHttp http = new StubHttp
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
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage get = await httpClient.GetAsync(new Uri("https://api.example.com/get")); // get.StatusCode == OK
        using HttpResponseMessage missing = await httpClient.GetAsync(new Uri("https://api.example.com/missing")); // missing.StatusCode == NotFound, from Route.Fallback()

        await SendOtherMethodsAsync(httpClient);

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.OK, get.StatusCode);
        SampleCheck.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Verify(http);
        EnumerateRoutes(http);
    }

    /// <summary>Sends the remaining method-factory routes after the GET and fallback checks.</summary>
    /// <param name="httpClient">The client attached to the method-factory table.</param>
    /// <returns>A task that completes after each route returns a successful status.</returns>
    private static async Task SendOtherMethodsAsync(HttpClient httpClient)
    {
        foreach ((HttpMethod method, string path) in new (HttpMethod, string)[]
        {
            (HttpMethod.Post, "post"),
            (HttpMethod.Put, "put"),
            (HttpMethod.Delete, "delete"),
            (HttpMethod.Patch, "patch"),
            (HttpMethod.Head, "head"),
            (HttpMethod.Options, "options"),
            (HttpMethod.Trace, "any"),
        })
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, $"https://api.example.com/{path}");
            using HttpResponseMessage response = await httpClient.SendAsync(request);
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

        SampleCheck.Equal(9, genericCount);
        int count = 0;
        foreach (object route in (IEnumerable)http)
        {
            SampleCheck.Equal(true, route is RouteMatcher);
            count++;
        }

        SampleCheck.Equal(9, count);
    }

    /// <summary>Problem: How do you require exact headers, form fields and a custom predicate before a route matches?</summary>
    /// <returns>A task that completes after the matching response is checked.</returns>
    private static async Task ShowMatchersAsync()
    {
        RouteMatcher route = new RouteMatcher
        {
            Method = HttpMethod.Post,
            Template = "/people/{id}",
            Query = [("mode", "short")],
            ExactQuery = "extra=1&mode=short",
            ExactQueryParams = [("mode", "short"), ("extra", "1")],
            Headers = [("X-Test", "yes"), ("Content-Type", "application/x-www-form-urlencoded")],
            Body = "name=Ada+Lovelace&extra=1",
            FormData = [("name", "Ada Lovelace")],
            Where = static request => request.RequestUri!.Host == "api.example.com",
            WhereAsync = static async request => (await request.Content!.ReadAsStringAsync()).Contains("Ada", StringComparison.Ordinal),
            Reusable = true,
            Fallback = false,
        };
        using StubHttp http = new StubHttp { { route, Reply.Text("matched") } };
        using HttpClient httpClient = CreateClient(http);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/people/7?mode=short&extra=1")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("name", "Ada Lovelace"),
                new KeyValuePair<string, string>("extra", "1"),
            ]),
        };
        request.Headers.Add("X-Test", "yes");
        using HttpResponseMessage response = await httpClient.SendAsync(request); // response body == "matched"

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("matched", await response.Content.ReadAsStringAsync());
        Verify(http);
    }

    /// <summary>Problem: Why does an exact-query matcher for "a=1&amp;a=1" still match a request without the duplicate?</summary>
    /// <returns>A task that completes after both incorrect matches are checked.</returns>
    private static async Task ShowDuplicateQueryAsync()
    {
        using StubHttp http = new StubHttp
        {
            {
                new RouteMatcher { Template = "/query", ExactQueryParams = [("a", "1"), ("a", "1")] },
                Reply.Text("accepted")
            },
            {
                new RouteMatcher { Template = "/query", ExactQuery = "a=1&a=1" },
                Reply.Text("accepted")
            },
        };
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage pairs = await httpClient.GetAsync(new Uri("https://api.example.com/query?a=1&b=2")); // matches, even though "a" is not duplicated
        using HttpResponseMessage encoded = await httpClient.GetAsync(new Uri("https://api.example.com/query?a=1&b=2")); // matches the same way

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal("accepted", await pairs.Content.ReadAsStringAsync());
        SampleCheck.Equal("accepted", await encoded.Content.ReadAsStringAsync());
        Verify(http);
    }

    /// <summary>Problem: How do you reply with JSON, text, raw content, a bare status, or code that inspects the request?</summary>
    /// <returns>A task that completes after every reply style is sent.</returns>
    private static async Task ShowRepliesAsync()
    {
        using StubHttp http = new StubHttp
        {
            {
                Route.Get("/json"),
                Reply.Json("{\"id\":1,\"name\":\"Ada\"}")
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
                Reply.From(static request => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(request.RequestUri!.AbsolutePath) })
            },
            {
                Route.Post("/async"),
                Reply.From(static async request => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(await request.Content!.ReadAsStringAsync()) })
            },
        };
        using HttpClient httpClient = CreateClient(http);

        using HttpResponseMessage json = await httpClient.GetAsync(new Uri("https://api.example.com/json")); // json.StatusCode == OK
        using HttpResponseMessage rejected = await httpClient.GetAsync(new Uri("https://api.example.com/rejected")); // rejected.StatusCode == BadRequest
        using HttpResponseMessage text = await httpClient.GetAsync(new Uri("https://api.example.com/text")); // text body == "hello"
        using HttpResponseMessage html = await httpClient.GetAsync(new Uri("https://api.example.com/html")); // html content type == "text/html"
        using HttpResponseMessage content = await httpClient.GetAsync(new Uri("https://api.example.com/content")); // content.StatusCode == OK
        using HttpResponseMessage status = await httpClient.GetAsync(new Uri("https://api.example.com/status")); // status.StatusCode == NoContent
        using HttpResponseMessage sync = await httpClient.GetAsync(new Uri("https://api.example.com/sync")); // sync body == "/sync"
        using HttpResponseMessage echoed = await httpClient.PostAsync(new Uri("https://api.example.com/async"), new StringContent("Ada")); // echoed body == "Ada"

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.OK, json.StatusCode);
        SampleCheck.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        SampleCheck.Equal("hello", await text.Content.ReadAsStringAsync());
        SampleCheck.Equal("text/html", html.Content.Headers.ContentType?.MediaType);
        SampleCheck.Equal(HttpStatusCode.OK, content.StatusCode);
        SampleCheck.Equal(HttpStatusCode.NoContent, status.StatusCode);
        SampleCheck.Equal(HttpStatusCode.Accepted, sync.StatusCode);
        SampleCheck.Equal("/sync", await sync.Content.ReadAsStringAsync());
        SampleCheck.Equal("Ada", await echoed.Content.ReadAsStringAsync());
        Verify(http);
    }

    /// <summary>Problem: When a stub response sets Status, Json, Text, Content, Responder and ResponderAsync all at once, which one wins?</summary>
    /// <returns>A task that completes after the selected response status is checked.</returns>
    private static async Task ShowReplyPropertiesAsync()
    {
        using StringContent explicitContent = new StringContent("explicit");
        StubResponse properties = new StubResponse
        {
            Status = HttpStatusCode.Created,
            Json = "{}",
            Text = "text",
            ContentType = "text/plain",
            Content = explicitContent,
            Responder = static _ => new HttpResponseMessage(HttpStatusCode.Accepted),
            ResponderAsync = static _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)),
        };
        using StubHttp http = new StubHttp { { Route.Get("/precedence"), properties } };
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage precedence = await httpClient.GetAsync(new Uri("https://api.example.com/precedence")); // precedence.StatusCode == NoContent, ResponderAsync wins

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.Created, properties.Status);
        SampleCheck.Equal(true, properties.ResponderAsync is not null);
        SampleCheck.Equal(HttpStatusCode.NoContent, precedence.StatusCode);
    }

    /// <summary>Problem: How do you wait for a stub route to be called, and fail loudly when one never is?</summary>
    /// <returns>A task that completes after both a met and an unmet expectation are checked.</returns>
    private static async Task ShowVerificationAsync()
    {
        using StubHttp http = new StubHttp { { Route.Get("/expected"), Reply.Text("received") } };
        using HttpClient httpClient = CreateClient(http);
        Task waiting = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(1));
        bool notYetCalled = waiting.IsCompleted; // false: nothing has called /expected yet
        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://api.example.com/expected"));
        await waiting; // now completes, because the route above was called

        using StubHttp unmet = new StubHttp { { Route.Get("/unmet"), Reply.Status(HttpStatusCode.OK) } };
        bool rejected = false;
        try
        {
            await unmet.VerifyAllCalledAsync(TimeSpan.Zero); // throws: nothing ever called /unmet
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(false, notYetCalled);
        await http.VerifyAllCalledAsync();
        SampleCheck.Equal(null, await http.LastRequestBodyAsync<TestingPerson>());
        SampleCheck.Equal(true, rejected);
    }

    /// <summary>Problem: What happens if you check verification, then add another expected route afterward?</summary>
    /// <returns>A task that completes after the immediate failure and the final successful send.</returns>
    private static async Task ShowAdditionalRouteAsync()
    {
        using StubHttp http = new StubHttp { { Route.Get("/first"), Reply.Status(HttpStatusCode.OK) } };
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage first = await httpClient.GetAsync(new Uri("https://api.example.com/first"));
        await http.VerifyAllCalledAsync(); // passes: /first was called

        http.Add(Route.Get("/second"), Reply.Status(HttpStatusCode.OK));
        Task verification = http.VerifyAllCalledAsync(TimeSpan.FromSeconds(1));
        bool immediatelyDone = verification.IsCompleted; // true: a route added after the previous success already fails the wait
        bool rejected = false;
        try
        {
            await verification;
        }
        catch (InvalidOperationException error)
        {
            rejected = error.Message.Contains("/second", StringComparison.Ordinal);
        }

        using HttpResponseMessage second = await httpClient.GetAsync(new Uri("https://api.example.com/second"));

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(true, immediatelyDone);
        SampleCheck.Equal(true, rejected);
        Verify(http);
    }

    /// <summary>Problem: If two requests match a one-shot route at the same instant, which reply do they get?</summary>
    /// <returns>A task that completes after both requests receive the shared one-shot reply.</returns>
    private static async Task ShowOneShotRaceAsync()
    {
        int arrivals = 0;
        int previousCount = -1;
        using SemaphoreSlim bothMatching = new SemaphoreSlim(0, 2);
        RouteMatcher route = new RouteMatcher
        {
            Template = "/one-shot",
            WhereAsync = async _ =>
            {
                if (Interlocked.Increment(ref arrivals) == 2)
                {
                    previousCount = bothMatching.Release(2);
                }

                await bothMatching.WaitAsync();
                return true;
            },
        };
        using StubHttp http = new StubHttp { { route, Reply.Text("accepted") } };
        using HttpClient httpClient = CreateClient(http);
        Uri address = new Uri("https://api.example.com/one-shot");
        Task<HttpResponseMessage> first = httpClient.GetAsync(address);
        Task<HttpResponseMessage> second = httpClient.GetAsync(address);
        using HttpResponseMessage firstReply = await first; // both requests share the one route's single reply
        using HttpResponseMessage secondReply = await second;

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(0, previousCount);
        SampleCheck.Equal(2, arrivals);
        SampleCheck.Equal(2, http.Requests.Count);
        SampleCheck.Equal("accepted", await firstReply.Content.ReadAsStringAsync());
        SampleCheck.Equal("accepted", await secondReply.Content.ReadAsStringAsync());
        await http.VerifyAllCalledAsync();
    }

    /// <summary>Problem: What do NetworkBehavior's low-level members return once delay, failure and error percentages are configured?</summary>
    /// <returns>A task that completes after every direct member is checked.</returns>
    private static Task ShowNetworkBehaviorMembersAsync()
    {
        NetworkBehavior defaults = new NetworkBehavior();
        NetworkBehavior behavior = new NetworkBehavior(7)
        {
            Delay = TimeSpan.Zero,
            Variance = 0,
            FailurePercent = 0,
            ErrorPercent = 1,
            ErrorStatusCode = HttpStatusCode.ServiceUnavailable,
            FailureFactory = static () => new HttpRequestException("Test connection failure."),
        };
        TimeSpan delay = behavior.NextDelay(); // TimeSpan.Zero
        bool isFailure = behavior.NextIsFailure(); // false, FailurePercent is 0
        bool isError = behavior.NextIsError(); // true, ErrorPercent is 1
        Exception failure = behavior.CreateFailure(); // failure.Message == "Test connection failure."
        using HttpResponseMessage errorResponse = behavior.CreateErrorResponse(); // errorResponse.StatusCode == ServiceUnavailable

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(TimeSpan.FromSeconds(2), defaults.Delay);
        SampleCheck.Equal(TimeSpan.Zero, delay);
        SampleCheck.Equal(false, isFailure);
        SampleCheck.Equal(true, isError);
        SampleCheck.Equal("Test connection failure.", failure.Message);
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, errorResponse.StatusCode);
        return Task.CompletedTask;
    }

    /// <summary>Problem: How do you simulate network delay, random failures, and error status codes for one stub?</summary>
    /// <returns>A task that completes after the simulated error response and the simulated failure are checked.</returns>
    private static async Task ShowFaultsAsync()
    {
        NetworkBehavior behavior = new NetworkBehavior(7)
        {
            Delay = TimeSpan.Zero,
            Variance = 0,
            FailurePercent = 0,
            ErrorPercent = 1,
            ErrorStatusCode = HttpStatusCode.ServiceUnavailable,
            FailureFactory = static () => new HttpRequestException("Test connection failure."),
        };
        using StubHttp http = new StubHttp(behavior) { { Route.Get("/fault"), Reply.Text("normal") } };
        using HttpClient httpClient = CreateClient(http);
        using HttpResponseMessage error = await httpClient.GetAsync(new Uri("https://api.example.com/fault")); // error.StatusCode == ServiceUnavailable, from ErrorPercent = 1

        behavior.FailurePercent = 1;
        http.Add(Route.Get("/failure"), Reply.Text("normal"));
        bool failed = false;
        string? failureMessage = null;
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(new Uri("https://api.example.com/failure")); // throws, because FailurePercent is now 1
        }
        catch (HttpRequestException cause)
        {
            failed = true;
            failureMessage = cause.Message;
        }

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        SampleCheck.Equal(true, failed);
        SampleCheck.Equal("Test connection failure.", failureMessage);
        Verify(http);
        http.Behavior = null;
    }

    /// <summary>Problem: How do you build an IApiResponse&lt;T&gt; by hand for code that never calls through a stub HTTP client?</summary>
    /// <returns>A task that completes after the success, request-error and response-error stubs are checked.</returns>
    private static async Task ShowResponseStubsAsync()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/people/1");
        using HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent("{\"id\":1,\"name\":\"Ada\"}") };
        StubApiResponse<TestingPerson> stub = new StubApiResponse<TestingPerson>
        {
            Content = new TestingPerson(1, "Ada"),
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
        CheckResponse(stub); // reads stub.Content.Name through the ordinary IApiResponse<T> contract

        ApiRequestException sendError = new ApiRequestException("Connection failed.", request, request.Method, CreateSettings());
        using StubApiResponse<TestingPerson> failed = new StubApiResponse<TestingPerson> { Error = sendError };
        bool hasRequestError = failed.HasRequestError(out ApiRequestException? captured); // true, captured == sendError

        using HttpResponseMessage rejected = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };
        ApiException replyError = await ApiException.Create(request, request.Method, rejected, CreateSettings());
        using StubApiResponse<TestingPerson> refused = new StubApiResponse<TestingPerson>
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
        bool hasResponseError = refused.HasResponseError(out ApiException? capturedReply); // true, capturedReply == replyError

        using ApiResponse<TestingPerson> real = new ApiResponse<TestingPerson>(message, new TestingPerson(1, "Ada"), CreateSettings());
        CheckResponse(real); // the same code works for a stub and a real ApiResponse

        // Checks for this sample (not part of the documentation excerpt):
        SampleCheck.Equal(false, stub.HasRequestError(out _));
        SampleCheck.Equal(false, stub.HasResponseError(out _));
        stub.Dispose();
        SampleCheck.Equal(true, hasRequestError);
        SampleCheck.Equal(sendError, captured);
        SampleCheck.Equal(true, hasResponseError);
        SampleCheck.Equal(replyError, capturedReply);
    }

    /// <summary>Checks typed content through the interface's non-null success contract.</summary>
    /// <param name="response">A real response or manually configured response stub.</param>
    private static void CheckResponse(IApiResponse<TestingPerson> response)
    {
        if (response.IsSuccessfulWithContent)
        {
            SampleCheck.Equal("Ada", response.Content.Name);
        }
    }
}
