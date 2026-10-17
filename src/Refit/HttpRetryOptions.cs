// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures HTTP retries. A handler snapshots the options at construction; callbacks must be thread-safe.</summary>
[System.Diagnostics.DebuggerDisplay("MaxRetries = {MaxRetries}")]
public sealed class HttpRetryOptions
{
    /// <summary>Gets the backoff options.</summary>
    public HttpRetryBackoffOptions Backoff { get; } = new();

    /// <summary>Gets the replay options.</summary>
    public HttpRetryReplayOptions Replay { get; } = new();

    /// <summary>Gets the responses options.</summary>
    public HttpRetryResponsesOptions Responses { get; } = new();

    /// <summary>Gets or sets the extra attempts after the first request. Defaults to three; zero disables retries.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Gets or sets the clock for dates and asynchronous waits. Defaults to the system clock.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.BaseDelay"/>.</summary>
    public TimeSpan BaseDelay
    {
        get => Backoff.BaseDelay;
        set => Backoff.BaseDelay = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.MaxDelay"/>.</summary>
    public TimeSpan MaxDelay
    {
        get => Backoff.MaxDelay;
        set => Backoff.MaxDelay = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.UseJitter"/>.</summary>
    public bool UseJitter
    {
        get => Backoff.UseJitter;
        set => Backoff.UseJitter = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.JitterMode"/>.</summary>
    public RetryJitterMode JitterMode
    {
        get => Backoff.JitterMode;
        set => Backoff.JitterMode = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.JitterRatio"/>.</summary>
    public double JitterRatio
    {
        get => Backoff.JitterRatio;
        set => Backoff.JitterRatio = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryBackoffOptions.ServerDelayJitterRatio"/>.</summary>
    public double ServerDelayJitterRatio
    {
        get => Backoff.ServerDelayJitterRatio;
        set => Backoff.ServerDelayJitterRatio = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryReplayOptions.RetryUnsafeMethods"/>.</summary>
    public bool RetryUnsafeMethods
    {
        get => Replay.RetryUnsafeMethods;
        set => Replay.RetryUnsafeMethods = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryReplayOptions.MaxRequestContentBufferSize"/>.</summary>
    public int MaxRequestContentBufferSize
    {
        get => Replay.MaxRequestContentBufferSize;
        set => Replay.MaxRequestContentBufferSize = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryReplayOptions.RetryHttpRequestExceptions"/>.</summary>
    public bool RetryHttpRequestExceptions
    {
        get => Replay.RetryHttpRequestExceptions;
        set => Replay.RetryHttpRequestExceptions = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryResponsesOptions.ShouldRetry"/>.</summary>
    public Func<HttpResponseMessage, bool>? ShouldRetry
    {
        get => Responses.ShouldRetry;
        set => Responses.ShouldRetry = value;
    }

    /// <summary>Gets or sets <see cref="HttpRetryResponsesOptions.DelayProvider"/>.</summary>
    public Func<HttpResponseMessage, DateTimeOffset, CancellationToken, TimeSpan?>? DelayProvider
    {
        get => Responses.DelayProvider;
        set => Responses.DelayProvider = value;
    }
}
