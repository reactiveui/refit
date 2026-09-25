// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace Refit.Testing.Tests;

/// <summary>
/// Unit and integration tests for <see cref="RequestCapture"/>, <see cref="CapturedRequestBody"/> and
/// <see cref="CapturingContent"/>: how <see cref="StubHttp"/> lets a streaming upload be consumed without
/// pre-reading it, and how a bounded recording behaves.
/// </summary>
[SuppressMessage("Performance", "PSH1313:A synchronous call where an async overload fits", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "PSH1314:Use the Memory-based ReadAsync overload", Justification = "These tests intentionally exercise the byte-array Stream overrides.")]
[SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "These tests intentionally exercise the synchronous Stream overrides.")]
[SuppressMessage("Performance", "CA1835:Prefer the memory-based Stream overloads", Justification = "These tests intentionally exercise the byte-array Stream overrides.")]
public sealed class RequestCaptureTests
{
    /// <summary>The base address used by these tests.</summary>
    private const string BaseUrl = "https://api.test";

    /// <summary>The route template used by the streaming-upload tests.</summary>
    private const string UploadTemplate = "/upload";

    /// <summary>The route template used by the typed-capture tests.</summary>
    private const string UsersTemplate = "/users";

    /// <summary>The number of items yielded by <see cref="LazyUsers"/>.</summary>
    private const int LazyUserCount = 3;

    /// <summary>A capture limit large enough to hold every test body used here in full.</summary>
    private const int AmpleLimit = 4096;

    /// <summary>A capture limit too small to hold the bodies used by the truncation test.</summary>
    private const int TinyLimit = 2;

    /// <summary>The serializer used to build request/response content directly.</summary>
    private static readonly SystemTextJsonContentSerializer Serializer = new();

    /// <summary>Verifies <see cref="RequestCapture.Bounded(int)"/> rejects a negative limit.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedRejectsNegativeLimit() =>
        await Assert.That(static () => RequestCapture.Bounded(-1)).Throws<ArgumentOutOfRangeException>();

    /// <summary>Verifies the static policy properties carry the documented flags.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StaticPoliciesCarryDocumentedFlags()
    {
        await Assert.That(RequestCapture.Full.IsEnabled).IsTrue();
        await Assert.That(RequestCapture.Full.MaxBytes).IsNull();

        await Assert.That(RequestCapture.None.IsEnabled).IsFalse();
        await Assert.That(RequestCapture.None.MaxBytes).IsNull();

        var bounded = RequestCapture.Bounded(AmpleLimit);
        await Assert.That(bounded.IsEnabled).IsTrue();
        await Assert.That(bounded.MaxBytes).IsEqualTo(AmpleLimit);
    }

    /// <summary>Verifies <see cref="StubHttp.RequestCapture"/> defaults to <see cref="RequestCapture.Full"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task DefaultCaptureIsFull()
    {
        var handler = new StubHttp();

        await Assert.That(handler.RequestCapture).IsSameReferenceAs(RequestCapture.Full);
    }

    /// <summary>
    /// Verifies that with <see cref="RequestCapture.None"/>, a streaming request body is not read before the reply
    /// code (a <c>Reply.From</c> responder) consumes it: nothing is pulled from the lazily-yielding source before
    /// the responder starts running.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NoneCaptureLeavesStreamingUploadForResponderToConsume()
    {
        List<int> pulled = [];
        var pulledBeforeResponder = new int[1];
        var handler = new StubHttp { { Route.Post(UploadTemplate), Reply.From(request => ReadAndRecordAsync(request, pulled, pulledBeforeResponder)) }, };
        handler.RequestCapture = RequestCapture.None;
        using var client = HttpClientTestFactory.Create(handler);
        using var content = new JsonLinesContent(LazyUsers(pulled), Serializer);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{UploadTemplate}") { Content = content };

        _ = await client.SendAsync(request);

        await Assert.That(pulledBeforeResponder[0]).IsEqualTo(0);
        await Assert.That(pulled.Count).IsEqualTo(LazyUserCount);
        await Assert.That(await handler.LastRequestBodyAsync<NewUser>()).IsNull();
    }

