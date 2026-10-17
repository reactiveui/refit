// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The immutable retry policy captured when a handler is constructed.</summary>
/// <param name="MaxRetries">The extra attempt count.</param>
/// <param name="Backoff">The delay policy.</param>
/// <param name="Replay">The request replay limits.</param>
/// <param name="Responses">The response callbacks.</param>
/// <param name="TimeProvider">The retry clock.</param>
internal sealed record RetryConfiguration(
    int MaxRetries,
    RetryBackoffConfiguration Backoff,
    RetryReplayConfiguration Replay,
    RetryResponseConfiguration Responses,
    TimeProvider TimeProvider);
