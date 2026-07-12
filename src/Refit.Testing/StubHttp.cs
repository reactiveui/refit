// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Testing;

/// <summary>
/// A declarative test <see cref="HttpMessageHandler"/> for Refit clients, written as a route table: each
/// entry pairs a <see cref="RouteMatcher"/> (built with <see cref="Route"/>) with a <see cref="StubResponse"/>
/// (built with <see cref="Reply"/>). Populate it with a collection initializer whose templates mirror the
/// routes declared on the Refit interface.
/// </summary>
/// <remarks>
/// <para>
/// <code>
/// var http = new StubHttp
/// {
///     { Route.Get("/users/{id}"), Reply.With(new User("octocat")) },
///     { Route.Post("/users"),     Reply.Status(HttpStatusCode.Created) },
/// };
/// var api = http.CreateClient&lt;IGitHubApi&gt;(baseUrl);
/// </code>
/// </para>
/// <para>
/// Non-reusable routes (the common case) are one-shot: each satisfies exactly one request and is then
/// consumed, and <see cref="VerifyAllCalled"/> asserts every one was hit. Set <see cref="RouteMatcher.Reusable"/>
/// for a background stub that may match any number of requests. An unmatched request throws rather than
/// returning a canned 404, and every request is recorded in <see cref="Requests"/> for inspection —
/// including typed inspection of the sent body via <see cref="LastRequestBodyAsync{T}"/>.
/// </para>
/// </remarks>
public sealed class StubHttp : HttpMessageHandler, IEnumerable<RouteMatcher>
{
    /// <summary>The default timeout used by the parameterless <see cref="VerifyAllCalledAsync()"/>.</summary>
    private static readonly TimeSpan DefaultVerifyTimeout = TimeSpan.FromSeconds(1);

    /// <summary>The route matchers, matched in declared order.</summary>
    private readonly List<RouteMatcher> _routes = [];

    /// <summary>The responses, parallel to <see cref="_routes"/>.</summary>
    private readonly List<StubResponse> _responses = [];

    /// <summary>Tracks, by index, which non-reusable route has already satisfied a request.</summary>
    private readonly List<bool> _consumed = [];

    /// <summary>The buffered request bodies, parallel to <see cref="_requests"/>, captured before disposal.</summary>
    private readonly List<CapturedBody?> _bodies = [];

    /// <summary>Guards mutation of the rule lists, <see cref="_requests"/> and <see cref="_outstanding"/>.</summary>
    private readonly Lock _gate = new();

    /// <summary>Completes when every non-reusable route has been consumed; awaited by <see cref="VerifyAllCalledAsync(TimeSpan)"/>.</summary>
    private readonly TaskCompletionSource<bool> _allConsumed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>The requests received so far, in order.</summary>
    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>A cached read-only view over <see cref="_requests"/>, returned by <see cref="Requests"/> so reads never copy.</summary>
    private readonly ReadOnlyCollection<HttpRequestMessage> _requestsView;

    /// <summary>The serializer used for typed replies and typed request capture; replaced by <see cref="ToSettings(RefitSettings)"/>.</summary>
    private IHttpContentSerializer _serializer = new SystemTextJsonContentSerializer();

    /// <summary>The number of non-reusable routes not yet consumed.</summary>
    private int _outstanding;

    /// <summary>Initializes a new instance of the <see cref="StubHttp"/> class with an empty route table.</summary>
    public StubHttp() => _requestsView = new(_requests);

    /// <summary>Initializes a new instance of the <see cref="StubHttp"/> class with network-fault simulation.</summary>
    /// <param name="behavior">The network behavior applied to every matched request.</param>
    public StubHttp(NetworkBehavior behavior)
        : this() => Behavior = behavior;

    /// <summary>Gets a read-only view of the requests this handler has received, in order.</summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requestsView;

    /// <summary>Gets or sets the network behavior simulated for each matched request; <c>null</c> disables simulation.</summary>
    public NetworkBehavior? Behavior { get; set; }

    /// <summary>Adds a route and its reply to the table. Called by the collection initializer.</summary>
    /// <param name="route">The route matcher.</param>
    /// <param name="response">The reply to return when the route matches.</param>
    public void Add(RouteMatcher route, StubResponse response)
    {
        ArgumentExceptionHelper.ThrowIfNull(route);
        ArgumentExceptionHelper.ThrowIfNull(response);

        lock (_gate)
        {
            _routes.Add(route);
            _responses.Add(response);
            _consumed.Add(false);
            if (!route.Reusable && !route.Fallback)
            {
                _outstanding++;
            }
        }
    }