    /// <summary>
    /// Verifies that with <see cref="RequestCapture.Bounded(int)"/>, a streaming request body is likewise left
    /// unread until the reply code consumes it, while still being recorded as it passes through.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureLeavesStreamingUploadForResponderToConsume()
    {
        List<int> pulled = [];
        var pulledBeforeResponder = new int[1];
        var handler = new StubHttp { { Route.Post(UploadTemplate), Reply.From(request => ReadAndRecordAsync(request, pulled, pulledBeforeResponder)) }, };
        handler.RequestCapture = RequestCapture.Bounded(AmpleLimit);
        using var client = HttpClientTestFactory.Create(handler);
        using var content = new JsonLinesContent(LazyUsers(pulled), Serializer);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{UploadTemplate}") { Content = content };

        _ = await client.SendAsync(request);

        await Assert.That(pulledBeforeResponder[0]).IsEqualTo(0);
        await Assert.That(pulled.Count).IsEqualTo(LazyUserCount);
    }

    /// <summary>Verifies a bounded capture within the limit and read to the end can be deserialized by <see cref="StubHttp.LastRequestBodyAsync{T}"/>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureWithinLimitAndReadToEndDeserializes()
    {
        var handler = new StubHttp { { Route.Post(UsersTemplate), Reply.From(DrainAsync) }, };
        handler.RequestCapture = RequestCapture.Bounded(AmpleLimit);
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        _ = await api.CreateUserResponse(new("carol"));

        var body = await handler.LastRequestBodyAsync<NewUser>();
        await Assert.That(body!.Login).IsEqualTo("carol");
    }

