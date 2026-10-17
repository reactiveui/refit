// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The captured replay limits.</summary>
/// <param name="MaxRequestContentBufferSize">The maximum buffered body size.</param>
/// <param name="RetryUnsafeMethods">Whether unsafe methods may be replayed.</param>
/// <param name="RetryHttpRequestExceptions">Whether transport failures may be retried.</param>
internal readonly record struct RetryReplayConfiguration(
    int MaxRequestContentBufferSize,
    bool RetryUnsafeMethods,
    bool RetryHttpRequestExceptions);
