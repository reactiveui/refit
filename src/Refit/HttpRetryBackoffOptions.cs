// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures HTTP retry backoff.</summary>
[System.Diagnostics.DebuggerDisplay("HttpRetryBackoffOptions")]
public sealed class HttpRetryBackoffOptions
{
    /// <summary>The default maximum delay in seconds.</summary>
    private const int DefaultMaxDelaySeconds = 30;

    /// <summary>Gets or sets the initial exponential backoff interval. Defaults to one second.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the maximum wait. Longer server hints stop retries.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(DefaultMaxDelaySeconds);

    /// <summary>Gets or sets a value indicating whether fallback backoff uses jitter.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>Gets or sets the fallback jitter distribution. Defaults to full jitter.</summary>
    public RetryJitterMode JitterMode { get; set; } = RetryJitterMode.Full;

    /// <summary>Gets or sets the symmetric jitter variation, from zero to one. Defaults to 0.2.</summary>
    public double JitterRatio { get; set; } = 0.2;

    /// <summary>Gets or sets the positive jitter above a server delay, from zero to one.</summary>
    public double ServerDelayJitterRatio { get; set; }
}