    /// <summary>Wraps this handler in a fresh <see cref="RefitSettings"/> that routes requests through it.</summary>
    /// <returns>New settings whose handler factory returns this handler.</returns>
    public RefitSettings ToSettings() => ToSettings(new RefitSettings());

    /// <summary>Points an existing <see cref="RefitSettings"/> at this handler and adopts its content serializer.</summary>
    /// <param name="baseSettings">
    /// The settings to attach the handler to; its <see cref="RefitSettings.HttpMessageHandlerFactory"/> is
    /// overwritten. Its serializer is adopted for typed replies and typed request capture.
    /// </param>
    /// <returns>The same settings, with their handler factory pointed at this handler.</returns>
    public RefitSettings ToSettings(RefitSettings baseSettings)
    {
        ArgumentExceptionHelper.ThrowIfNull(baseSettings);
        _serializer = baseSettings.ContentSerializer;
        baseSettings.HttpMessageHandlerFactory = () => this;
        return baseSettings;
    }

    /// <summary>
    /// Creates a Refit implementation of <typeparamref name="T"/> whose HTTP requests are routed through this
    /// handler. A one-line replacement for <c>RestService.For&lt;T&gt;(hostUrl, handler.ToSettings())</c>.
    /// </summary>
    /// <typeparam name="T">The Refit interface to implement.</typeparam>
    /// <param name="hostUrl">The base address the client sends requests to.</param>
    /// <returns>A Refit client that sends every request to this handler.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The interface type is intentionally specified explicitly by the caller, matching RestService.For<T>.")]
    [RequiresUnreferencedCode("Creating a Refit client through the reflection path requires runtime type lookup and request metadata.")]
    public T CreateClient<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
    T>(string hostUrl) => CreateClient<T>(hostUrl, new RefitSettings());

    /// <summary>
    /// Creates a Refit implementation of <typeparamref name="T"/> whose HTTP requests are routed through this
    /// handler, starting from the supplied settings. Use this overload to keep a custom serializer or
    /// URL-resolution configuration; the settings' handler factory is pointed at this handler.
    /// </summary>
    /// <typeparam name="T">The Refit interface to implement.</typeparam>
    /// <param name="hostUrl">The base address the client sends requests to.</param>
    /// <param name="baseSettings">The settings to route through this handler.</param>
    /// <returns>A Refit client that sends every request to this handler.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The interface type is intentionally specified explicitly by the caller, matching RestService.For<T>.")]
    [RequiresUnreferencedCode("Creating a Refit client through the reflection path requires runtime type lookup and request metadata.")]
    public T CreateClient<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.Interfaces |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
    T>(string hostUrl, RefitSettings baseSettings) => RestService.For<T>(hostUrl, ToSettings(baseSettings));

    /// <summary>
    /// Creates a source-generated Refit implementation of <typeparamref name="T"/> whose HTTP requests are
    /// routed through this handler, without falling back to reflection. Use it in trim- or AOT-compiled test
    /// hosts where the reflection-based <see cref="CreateClient{T}(string)"/> is unavailable.
    /// </summary>
    /// <typeparam name="T">The Refit interface to implement; a generated implementation must be registered for it.</typeparam>
    /// <param name="hostUrl">The base address the client sends requests to.</param>
    /// <returns>A source-generated Refit client that sends every request to this handler.</returns>
    /// <exception cref="InvalidOperationException">No generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The interface type is intentionally specified explicitly by the caller, matching RestService.ForGenerated<T>.")]
    public T CreateGeneratedClient<T>(string hostUrl) => CreateGeneratedClient<T>(hostUrl, new RefitSettings());

    /// <summary>
    /// Creates a source-generated Refit implementation of <typeparamref name="T"/> whose HTTP requests are
    /// routed through this handler, starting from the supplied settings and without falling back to reflection.
    /// </summary>
    /// <typeparam name="T">The Refit interface to implement; a generated implementation must be registered for it.</typeparam>
    /// <param name="hostUrl">The base address the client sends requests to.</param>
    /// <param name="baseSettings">The settings to route through this handler.</param>
    /// <returns>A source-generated Refit client that sends every request to this handler.</returns>
    /// <exception cref="InvalidOperationException">No generated implementation is registered for <typeparamref name="T"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The interface type is intentionally specified explicitly by the caller, matching RestService.ForGenerated<T>.")]
    public T CreateGeneratedClient<T>(string hostUrl, RefitSettings baseSettings) => RestService.ForGenerated<T>(hostUrl, ToSettings(baseSettings));