    /// <summary>Verifies a bounded capture exceeding the limit is reported as truncated when read.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureExceedingLimitThrowsWhenRead()
    {
        var handler = new StubHttp { { Route.Post(UsersTemplate), Reply.From(DrainAsync) }, };
        handler.RequestCapture = RequestCapture.Bounded(TinyLimit);
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        _ = await api.CreateUserResponse(new("dave"));

        await Assert.That(() => handler.LastRequestBodyAsync<NewUser>()).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies a bounded capture not read to the end throws rather than returning a partial body.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureNotReadToEndThrows()
    {
        var handler = new StubHttp { { Route.Post(UsersTemplate), Reply.Status(HttpStatusCode.OK) }, };
        handler.RequestCapture = RequestCapture.Bounded(AmpleLimit);
        var api = handler.CreateClient<IUserApi>(BaseUrl);

        _ = await api.CreateUserResponse(new("erin"));

        await Assert.That(() => handler.LastRequestBodyAsync<NewUser>()).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies a bounded capture records a request body that carries no <c>Content-Type</c> header.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BoundedCaptureHandlesMissingContentType()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/raw" }, Reply.From(DrainAsync) }, };
        handler.RequestCapture = RequestCapture.Bounded(AmpleLimit);
        using var client = HttpClientTestFactory.Create(handler);
        using var content = new ByteArrayContent("{\"Login\":\"z\"}"u8.ToArray());
        content.Headers.ContentType = null;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/raw") { Content = content };

        _ = await client.SendAsync(request);

        var body = await handler.LastRequestBodyAsync<NewUser>();
        await Assert.That(body!.Login).IsEqualTo("z");
    }

    /// <summary>Verifies a route that matches on the request body throws when the capture policy leaves the body unread.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyMatcherRequiresFullCapture()
    {
        var handler = new StubHttp { { new RouteMatcher { Method = HttpMethod.Post, Template = "/b", Body = "x" }, Reply.Status(HttpStatusCode.OK) }, };
        handler.RequestCapture = RequestCapture.None;
        using var client = HttpClientTestFactory.Create(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/b") { Content = new StringContent("x") };

        await Assert.That(async () => _ = await client.SendAsync(request)).ThrowsExactly<InvalidOperationException>();
    }

    /// <summary>Directly exercises <see cref="CapturedRequestBody"/>: append within the limit, then a normal completed read.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturedRequestBodyRoundTripsWithinLimit()
    {
        var body = new CapturedRequestBody("application/json", AmpleLimit);

        body.Append("hello"u8);
        body.Complete();

        await Assert.That(body.MediaType).IsEqualTo("application/json");
        await Assert.That(body.GetText()).IsEqualTo("hello");
    }

    /// <summary>Verifies <see cref="CapturedRequestBody.Append"/> truncates once the limit is exceeded and <see cref="CapturedRequestBody.GetText"/> then throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturedRequestBodyTruncatesPastLimit()
    {
        var body = new CapturedRequestBody(null, TinyLimit);

        body.Append("hello world"u8);
        body.Complete();

        await Assert.That(() => body.GetText()).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies an unlimited <see cref="CapturedRequestBody"/> never truncates.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturedRequestBodyUnlimitedNeverTruncates()
    {
        const int Length = 5000;
        var body = new CapturedRequestBody(null, null);

        body.Append(Encoding.UTF8.GetBytes(new string('x', Length)));
        body.Complete();

        await Assert.That(body.GetText().Length).IsEqualTo(Length);
    }

    /// <summary>Verifies <see cref="CapturedRequestBody.GetText"/> throws before the body has been read to the end.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturedRequestBodyThrowsWhenIncomplete()
    {
        var body = new CapturedRequestBody(null, AmpleLimit);

        body.Append("hi"u8);

        await Assert.That(() => body.GetText()).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies <see cref="CapturingContent"/> copies the inner headers except <c>Content-Length</c>.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturingContentCopiesHeadersExceptContentLength()
    {
        using var inner = new StringContent("abc");
        inner.Headers.Add("X-Test", "value");
        var capture = new CapturedRequestBody(null, null);

        using var content = new CapturingContent(inner, capture);

        await Assert.That(content.Headers.Contains("Content-Length")).IsFalse();
        await Assert.That(content.Headers.GetValues("X-Test").Single()).IsEqualTo("value");
    }

    /// <summary>Verifies <see cref="CapturingContent"/> reports a known length only when the inner content reports one.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturingContentReportsLengthFromInner()
    {
        using var known = new StringContent("abc");
        using var withKnownLength = new CapturingContent(known, new CapturedRequestBody(null, null));

        using var unknown = new JsonLinesContent(Array.Empty<User>(), Serializer);
        using var withUnknownLength = new CapturingContent(unknown, new CapturedRequestBody(null, null));

        await Assert.That(withKnownLength.Headers.ContentLength).IsNotNull();
        await Assert.That(withUnknownLength.Headers.ContentLength).IsNull();
    }

    /// <summary>Verifies the write path (<c>SerializeToStreamAsync</c>/<c>CopyToAsync</c>) records the bytes written and completes the capture.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturingContentWritePathRecordsAndCompletes()
    {
        const string Body = "payload-write";
        using var inner = new StringContent(Body);
        var capture = new CapturedRequestBody(null, null);
        using var content = new CapturingContent(inner, capture);
        await using var target = new MemoryStream();

        await content.CopyToAsync(target);

        await Assert.That(capture.GetText()).IsEqualTo(Body);
    }

    /// <summary>Verifies the read path (<c>CreateContentReadStreamAsync</c>) records bytes as they are read and completes the capture at the end.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CapturingContentReadPathRecordsAndCompletes()
    {
        const string Body = "payload-read";
        using var inner = new StringContent(Body);
        var capture = new CapturedRequestBody(null, null);
        using var content = new CapturingContent(inner, capture);

        await using var stream = await content.ReadAsStreamAsync();
        await using var target = new MemoryStream();
        await stream.CopyToAsync(target);

        await Assert.That(capture.GetText()).IsEqualTo(Body);
    }

    /// <summary>Verifies <see cref="CapturingContent.CaptureStream"/> directly: sync/async read and write, both dispose modes, and the unsupported members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CaptureStreamSyncAndAsyncReadWriteAndDisposeOwnership()
    {
        const int HalfLength = 3;
        var capture = new CapturedRequestBody(null, null);
        var innerDisposed = new TrackingDisposeStream("abcdef"u8.ToArray());
        var owning = new CapturingContent.CaptureStream(innerDisposed, capture, ownsInner: true);

        await Assert.That(owning.CanRead).IsTrue();
        await Assert.That(owning.CanWrite).IsTrue();
        await Assert.That(() => owning.Length).Throws<NotSupportedException>();
        await Assert.That(() => owning.Position).Throws<NotSupportedException>();
        await Assert.That(() => owning.Position = 1).Throws<NotSupportedException>();
        await Assert.That(() => owning.Seek(0, SeekOrigin.Begin)).Throws<NotSupportedException>();
        await Assert.That(() => owning.SetLength(1)).Throws<NotSupportedException>();

        var buffer = new byte[HalfLength];
        var read = owning.Read(buffer, 0, HalfLength);
        await Assert.That(read).IsEqualTo(HalfLength);
        await Assert.That(Encoding.UTF8.GetString(buffer)).IsEqualTo("abc");

        var asyncBuffer = new byte[HalfLength];
        var asyncRead = await owning.ReadAsync(asyncBuffer, 0, HalfLength, CancellationToken.None);
        await Assert.That(asyncRead).IsEqualTo(HalfLength);
        await Assert.That(Encoding.UTF8.GetString(asyncBuffer)).IsEqualTo("def");

        // Reading past the end records the completion via the empty read.
        var trailing = await owning.ReadAsync(new byte[1], 0, 1, CancellationToken.None);
        await Assert.That(trailing).IsEqualTo(0);
        await Assert.That(capture.GetText()).IsEqualTo("abcdef");

        owning.Flush();
        await owning.FlushAsync(CancellationToken.None);
        var tail1 = "gh"u8.ToArray();
        var tail2 = "ij"u8.ToArray();
        owning.Write(tail1, 0, tail1.Length);
        await owning.WriteAsync(tail2, 0, tail2.Length, CancellationToken.None);

        owning.Dispose();
        await Assert.That(innerDisposed.Disposed).IsTrue();

        var trackingInner = new TrackingDisposeStream([]);
        var notOwning = new CapturingContent.CaptureStream(trackingInner, new CapturedRequestBody(null, null), ownsInner: false);
        notOwning.Dispose();
        await Assert.That(trackingInner.Disposed).IsFalse();
    }

    /// <summary>Runs a route responder that reads the request content to the end, recording whether anything had already been pulled from a lazy source.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="pulled">The list of indices pulled so far from the lazy source.</param>
    /// <param name="pulledBeforeResponder">A one-element scratch array recording the pulled count observed when the responder starts.</param>
    /// <returns>A status-200 response.</returns>
    private static async Task<HttpResponseMessage> ReadAndRecordAsync(HttpRequestMessage request, List<int> pulled, int[] pulledBeforeResponder)
    {
        pulledBeforeResponder[0] = pulled.Count;
        _ = await request.Content!.ReadAsStringAsync();
        return new(HttpStatusCode.OK);
    }

    /// <summary>Runs a route responder that reads the request content to the end and replies with status 200.</summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>A status-200 response.</returns>
    private static async Task<HttpResponseMessage> DrainAsync(HttpRequestMessage request)
    {
        _ = await request.Content!.ReadAsStringAsync();
        return new(HttpStatusCode.OK);
    }

    /// <summary>Builds an enumerable of users that records the index it pulls before yielding each one.</summary>
    /// <param name="pulled">The list of indices pulled so far, appended to as the enumerable is advanced.</param>
    /// <returns>The lazily-yielding sequence.</returns>
    private static IEnumerable<User> LazyUsers(List<int> pulled)
    {
        for (var i = 0; i < LazyUserCount; i++)
        {
            pulled.Add(i);
            yield return new User(i, $"u{i}");
        }
    }

    /// <summary>A minimal in-memory stream that records whether it was disposed.</summary>
    private sealed class TrackingDisposeStream : MemoryStream
    {
        /// <summary>Initializes a new instance of the <see cref="TrackingDisposeStream"/> class, growable, seeded with initial bytes at position 0.</summary>
        /// <param name="initial">The initial contents; the stream is left positioned at the start so they can be read back.</param>
        internal TrackingDisposeStream(byte[] initial)
        {
            if (initial.Length == 0)
            {
                return;
            }

            Write(initial, 0, initial.Length);
            Position = 0;
        }

        /// <summary>Gets a value indicating whether <see cref="Dispose(bool)"/> ran.</summary>
        internal bool Disposed { get; private set; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
