// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Refit;

/// <summary>Retries transient HTTP failures using fresh requests and bounded, replayable bodies. Works with any HttpClient.</summary>
/// <param name="options">The retry configuration.</param>
[System.Diagnostics.DebuggerDisplay("HTTP retry handler")]
public sealed class HttpRetryHandler(HttpRetryOptions options) : DelegatingHandler
{
    /// <summary>Maps a unit sample onto the symmetric interval from negative one to one.</summary>
    private const double SymmetricSampleRange = 2;

    /// <summary>The validated immutable configuration.</summary>
    private readonly RetryConfiguration _options = ValidateOptions(options);

    /// <summary>Initializes a new instance of the <see cref="HttpRetryHandler"/> class.</summary>
    public HttpRetryHandler()
        : this(new HttpRetryOptions())
    {
    }

    /// <summary>Sends a request, disposing failed responses before waiting and preserving the final response for the caller.</summary>
    /// <param name="request">The caller-owned request.</param>
    /// <param name="cancellationToken">Cancels buffering, sending and backoff.</param>
    /// <returns>The final response.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentExceptionHelper.ThrowIfNull(request);
        if (_options.MaxRetries == 0 || (!_options.Replay.RetryUnsafeMethods && !IsSafe(request.Method)))
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
                await HttpRetryCompatibility.DelayAsync(GetBackoff(attempt), _options.TimeProvider, cancellationToken).ConfigureAwait(false);
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
            await HttpRetryCompatibility.DelayAsync(delay, _options.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Validates the immutable retry configuration.</summary>
    /// <param name="options">The retry configuration.</param>
    /// <returns>The validated configuration.</returns>
    /// <exception cref="ArgumentNullException">The options or clock is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or delay is outside its permitted range.</exception>
    private static RetryConfiguration ValidateOptions(HttpRetryOptions options)
    {
        ArgumentExceptionHelper.ThrowIfNull(options);
        ArgumentOutOfRangeExceptionHelper.ThrowIfNegative(options.MaxRetries);
        if (options.BaseDelay < TimeSpan.Zero || options.BaseDelay > options.MaxDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(options.BaseDelay));
        }

        if (options.MaxDelay < TimeSpan.Zero || options.MaxDelay.TotalMilliseconds > uint.MaxValue - 1D)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxDelay));
        }

        ArgumentOutOfRangeExceptionHelper.ThrowIfNegativeOrZero(options.MaxRequestContentBufferSize);
        ArgumentExceptionHelper.ThrowIfNull(options.TimeProvider);
        if (options.JitterMode is < RetryJitterMode.Full or > RetryJitterMode.Symmetric)
        {
            throw new ArgumentOutOfRangeException(nameof(options.JitterMode));
        }

        ValidateJitterRatio(options.JitterRatio, nameof(options.JitterRatio));
        ValidateJitterRatio(options.ServerDelayJitterRatio, nameof(options.ServerDelayJitterRatio));
        return new(
            options.MaxRetries,
            new(
                options.BaseDelay,
                options.MaxDelay,
                new(options.JitterRatio, options.ServerDelayJitterRatio, options.JitterMode, options.UseJitter)),
            new(options.MaxRequestContentBufferSize, options.RetryUnsafeMethods, options.RetryHttpRequestExceptions),
            new(options.ShouldRetry, options.DelayProvider),
            options.TimeProvider);
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
        status is HttpStatusCode.RequestTimeout or HttpRetryCompatibility.TooManyRequests
            or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;

    /// <summary>Validates a finite fractional jitter ratio.</summary>
    /// <param name="ratio">The configured ratio.</param>
    /// <param name="name">The option name.</param>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is not between zero and one or is NaN.</exception>
    private static void ValidateJitterRatio(double ratio, string name)
    {
        if (ratio is < 0 or > 1 || double.IsNaN(ratio))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>Copies request metadata and creates a separately owned content wrapper.</summary>
    /// <param name="request">The original request.</param>
    /// <param name="body">The buffered body, or null.</param>
    /// <returns>A request owned by the retry loop.</returns>
    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? body)
    {
        var copy = HttpRetryCompatibility.CloneRequestMetadata(request);

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

#if NET8_0_OR_GREATER
        await using var buffer = new RetryRequestContentBuffer(_options.Replay.MaxRequestContentBufferSize, cancellationToken);
#else
        using var buffer = new RetryRequestContentBuffer(_options.Replay.MaxRequestContentBufferSize, cancellationToken);
#endif
        await HttpRetryCompatibility.CopyContentAsync(content, buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>Checks whether a transport failure has attempts remaining.</summary>
    /// <param name="attempt">The current attempt.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    private bool CanRetryException(int attempt, CancellationToken cancellationToken) =>
        _options.Replay.RetryHttpRequestExceptions && attempt < _options.MaxRetries && !cancellationToken.IsCancellationRequested;

    /// <summary>Computes the wait only for eligible responses.</summary>
    /// <param name="response">The failed response.</param>
    /// <param name="attempt">The zero-based attempt.</param>
    /// <param name="cancellationToken">The operation token.</param>
    /// <param name="delay">The wait before the next attempt.</param>
    /// <returns>Whether another attempt is permitted.</returns>
    private bool TryGetDelay(HttpResponseMessage response, int attempt, CancellationToken cancellationToken, out TimeSpan delay)
    {
        var hint = _options.Responses.DelayProvider?.Invoke(response, _options.TimeProvider.GetUtcNow(), cancellationToken);
        delay = hint ?? GetBackoff(attempt);
        if (!ShouldRetryResponse(response, hint) || delay < TimeSpan.Zero || delay > _options.Backoff.MaxDelay)
        {
            return false;
        }

        if (hint.HasValue && _options.Backoff.Jitter.ServerDelayRatio > 0)
        {
            var scale = 1 + (_options.Backoff.Jitter.ServerDelayRatio * HttpRetryCompatibility.NextJitterSample());
            delay = TimeSpan.FromMilliseconds(Math.Min(_options.Backoff.MaxDelay.TotalMilliseconds, delay.TotalMilliseconds * scale));
        }

        return true;
    }

    /// <summary>Applies a response predicate or the default transient classification.</summary>
    /// <param name="response">The failed response.</param>
    /// <param name="hint">The parsed server interval.</param>
    /// <returns>Whether the response qualifies for retry.</returns>
    private bool ShouldRetryResponse(HttpResponseMessage response, TimeSpan? hint) =>
        _options.Responses.ShouldRetry?.Invoke(response)
        ?? (IsTransient(response.StatusCode) || (response.StatusCode == HttpStatusCode.Forbidden && hint.HasValue));

    /// <summary>Computes bounded exponential backoff with the configured jitter distribution.</summary>
    /// <param name="attempt">The zero-based attempt.</param>
    /// <returns>The fallback interval.</returns>
    private TimeSpan GetBackoff(int attempt)
    {
        if (_options.Backoff.BaseDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var milliseconds = Math.Min(_options.Backoff.MaxDelay.TotalMilliseconds, HttpRetryCompatibility.ScaleBackoff(_options.Backoff.BaseDelay.TotalMilliseconds, attempt));
        if (_options.Backoff.Jitter.Enabled)
        {
            var sample = HttpRetryCompatibility.NextJitterSample();
            var scale = _options.Backoff.Jitter.Mode == RetryJitterMode.Symmetric
                ? 1 + (((SymmetricSampleRange * sample) - 1) * _options.Backoff.Jitter.Ratio)
                : sample;
            milliseconds = Math.Min(_options.Backoff.MaxDelay.TotalMilliseconds, milliseconds * scale);
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
