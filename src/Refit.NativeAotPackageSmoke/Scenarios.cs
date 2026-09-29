// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Refit.Testing;

namespace Refit.NativeAotPackageSmoke;

/// <summary>The scenarios the published binary runs. Each one throws when Refit does not behave as documented.</summary>
internal static class Scenarios
{
    private const string Host = "https://people.example";

    private const string AdaJson = """{"id":1,"name":"Ada"}""";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly Person[] People = [new(1, "Ada"), new(2, "Grace"), new(3, "Linus")];

    /// <summary>Resolves a client registered with <c>AddRefitGeneratedClient</c> and a JSON context.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task DependencyInjectionAsync()
    {
        using var http = new StubHttp { { Route.Get("/people/1"), Reply.Json(AdaJson) } };
        var services = new ServiceCollection();
        services.AddRefitGeneratedClient<IPeopleApi>(SmokeJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new(Host))
            .ConfigurePrimaryHttpMessageHandler(() => http);

        await using var provider = services.BuildServiceProvider();
        var person = await provider.GetRequiredService<IPeopleApi>().GetAsync(1, CancellationToken.None).ConfigureAwait(false);

        Check.Equal(People[0], person, "resolved client reply");
        await http.VerifyAllCalledAsync().ConfigureAwait(false);
    }

    /// <summary>Resolves two keyed clients, each with its own base address.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task KeyedDependencyInjectionAsync()
    {
        using var http = new StubHttp
        {
            { Route.Get("/people/1"), Reply.From(static request => Json(request.RequestUri!.Host == "primary.example" ? AdaJson : """{"id":1,"name":"Wrong host"}""")) },
            { Route.Get("/people/1"), Reply.From(static request => Json(request.RequestUri!.Host == "backup.example" ? """{"id":1,"name":"Backup Ada"}""" : """{"id":1,"name":"Wrong host"}""")) },
        };
        var services = new ServiceCollection();
        services.AddKeyedRefitGeneratedClient<IPeopleApi>("primary", SmokeJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new("https://primary.example"))
            .ConfigurePrimaryHttpMessageHandler(() => http);
        services.AddKeyedRefitGeneratedClient<IPeopleApi>("backup", SmokeJsonContext.Default)
            .ConfigureHttpClient(static client => client.BaseAddress = new("https://backup.example"))
            .ConfigurePrimaryHttpMessageHandler(() => http);

        await using var provider = services.BuildServiceProvider();
        var primary = await provider.GetRequiredKeyedService<IPeopleApi>("primary").GetAsync(1, CancellationToken.None).ConfigureAwait(false);
        var backup = await provider.GetRequiredKeyedService<IPeopleApi>("backup").GetAsync(1, CancellationToken.None).ConfigureAwait(false);

        Check.Equal("Ada", primary.Name, "primary keyed client");
        Check.Equal("Backup Ada", backup.Name, "backup keyed client");
    }

    /// <summary>Writes the query string through an <see cref="IQueryConverter{T}"/>.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task QueryConverterAsync()
    {
        string? query = null;
        using var http = new StubHttp
        {
            {
                Route.Get("/people"),
                Reply.From(request =>
                {
                    query = request.RequestUri!.Query;
                    return Json($"[{AdaJson}]");
                })
            },
        };
        var api = CreateClient(http);

        var found = await api.SearchAsync(new("ada lovelace", 5), CancellationToken.None).ConfigureAwait(false);

        Check.Equal("?q=ada%20lovelace&limit=5", query, "converted query string");
        Check.Equal(1, found.Count, "search result count");
    }

    /// <summary>Uploads a stream part and a JSON part serialized with context metadata.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task MultipartAsync()
    {
        var parts = new Dictionary<string, (string? MediaType, string Body)>();
        using var http = new StubHttp
        {
            {
                Route.Post("/people/1/photo"),
                Reply.From(async request =>
                {
                    foreach (var part in (MultipartFormDataContent)request.Content!)
                    {
                        var partName = part.Headers.ContentDisposition!.Name!.Trim('"');
                        parts[partName] = (part.Headers.ContentType?.MediaType, await part.ReadAsStringAsync().ConfigureAwait(false));
                    }

                    return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("stored") };
                })
            },
        };

