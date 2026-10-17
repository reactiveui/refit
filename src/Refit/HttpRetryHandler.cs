// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
using System.Net;
using System.Security.Cryptography;

namespace Refit;

/// <summary>Retries transient HTTP failures using fresh requests and bounded, replayable bodies. Works with any HttpClient.</summary>
[System.Diagnostics.DebuggerDisplay("HTTP retry handler")]
public sealed class HttpRetryHandler : DelegatingHandler
{
    /// <summary>The immutable-to-callers configuration snapshot.</summary>
    private readonly HttpRetryOptions _options;

    /// <summary>Initializes a new instance of the <see cref="HttpRetryHandler"/> class.</summary>
    public HttpRetryHandler()
        : this(new HttpRetryOptions())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HttpRetryHandler"/> class.</summary>
    /// <param name="options">The retry configuration.</param>
    /// <exception cref="ArgumentNullException">The options or clock is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or delay is outside its permitted range.</exception>
    public HttpRetryHandler(HttpRetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BaseDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MaxDelay.TotalMilliseconds, uint.MaxValue - 1D);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.BaseDelay, options.MaxDelay);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxRequestContentBufferSize);
        ArgumentNullException.ThrowIfNull(options.TimeProvider);
        _options = new()
        {
            MaxRetries = options.MaxRetries,
            BaseDelay = options.BaseDelay,
            MaxDelay = options.MaxDelay,
            UseJitter = options.UseJitter,
            RetryUnsafeMethods = options.RetryUnsafeMethods,
            MaxRequestContentBufferSize = options.MaxRequestContentBufferSize,
            RetryHttpRequestExceptions = options.RetryHttpRequestExceptions,
            ShouldRetry = options.ShouldRetry,
            DelayProvider = options.DelayProvider,
            TimeProvider = options.TimeProvider,
        };
    }

    /// <summary>Sends a request, disposing failed responses before waiting and preserving the final response for the caller.</summary>
    /// <param name="request">The caller-owned request.</param>
    /// <param name="cancellationToken">Cancels buffering, sending and backoff.</param>
    /// <returns>The final response.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_options.MaxRetries == 0 || (!_options.RetryUnsafeMethods && !IsSafe(request.Method)))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var body = await BufferContentAsync(request.Content, cancellationToken).ConfigureAwait(false);
        var attempt = -1;
        while (true)
        {
            ++attempt;
            cancellationToken.ThrowIfCancellationRequested();
            using var copy = CloneRequest(request, body);
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(copy, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (CanRetryException(attempt, cancellationToken))
            {
                await Task.Delay(GetBackoff(attempt), _options.TimeProvider, cancellationToken).ConfigureAwait(false);
                continue;
            }

            TimeSpan delay;
            try
            {
                if (attempt >= _options.MaxRetries || !TryGetDelay(response, attempt, cancellationToken, out delay))
                {
                    // The cloned request is owned by this handler, but response metadata must remain usable.
                    if (ReferenceEquals(response.RequestMessage, copy))
                    {
                        response.RequestMessage = request;
                    }

                    return response;
                }
            }
            catch
            {
                response.Dispose();
                throw;
            }

            response.Dispose();
            await Task.Delay(delay, _options.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Checks the HTTP methods that do not change server state.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <returns>Whether replay is enabled without explicit opt-in.</returns>
    private static bool IsSafe(HttpMethod method) =>
        method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options || method == HttpMethod.Trace;

    /// <summary>Checks the default transient failure statuses.</summary>
    /// <param name="status">The response status.</param>
    /// <returns>Whether the status is normally retryable.</returns>
    private static bool IsTransient(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    /// <summary>Copies request metadata and creates a separately owned content wrapper.</summary>
    /// <param name="request">The original request.</param>
    /// <param name="body">The buffered body, or null.</param>
    /// <returns>A request owned by the retry loop.</returns>
    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? body)
    {
        var copy = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version, VersionPolicy = request.VersionPolicy };
        foreach (var header in request.Headers)
        {
            _ = copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            copy.Options.Set(new(option.Key), option.Value);
        }

        if (body is not null)
        {
            copy.Content = new ByteArrayContent(body);

            // A buffered body is produced only when the original request has content.
            foreach (var header in request.Content!.Headers)
            {
                _ = copy.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return copy;
    }

    /// <summary>Buffers a body once for all attempts without consuming an unbounded stream.</summary>
    /// <param name="content">The original content.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <returns>The body bytes, or null.</returns>
    private async Task<byte[]?> BufferContentAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return null;
        }

        await using var buffer = new RetryRequestContentBuffer(_options.MaxRequestContentBufferSize, cancellationToken);
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Checks whether a transport failure has attempts remaining.</summary>
    /// <param name="attempt">The current attempt.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    private bool CanRetryException(int attempt, CancellationToken cancellationToken) =>
        _options.RetryHttpRequestExceptions && attempt < _options.MaxRetries && !cancellationToken.IsCancellationRequested;

    /// <summary>Computes the wait only for eligible responses.</summary>
    /// <param name="response">The failed response.</param>
    /// <param name="attempt">The zero-based attempt.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <param name="delay">The wait before the next attempt.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    private bool TryGetDelay(HttpResponseMessage response, int attempt, CancellationToken cancellationToken, out TimeSpan delay)
    {
        var hint = _options.DelayProvider?.Invoke(response, _options.TimeProvider.GetUtcNow(), cancellationToken);
        delay = hint ?? GetBackoff(attempt);
        return ShouldRetryResponse(response, hint) && delay >= TimeSpan.Zero && delay <= _options.MaxDelay;
    }

    /// <summary>Applies a response predicate or the default transient classification.</summary>
    /// <param name="response">The failed response.</param>
    /// <param name="hint">The parsed server interval.</param>
    /// <returns>Whether the response qualifies for retry.</returns>
    private bool ShouldRetryResponse(HttpResponseMessage response, TimeSpan? hint) =>
        _options.ShouldRetry?.Invoke(response)
        ?? (IsTransient(response.StatusCode) || (response.StatusCode == HttpStatusCode.Forbidden && hint.HasValue));

    /// <summary>Computes bounded exponential backoff, optionally with full jitter.</summary>
    /// <param name="attempt">The zero-based attempt.</param>
    /// <returns>The fallback interval.</returns>
    private TimeSpan GetBackoff(int attempt)
    {
        if (_options.BaseDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var milliseconds = Math.Min(_options.MaxDelay.TotalMilliseconds, Math.ScaleB(_options.BaseDelay.TotalMilliseconds, attempt));
        if (_options.UseJitter)
        {
            milliseconds *= RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
#endif