    /// <summary>Deserializes the body of the most recent request using the client's content serializer.</summary>
    /// <typeparam name="T">The type to deserialize the request body into.</typeparam>
    /// <returns>The deserialized request body, or <see langword="default"/> when the body is empty.</returns>
    /// <exception cref="InvalidOperationException">No request has been received yet.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The body type is intentionally specified explicitly by the caller, like a deserialization target.")]
    public Task<T?> LastRequestBodyAsync<T>()
    {
        CapturedBody? body;
        lock (_gate)
        {
            if (_bodies.Count == 0)
            {
                throw new InvalidOperationException("No request has been received yet.");
            }

            body = _bodies[^1];
        }

        return DeserializeBodyAsync<T>(body);
    }

    /// <summary>Deserializes the body of the request at <paramref name="index"/> using the client's content serializer.</summary>
    /// <typeparam name="T">The type to deserialize the request body into.</typeparam>
    /// <param name="index">The zero-based index into <see cref="Requests"/>.</param>
    /// <returns>The deserialized request body, or <see langword="default"/> when the body is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">No request exists at <paramref name="index"/>.</exception>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The body type is intentionally specified explicitly by the caller, like a deserialization target.")]
    public Task<T?> RequestBodyAsync<T>(int index)
    {
        CapturedBody? body;
        lock (_gate)
        {
            if (index < 0 || index >= _bodies.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            body = _bodies[index];
        }

        return DeserializeBodyAsync<T>(body);
    }

    /// <summary>Asserts every non-reusable route was matched by a request.</summary>
    /// <exception cref="InvalidOperationException">One or more expected routes were never hit.</exception>
    public void VerifyAllCalled() => ThrowIfOutstanding();

    /// <summary>
    /// Asynchronously waits (up to a default 1 second) for every non-reusable route to be hit, then asserts.
    /// Use this when the requests under test are fired-and-forget (e.g. an observable subscription or a background
    /// send) and may not have completed at the point of verification.
    /// </summary>
    /// <returns>A task that completes when all routes are hit, or faults with the outstanding list on timeout.</returns>
    public Task VerifyAllCalledAsync() => VerifyAllCalledAsync(DefaultVerifyTimeout);

    /// <summary>Asynchronously waits up to <paramref name="timeout"/> for every non-reusable route to be hit, then asserts.</summary>
    /// <param name="timeout">How long to wait for the outstanding requests to arrive before asserting.</param>
    /// <returns>A task that completes when all routes are hit, or faults with the outstanding list on timeout.</returns>
    /// <exception cref="InvalidOperationException">One or more expected routes were not hit within the timeout.</exception>
    [SuppressMessage(
        "Usage",
        "VSTHRD003:Avoid awaiting foreign Tasks",
        Justification = "The awaited task is this handler's own completion signal, set by its SendAsync; there is no foreign context or deadlock risk.")]
    public async Task VerifyAllCalledAsync(TimeSpan timeout)
    {
        lock (_gate)
        {
            if (_outstanding == 0)
            {
                _ = _allConsumed.TrySetResult(true);
            }
        }

        if (!_allConsumed.Task.IsCompleted)
        {
            using var cts = new CancellationTokenSource();
            var delay = Task.Delay(timeout, cts.Token);
            var winner = await Task.WhenAny(_allConsumed.Task, delay).ConfigureAwait(false);
            if (winner == _allConsumed.Task)
            {
                await CancelAsync(cts).ConfigureAwait(false);
            }
        }

        ThrowIfOutstanding();
    }

    /// <inheritdoc/>
    IEnumerator<RouteMatcher> IEnumerable<RouteMatcher>.GetEnumerator()
    {
        lock (_gate)
        {
            return ((IEnumerable<RouteMatcher>)_routes.ToArray()).GetEnumerator();
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<RouteMatcher>)this).GetEnumerator();

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        int requestIndex;
        lock (_gate)
        {
            _requests.Add(request);
            _bodies.Add(null);
            requestIndex = _requests.Count - 1;
        }

        // Buffer the body up front (while its content is alive) so it can be matched, re-read, and captured
        // for typed inspection even after the client disposes the request.
        await BufferRequestAsync(request, requestIndex).ConfigureAwait(false);

        // Priority tiers, tried in order regardless of declaration order: one-shot expectations, then reusable
        // background stubs, then catch-all fallbacks.
        var index = await FindMatchAsync(request, RouteTier.OneShot, cancellationToken).ConfigureAwait(false);
        if (index < 0)
        {
            index = await FindMatchAsync(request, RouteTier.Reusable, cancellationToken).ConfigureAwait(false);
        }

        if (index < 0)
        {
            index = await FindMatchAsync(request, RouteTier.Fallback, cancellationToken).ConfigureAwait(false);
        }

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"No stubbed route matched the request: {request.Method} {request.RequestUri}");
        }

        if (!_routes[index].Reusable && !_routes[index].Fallback)
        {
            Consume(index);
        }

        var faulted = await ApplyBehaviorAsync(request, cancellationToken).ConfigureAwait(false);
        if (faulted is not null)
        {
            return faulted;
        }

        var response = await BuildResponseAsync(_responses[index], request).ConfigureAwait(false);

        // Honor cancellation requested during matching or by a responder (e.g. a test that cancels mid-send).
        cancellationToken.ThrowIfCancellationRequested();
        return response;
    }

    /// <summary>Determines whether the request satisfies every matcher on the route.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="request">The incoming request.</param>
    /// <param name="cancellationToken">A token to cancel body reads.</param>
    /// <returns><see langword="true"/> when the request matches.</returns>
    private static async Task<bool> MatchesAsync(RouteMatcher route, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return (route.Method is null || request.Method == route.Method)
            && MatchesTemplate(route.Template, request.RequestUri)
            && MatchesQuery(route, request.RequestUri)
            && (route.Headers is null || MatchesHeaders(request, route.Headers))
            && await MatchesBodyAsync(route, request, cancellationToken).ConfigureAwait(false)
            && await MatchesPredicatesAsync(route, request).ConfigureAwait(false);
    }

    /// <summary>Applies the synchronous and asynchronous request predicates, if any.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="request">The incoming request.</param>
    /// <returns><see langword="true"/> when both predicates pass (or are absent).</returns>
    private static async Task<bool> MatchesPredicatesAsync(RouteMatcher route, HttpRequestMessage request)
        => (route.Where is null || route.Where(request))
            && (route.WhereAsync is null || await route.WhereAsync(request).ConfigureAwait(false));

    /// <summary>Matches the request path against a template, treating <c>{name}</c> segments as wildcards.</summary>
    /// <param name="template">The expected template (<c>"*"</c> matches any; relative or absolute).</param>
    /// <param name="actual">The request URI.</param>
    /// <returns><see langword="true"/> when the path matches the template.</returns>
    private static bool MatchesTemplate(string template, Uri? actual)
    {
        if (template == "*")
        {
            return true;
        }

        if (actual is null)
        {
            return false;
        }

        var expectedPath = template.Split('?')[0];

        // A relative template (e.g. "/users") matches the request's absolute path; an absolute template
        // matches the full scheme/host/path.
#if NETFRAMEWORK
        var isRelative = expectedPath.StartsWith("/", StringComparison.Ordinal);
#else
        var isRelative = expectedPath.StartsWith('/');
#endif
        var actualPath = isRelative ? actual.AbsolutePath : actual.GetLeftPart(UriPartial.Path);
        return SegmentsMatch(expectedPath, actualPath);
    }

    /// <summary>Compares two paths segment by segment, where an expected <c>{name}</c> segment matches any one segment.</summary>
    /// <param name="expected">The expected template path.</param>
    /// <param name="actual">The actual request path.</param>
    /// <returns><see langword="true"/> when the segments match.</returns>
    private static bool SegmentsMatch(string expected, string actual)
    {
        var expectedSegments = expected.Split('/');
        var actualSegments = actual.Split('/');
        if (expectedSegments.Length != actualSegments.Length)
        {
            return false;
        }

        for (var i = 0; i < expectedSegments.Length; i++)
        {
            var expectedSegment = expectedSegments[i];
            if (IsPlaceholder(expectedSegment))
            {
                if (actualSegments[i].Length == 0)
                {
                    return false;
                }

                continue;
            }

            if (!string.Equals(expectedSegment, actualSegments[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a template segment is a <c>{name}</c> placeholder.</summary>
    /// <param name="segment">The template segment.</param>
    /// <returns><see langword="true"/> when the segment is a placeholder.</returns>
    private static bool IsPlaceholder(string segment) =>
        segment.Length >= 2 && segment[0] == '{' && segment[^1] == '}';

    /// <summary>Classifies a route into its priority tier.</summary>
    /// <param name="route">The route to classify.</param>
    /// <returns>The route's priority tier.</returns>
    private static RouteTier TierOf(RouteMatcher route) =>
        route switch
        {
            { Fallback: true } => RouteTier.Fallback,
            { Reusable: true } => RouteTier.Reusable,
            _ => RouteTier.OneShot
        };

    /// <summary>Applies the partial and exact query matchers, if any, to the request URI.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="actual">The request URI.</param>
    /// <returns><see langword="true"/> when the query matches (or no query matcher is set).</returns>
    private static bool MatchesQuery(RouteMatcher route, Uri? actual)
    {
        var raw = actual?.Query.TrimStart('?') ?? string.Empty;
        var pairs = ParsePairs(raw);
        return (route.ExactQuery is null || ExactMatch(pairs, ParsePairs(route.ExactQuery)))
            && (route.ExactQueryParams is null || ExactMatch(pairs, route.ExactQueryParams))
            && (route.Query is null || ContainsAll(pairs, route.Query));
    }

    /// <summary>Reads and compares the request body for the exact-body and form-data matchers.</summary>
    /// <param name="route">The candidate route.</param>
    /// <param name="request">The incoming request.</param>
    /// <param name="cancellationToken">A token to cancel the body read.</param>
    /// <returns><see langword="true"/> when the body matches (or no body matcher is set).</returns>
    private static async Task<bool> MatchesBodyAsync(RouteMatcher route, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (route.Body is null && route.FormData is null)
        {
            return true;
        }

        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return (route.Body is null || string.Equals(body, route.Body, StringComparison.Ordinal))
            && (route.FormData is null || ContainsAll(ParsePairs(body), route.FormData));
    }

    /// <summary>Confirms the request carries each expected header, checking request and content headers.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="headers">The expected header name/value pairs.</param>
    /// <returns><see langword="true"/> when every expected header is present with the expected value.</returns>
    private static bool MatchesHeaders(HttpRequestMessage request, (string Name, string Value)[] headers)
    {
        foreach (var (name, value) in headers)
        {
            if (!HeaderMatches(request, name, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a single named header is present with the expected value.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The expected header value.</param>
    /// <returns><see langword="true"/> when the header matches.</returns>
    private static bool HeaderMatches(HttpRequestMessage request, string name, string value)
    {
        var present = TryGetHeader(request.Headers, name, out var actual) ||
            (request.Content is not null && TryGetHeader(request.Content.Headers, name, out actual));
        return present && string.Equals(actual, value, StringComparison.Ordinal);
    }

    /// <summary>Gets the combined value of a named header, if present.</summary>
    /// <param name="headers">The header collection to search.</param>
    /// <param name="name">The header name.</param>
    /// <param name="value">The comma-joined header value when found.</param>
    /// <returns><see langword="true"/> when the header exists.</returns>
    private static bool TryGetHeader(HttpHeaders headers, string name, out string? value)
    {
        if (headers.TryGetValues(name, out var values))
        {
            value = string.Join(", ", values);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Parses a URL-encoded <c>key=value&amp;...</c> string into decoded pairs.</summary>
    /// <param name="encoded">The encoded query or form body.</param>
    /// <returns>The decoded key/value pairs, in order.</returns>
    private static List<(string Key, string Value)> ParsePairs(string encoded)
    {
        var result = new List<(string, string)>();
        if (encoded.Length == 0)
        {
            return result;
        }

        foreach (var pair in encoded.Split('&'))
        {
            if (pair.Length == 0)
            {
                continue;
            }

            var eq = pair.IndexOf('=');
            var key = eq < 0 ? pair : pair[..eq];
            var value = eq < 0 ? string.Empty : pair[(eq + 1)..];
            result.Add((Decode(key), Decode(value)));
        }

        return result;
    }

    /// <summary>URL-decodes a single query or form token.</summary>
    /// <param name="value">The encoded token.</param>
    /// <returns>The decoded token.</returns>
    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    /// <summary>Determines whether every expected pair is present in the actual pairs.</summary>
    /// <param name="actual">The pairs parsed from the request.</param>
    /// <param name="expected">The pairs that must all be present.</param>
    /// <returns><see langword="true"/> when all expected pairs are found.</returns>
    private static bool ContainsAll(List<(string Key, string Value)> actual, (string Key, string Value)[] expected)
    {
        foreach (var (key, value) in expected)
        {
            if (!Contains(actual, key, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether the actual pairs are exactly the expected pairs (same set, no extras).</summary>
    /// <param name="actual">The pairs parsed from the request.</param>
    /// <param name="expected">The complete expected pairs.</param>
    /// <returns><see langword="true"/> when the pair sets are equal, ignoring order.</returns>
    private static bool ExactMatch(List<(string Key, string Value)> actual, IReadOnlyList<(string Key, string Value)> expected)
    {
        if (actual.Count != expected.Count)
        {
            return false;
        }

        foreach (var (key, value) in expected)
        {
            if (!Contains(actual, key, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a specific key/value pair is present.</summary>
    /// <param name="pairs">The pairs to search.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="value">The value to find.</param>
    /// <returns><see langword="true"/> when the pair exists.</returns>
    private static bool Contains(List<(string Key, string Value)> pairs, string key, string value)
    {
        foreach (var pair in pairs)
        {
            if (pair.Key == key && pair.Value == value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Cancels the token source, using the async cancellation path where the framework provides it.</summary>
    /// <param name="source">The token source to cancel.</param>
    /// <returns>A task that completes once cancellation has been requested.</returns>
    private static Task CancelAsync(CancellationTokenSource source)
    {
#if NET8_0_OR_GREATER
        return source.CancelAsync();
#else
        source.Cancel();
        return Task.CompletedTask;
#endif
    }

    /// <summary>Builds the configured response for a matched route.</summary>
    /// <param name="response">The matched reply.</param>
    /// <param name="request">The request being answered.</param>
    /// <returns>The response message.</returns>
    private async Task<HttpResponseMessage> BuildResponseAsync(StubResponse response, HttpRequestMessage request)
    {
        if (response.ResponderAsync is not null)
        {
            var custom = await response.ResponderAsync(request).ConfigureAwait(false);
            custom.RequestMessage ??= request;
            return custom;
        }

        if (response.Responder is not null)
        {
            var custom = response.Responder(request);
            custom.RequestMessage ??= request;
            return custom;
        }

        var message = new HttpResponseMessage(response.Status) { RequestMessage = request };

        if (response.Content is not null)
        {
            message.Content = response.Content;
        }
        else if (response.BodyFactory is not null)
        {
            message.Content = response.BodyFactory(_serializer);
        }
        else if (response.Json is not null)
        {
            message.Content = new StringContent(response.Json, Encoding.UTF8, "application/json");
        }
        else if (response.Text is not null)
        {
            message.Content = new StringContent(response.Text, Encoding.UTF8, response.ContentType ?? "text/plain");
        }

        return message;
    }

    /// <summary>Deserializes a buffered request body using the captured serializer.</summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="body">The buffered body, or <see langword="null"/> when none was captured.</param>
    /// <returns>The deserialized body, or <see langword="default"/> when there is no content.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4018:Generic methods should provide type parameters",
        Justification = "The body type is intentionally specified explicitly by the caller, like a deserialization target.")]
    private Task<T?> DeserializeBodyAsync<T>(CapturedBody? body)
    {
        if (body is not { } captured)
        {
            return Task.FromResult<T?>(default);
        }

        var content = new StringContent(captured.Text, Encoding.UTF8, captured.MediaType ?? "application/json");
        return _serializer.FromHttpContentAsync<T>(content);
    }

    /// <summary>
    /// Reads the request body once and swaps in a re-readable copy that preserves the original content headers
    /// (including a null <c>Content-Length</c>), then records it for typed capture.
    /// </summary>
    /// <param name="request">The request whose body to buffer.</param>
    /// <param name="index">The slot in <see cref="_bodies"/> to fill.</param>
    /// <returns>A task that completes once the body is buffered (or skipped).</returns>
    private async Task BufferRequestAsync(HttpRequestMessage request, int index)
    {
        var content = request.Content;
        if (content is null)
        {
            return;
        }

        // Read the declared length before buffering; reading the body can populate a streamed body's length.
        var hadLength = content.Headers.ContentLength.HasValue;

        byte[] bytes;
        try
        {
            bytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A body that cannot be buffered (e.g. an already-consumed one-shot stream) is left as-is.
            return;
        }

        var replacement = new BufferedContent(bytes, hadLength);
        foreach (var header in content.Headers)
        {
            if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _ = replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        request.Content = replacement;

        lock (_gate)
        {
            _bodies[index] = new CapturedBody(Encoding.UTF8.GetString(bytes), content.Headers.ContentType?.MediaType);
        }
    }

    /// <summary>Throws if any non-reusable route has not yet been consumed.</summary>
    /// <exception cref="InvalidOperationException">One or more expected routes were never hit.</exception>
    private void ThrowIfOutstanding()
    {
        var missing = new List<string>();
        lock (_gate)
        {
            for (var i = 0; i < _routes.Count; i++)
            {
                var route = _routes[i];
                if (route.Reusable || route.Fallback || _consumed[i])
                {
                    continue;
                }

                missing.Add($"  - {route.Method?.Method ?? "ANY"} {route.Template}");
            }
        }

        if (missing.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{missing.Count} expected request(s) were not made:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>Marks a non-reusable route consumed and signals completion once the last one is hit.</summary>
    /// <param name="index">The index of the route that satisfied a request.</param>
    /// <remarks>
    /// Excluded from coverage: the double-consume guard only triggers when two requests race the same
    /// one-shot route between matching and consumption, which cannot be exercised deterministically.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    private void Consume(int index)
    {
        lock (_gate)
        {
            if (_consumed[index])
            {
                return;
            }

            _consumed[index] = true;
            if (--_outstanding == 0)
            {
                _ = _allConsumed.TrySetResult(true);
            }
        }
    }

    /// <summary>Applies the configured <see cref="Behavior"/> to a matched request, if any.</summary>
    /// <param name="request">The request being answered.</param>
    /// <param name="cancellationToken">A token to cancel the simulated delay.</param>
    /// <returns>An injected error response, or <c>null</c> to use the matched route's response.</returns>
    /// <exception cref="Exception">A simulated network failure produced by the behavior.</exception>
    private async Task<HttpResponseMessage?> ApplyBehaviorAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var behavior = Behavior;
        if (behavior is null)
        {
            return null;
        }

        await Task.Delay(behavior.NextDelay(), cancellationToken).ConfigureAwait(false);

        if (behavior.NextIsFailure())
        {
            throw behavior.CreateFailure();
        }

        if (!behavior.NextIsError())
        {
            return null;
        }

        var error = behavior.CreateErrorResponse();
        error.RequestMessage ??= request;
        return error;
    }

    /// <summary>Finds the first eligible route of the requested priority tier that matches the request.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="tier">The priority tier to search.</param>
    /// <param name="cancellationToken">A token to cancel body reads.</param>
    /// <returns>The matching route index, or <c>-1</c> if none match.</returns>
    private async Task<int> FindMatchAsync(HttpRequestMessage request, RouteTier tier, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RouteMatcher[] routes;
        bool[] consumed;
        lock (_gate)
        {
            routes = _routes.ToArray();
            consumed = _consumed.ToArray();
        }

        for (var i = 0; i < routes.Length; i++)
        {
            var route = routes[i];
            if (TierOf(route) != tier || (tier == RouteTier.OneShot && consumed[i]))
            {
                continue;
            }

            if (await MatchesAsync(route, request, cancellationToken).ConfigureAwait(false))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>A request body buffered at send time, with its media type, for later typed deserialization.</summary>
    /// <param name="Text">The raw request body text.</param>
    /// <param name="MediaType">The request content media type, if any.</param>
    private readonly record struct CapturedBody(string Text, string? MediaType);

    /// <summary>
    /// A re-readable in-memory replacement for a consumed request body that reports a length only when the
    /// original did, so a streamed body still appears to have no <c>Content-Length</c>.
    /// </summary>
    /// <remarks>Initializes a new instance of the <see cref="BufferedContent"/> class.</remarks>
    /// <param name="bytes">The buffered body bytes.</param>
    /// <param name="knownLength">Whether the original content reported a length.</param>
    private sealed class BufferedContent(byte[] bytes, bool knownLength) : HttpContent
    {
        /// <summary>The buffered body bytes.</summary>
        private readonly byte[] _bytes = bytes;

        /// <summary>Whether the original content reported a length.</summary>
        private readonly bool _knownLength = knownLength;

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(_bytes, 0, _bytes.Length);

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = _bytes.Length;
            return _knownLength;
        }
    }
}
