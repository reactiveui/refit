// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
namespace Refit;

/// <summary>Configures HTTP retries. A handler takes a snapshot when constructed; callbacks must be thread-safe.</summary>
[System.Diagnostics.DebuggerDisplay("MaxRetries = {MaxRetries}, BaseDelay = {BaseDelay}")]
public sealed class HttpRetryOptions
{
    /// <summary>The default extra attempt count.</summary>
    private const int DefaultMaxRetries = 3;

    /// <summary>The default maximum wait in seconds.</summary>
    private const int DefaultMaxDelaySeconds = 30;

    /// <summary>The default maximum body buffer in bytes.</summary>
    private const int DefaultMaxRequestContentBufferSize = 1_048_576;

    /// <summary>Gets or sets the extra attempts after the first request. Defaults to three; zero disables retries.</summary>
    public int MaxRetries { get; set; } = DefaultMaxRetries;

    /// <summary>Gets or sets the initial exponential backoff interval. Defaults to one second.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the maximum wait. Hints exceeding it stop retries rather than retrying before the server permits.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(DefaultMaxDelaySeconds);

    /// <summary>Gets or sets a value indicating whether fallback backoff uses full jitter. Server hints are never shortened by jitter.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether methods other than GET, HEAD, OPTIONS and TRACE may be replayed. Defaults to false.</summary>
    public bool RetryUnsafeMethods { get; set; }

    /// <summary>Gets or sets the maximum buffered request body size in bytes. Larger bodies fail before sending. Defaults to one MiB.</summary>
    public int MaxRequestContentBufferSize { get; set; } = DefaultMaxRequestContentBufferSize;

    /// <summary>Gets or sets a value indicating whether HttpRequestException is retried. Cancellation is never retried.</summary>
    public bool RetryHttpRequestExceptions { get; set; } = true;

    /// <summary>Gets or sets a response predicate. Null retries 408, 429, 500, 502, 503, 504 and 403 carrying a valid retry hint.</summary>
    public Func<HttpResponseMessage, bool>? ShouldRetry { get; set; }

    /// <summary>Gets or sets the response delay reader. Null disables header hints; the default reads standard Retry-After.</summary>
    public Func<HttpResponseMessage, DateTimeOffset, CancellationToken, TimeSpan?>? DelayProvider { get; set; } = RetryDelayProviders.RetryAfter;

    /// <summary>Gets or sets the clock used for dates and asynchronous waits. Defaults to the system clock.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
#endif