        // Full capture replaces the request content with a buffered copy; the responder needs the multipart parts.
        http.RequestCapture = RequestCapture.None;
        var api = CreateClient(http);

        using var photo = new MemoryStream(Encoding.UTF8.GetBytes("fake-png"));
        var receipt = await api.UploadPhotoAsync(1, new(photo, "ada.png", "image/png"), new("At the engine", ["math", "history"])).ConfigureAwait(false);

        Check.Equal("stored", receipt, "multipart receipt");
        Check.Equal("fake-png", parts["photo"].Body, "stream part body");
        Check.Equal("image/png", parts["photo"].MediaType, "stream part media type");
        Check.Equal("application/json", parts["metadata"].MediaType, "JSON part media type");
        Check.Equal("""{"caption":"At the engine","tags":["math","history"]}""", parts["metadata"].Body, "JSON part body");
    }

    /// <summary>Reads a registered error body from <see cref="ApiException"/> and keeps the status on <see cref="ApiResponse{T}"/>.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task ErrorBodyAsync()
    {
        const string failureJson = """{"code":"missing","message":"No person 9"}""";
        using var http = new StubHttp
        {
            { Route.Get("/people/9"), Reply.Json(failureJson, HttpStatusCode.NotFound) },
            { Route.Get("/people/9"), Reply.Json(failureJson, HttpStatusCode.NotFound) },
        };
        var api = CreateClient(http);

        Failure? failure = null;
        try
        {
            _ = await api.GetAsync(9, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ApiException error)
        {
            Check.Equal(HttpStatusCode.NotFound, error.StatusCode, "exception status");
            failure = await error.GetContentAsAsync<Failure>().ConfigureAwait(false);
        }

        Check.Equal(new Failure("missing", "No person 9"), failure, "error body");

        using var response = await api.GetResponseAsync(9, CancellationToken.None).ConfigureAwait(false);
        Check.Equal(false, response.IsSuccessStatusCode, "response success flag");
        Check.Equal(HttpStatusCode.NotFound, response.StatusCode, "response status");
        var responseError = response.Error as ApiException ?? throw new InvalidOperationException("The response did not keep its ApiException.");
        Check.Equal("missing", (await responseError.GetContentAsAsync<Failure>().ConfigureAwait(false))?.Code, "response error body");
    }

    /// <summary>Defers the request until the adapted call is invoked.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task ReturnTypeAdapterAsync()
    {
        using var http = new StubHttp { { Route.Get("/people/1"), Reply.Json(AdaJson) } };
        var api = CreateClient(http);

        var call = api.GetLater(1);
        Check.Equal(0, http.Requests.Count, "requests before invoke");

        var person = await call.InvokeAsync(CancellationToken.None).ConfigureAwait(false);
        Check.Equal(People[0], person, "adapted reply");
        Check.Equal(1, http.Requests.Count, "requests after invoke");
    }

    /// <summary>Reads one streaming method as a JSON array, JSON Lines and server-sent events.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task StreamedRepliesAsync()
    {
        using var http = new StubHttp
        {
            { Route.Get("/people/live"), Reply.Json("""[{"id":1,"name":"Ada"},{"id":2,"name":"Grace"},{"id":3,"name":"Linus"}]""") },
            { Route.Get("/people/live"), Reply.JsonLines(People) },
            { Route.Get("/people/live"), Reply.ServerSentEvents(People) },
        };
        var api = CreateClient(http);

        foreach (var format in (string[])["JSON array", "JSON Lines", "server-sent events"])
        {
            var names = new List<string>();
            await foreach (var person in api.WatchAsync(CancellationToken.None).ConfigureAwait(false))
            {
                names.Add(person.Name);
            }

            Check.Equal("Ada,Grace,Linus", string.Join(',', names), format);
        }
    }

    /// <summary>Leaving a streamed reply early disposes the response.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task EarlyDisposalAsync()
    {
        var source = new StreamSource(StreamingContentFormat.JsonLines);
        using var http = new StubHttp { { Route.Get("/people/live"), Reply.Stream(source) } };
        var api = CreateClient(http);

        source.Release(People[0]);
        source.Release(People[1]);
        await foreach (var person in api.WatchAsync(CancellationToken.None).ConfigureAwait(false))
        {
            Check.Equal("Ada", person.Name, "first streamed person");
            break;
        }

        await source.Closed.WaitAsync(Timeout).ConfigureAwait(false);
    }

    /// <summary>Cancelling a stalled streamed read ends it and closes the response.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task CancellationAsync()
    {
        var source = new StreamSource(StreamingContentFormat.JsonLines);
        using var http = new StubHttp { { Route.Get("/people/live"), Reply.Stream(source) } };
        var api = CreateClient(http);
        using var cancellation = new CancellationTokenSource();

        var people = api.WatchAsync(cancellation.Token).GetAsyncEnumerator(cancellation.Token);
        await using (people.ConfigureAwait(false))
        {
            source.Release(People[0]);
            Check.Equal(true, await people.MoveNextAsync().ConfigureAwait(false), "first read");

            var stalled = people.MoveNextAsync().AsTask();
            Check.Equal(false, stalled.IsCompleted, "stalled read completed early");

            await cancellation.CancelAsync().ConfigureAwait(false);
            try
            {
                _ = await stalled.WaitAsync(Timeout).ConfigureAwait(false);
                throw new InvalidOperationException("The stalled read was not cancelled.");
            }
            catch (OperationCanceledException)
            {
                // Expected: cancelling the token ends the stalled read.
            }
        }

        await source.Closed.WaitAsync(Timeout).ConfigureAwait(false);
    }

    /// <summary>Streams an <see cref="IAsyncEnumerable{T}"/> body as JSON Lines to a handler that reads it line by line.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task JsonLinesUploadAsync()
    {
        var lines = new List<string>();
        using var http = new StubHttp
        {
            {
                Route.Post("/people/import"),
                Reply.From(async (request, cancellationToken) =>
                {
                    var body = await request.Content!.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    await using (body.ConfigureAwait(false))
                    {
                        using var reader = new StreamReader(body);
                        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                        {
                            lines.Add(line);
                        }
                    }

                    return new HttpResponseMessage(HttpStatusCode.Accepted);
                })
            },
        };
        http.RequestCapture = RequestCapture.None;
        var api = CreateClient(http);

        await api.ImportAsync(ProduceAsync(), CancellationToken.None).ConfigureAwait(false);

        Check.Equal(3, lines.Count, "uploaded line count");
        Check.Equal("""{"id":3,"name":"Linus"}""", lines[2], "last uploaded line");
    }

    /// <summary>Follows the page cursor until the last page.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task PagingAsync()
    {
        using var http = new StubHttp
        {
            {
                new RouteMatcher { Method = HttpMethod.Get, Template = "/people/pages", Reusable = true },
                Reply.From(static request => request.RequestUri!.Query.Contains("cursor=page-2", StringComparison.Ordinal)
                    ? Json("""{"items":[{"id":3,"name":"Linus"}],"next":null}""")
                    : Json("""{"items":[{"id":1,"name":"Ada"},{"id":2,"name":"Grace"}],"next":"page-2"}"""))
            },
        };
        var api = CreateClient(http);

        var names = new List<string>();
        await foreach (var person in api.ListAll().ConfigureAwait(false))
        {
            names.Add(person.Name);
        }

        Check.Equal("Ada,Grace,Linus", string.Join(',', names), "paged people");
        Check.Equal(2, http.Requests.Count, "page requests");
    }

    /// <summary>A reply type the context does not describe fails instead of falling back to reflection.</summary>
    /// <returns>A task that completes when the check passes.</returns>
    internal static async Task MissingMetadataAsync()
    {
        using var http = new StubHttp { { Route.Get("/undescribed"), Reply.Json("""{"value":"x"}""") } };
        var api = CreateClient(http);

        try
        {
            _ = await api.GetUndescribedAsync(CancellationToken.None).ConfigureAwait(false);
            throw new InvalidOperationException("The client read a type its JSON context does not describe.");
        }
        catch (Exception ex) when (ex.GetBaseException() is NotSupportedException)
        {
            // Expected: the JSON context is the only metadata source.
        }
    }

    private static IPeopleApi CreateClient(StubHttp http) =>
        http.CreateGeneratedClient<IPeopleApi>(Host, RefitSettings.ForJsonContext(SmokeJsonContext.Default));

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static async IAsyncEnumerable<Person> ProduceAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var person in People)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return person;
        }
    }
}
