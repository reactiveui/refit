// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Net;
using System.Text;

namespace Refit.NativeAotSmoke;

/// <summary>A test HTTP message handler that serves canned responses for the native AOT smoke test.</summary>
internal sealed class NativeAotSmokeHandler : HttpMessageHandler
{
    /// <summary>Gets a value indicating whether a POST body containing the expected payload was observed.</summary>
    internal bool SawPostBody { get; private set; }

    /// <summary>Gets a value indicating whether a URL-encoded form body was observed.</summary>
    internal bool SawFormBody { get; private set; }

    /// <summary>Gets a value indicating whether the generated query string matched the expected shape.</summary>
    internal bool SawExpectedQuery { get; private set; }

    /// <summary>Gets a value indicating whether a JSON Lines upload body was observed on <c>/uploads</c>.</summary>
    internal bool SawUploadBody { get; private set; }

    /// <summary>Gets the raw JSON Lines body captured from the last <c>/uploads</c> request, or <see langword="null"/> if none has been observed yet.</summary>
    internal string? UploadedBody { get; private set; }

    /// <summary>Gets or sets a callback invoked once per <c>/uploads</c> request, as soon as the producer's first written bytes reach this handler.</summary>
    internal Action? OnUploadFirstBytes { get; set; }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        request.RequestUri?.AbsolutePath switch
        {
            "/todos" => HandleTodosAsync(request, cancellationToken),
            "/echo" => Task.FromResult(Json("""{"id":42,"title":"generic inline"}""")),
            "/forms" => HandleFormsAsync(request, cancellationToken),
            "/uploads" => HandleUploadAsync(request, cancellationToken),
            "/search" => Task.FromResult(HandleSearch(request)),
            "/status" or "/unregistered" => Task.FromResult(Json("""{"name":"native-aot"}""")),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
        };

    /// <summary>Builds an OK response with the given JSON content.</summary>
    /// <param name="content">The JSON content for the response body.</param>
    /// <returns>The constructed JSON response message.</returns>
    private static HttpResponseMessage Json(string content) =>
        new(HttpStatusCode.OK) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    /// <summary>Serves <c>/todos</c>, recording whether the expected title reached this handler.</summary>
    /// <param name="request">The request to serve.</param>
    /// <param name="cancellationToken">A token that cancels reading the body.</param>
    /// <returns>The canned response.</returns>
    private async Task<HttpResponseMessage> HandleTodosAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        SawPostBody = body.Contains("prove native aot", StringComparison.Ordinal);
        return Json("""{"id":42,"title":"prove native aot"}""");
    }

    /// <summary>Serves <c>/forms</c>, recording whether the expected URL-encoded fields reached this handler.</summary>
    /// <param name="request">The request to serve.</param>
    /// <param name="cancellationToken">A token that cancels reading the body.</param>
    /// <returns>The canned response.</returns>
    private async Task<HttpResponseMessage> HandleFormsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        SawFormBody = body.Contains("Name=Ada", StringComparison.Ordinal)
            && body.Contains("Count=2", StringComparison.Ordinal);
        return new(HttpStatusCode.OK) { Content = new StringContent("accepted", Encoding.UTF8, "text/plain") };
    }

    /// <summary>Serves <c>/search</c>, recording whether the generated query string matched the expected shape.</summary>
    /// <param name="request">The request to serve.</param>
    /// <returns>The canned response.</returns>
    private HttpResponseMessage HandleSearch(HttpRequestMessage request)
    {
        SawExpectedQuery = request.RequestUri?.PathAndQuery
            == "/search?q=a%20b&page=3&ids=1&ids=2&sort=date-desc&ready&cursor=x%2Fy";
        return Json("""{"name":"native-aot"}""");
    }

    /// <summary>Reads the JSON Lines upload body incrementally, the way <c>SocketsHttpHandler</c> does: by copying the
    /// request content into a destination stream rather than buffering it whole with <c>ReadAsStringAsync</c>. The
    /// destination stream notifies <see cref="OnUploadFirstBytes"/> as soon as the first bytes arrive, which lets the
    /// caller prove data reaches the handler before the producer finishes producing.</summary>
    /// <param name="request">The upload request.</param>
    /// <param name="cancellationToken">A token that cancels the copy.</param>
    /// <returns>The canned response for a completed upload.</returns>
    private async Task<HttpResponseMessage> HandleUploadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var sawFirstBytes = false;
        var observer = new FirstWriteObserverStream(
            buffer,
            () =>
            {
                if (!sawFirstBytes)
                {
                    sawFirstBytes = true;
                    OnUploadFirstBytes?.Invoke();
                }
            });

        if (request.Content is not null)
        {
            await request.Content.CopyToAsync(observer, cancellationToken).ConfigureAwait(false);
        }

        UploadedBody = Encoding.UTF8.GetString(buffer.ToArray());
        SawUploadBody = UploadedBody.Length > 0;
        return new(HttpStatusCode.OK);
    }

    /// <summary>A write-only stream that forwards every write to an inner stream and calls back once, before the first
    /// write is forwarded. Used to observe when the first bytes of a streamed request body arrive, the same signal a
    /// real socket-backed handler would see as it reads the body incrementally rather than after buffering it
    /// whole.</summary>
    /// <param name="inner">The stream every write is forwarded to.</param>
    /// <param name="onFirstWrite">Invoked once, before the first write is forwarded.</param>
    private sealed class FirstWriteObserverStream(Stream inner, Action onFirstWrite) : Stream
    {
        /// <summary>Set to 1 once the first write has been observed.</summary>
        private int _firstWriteSignaled;

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush() => inner.Flush();

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            SignalFirstWrite();
            inner.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            SignalFirstWrite();
            await inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            SignalFirstWrite();
            await inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Invokes the first-write callback exactly once, on the first call from any write overload.</summary>
        private void SignalFirstWrite()
        {
            if (Interlocked.Exchange(ref _firstWriteSignaled, 1) == 0)
            {
                onFirstWrite();
            }
        }
    }
}
