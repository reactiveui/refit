// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures HTTP retry replay.</summary>
[System.Diagnostics.DebuggerDisplay("HttpRetryReplayOptions")]
public sealed class HttpRetryReplayOptions
{
    /// <summary>Gets or sets a value indicating whether methods other than GET, HEAD, OPTIONS and TRACE may be replayed.</summary>
    public bool RetryUnsafeMethods { get; set; }

    /// <summary>Gets or sets the maximum buffered request body size in bytes. Defaults to one MiB.</summary>
    public int MaxRequestContentBufferSize { get; set; } = 1_048_576;

    /// <summary>Gets or sets a value indicating whether transport failures are retried. Cancellation is never retried.</summary>
    public bool RetryHttpRequestExceptions { get; set; } = true;
}
