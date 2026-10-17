// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Configures HTTP retry responses.</summary>
[System.Diagnostics.DebuggerDisplay("HttpRetryResponsesOptions")]
public sealed class HttpRetryResponsesOptions
{
    /// <summary>Gets or sets the response predicate. Null uses the default transient status rules.</summary>
    public Func<HttpResponseMessage, bool>? ShouldRetry { get; set; }

    /// <summary>Gets or sets the response delay reader. Null disables server hints.</summary>
    public Func<HttpResponseMessage, DateTimeOffset, CancellationToken, TimeSpan?>? DelayProvider { get; set; } = RetryDelayProviders.RetryAfter;
}
