// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The captured response callbacks.</summary>
/// <param name="ShouldRetry">The response eligibility predicate.</param>
/// <param name="DelayProvider">The server hint reader.</param>
internal readonly record struct RetryResponseConfiguration(
    Func<HttpResponseMessage, bool>? ShouldRetry,
    Func<HttpResponseMessage, DateTimeOffset, CancellationToken, TimeSpan?>? DelayProvider);
