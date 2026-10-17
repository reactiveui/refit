// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The captured exponential backoff limits and jitter.</summary>
/// <param name="BaseDelay">The first backoff interval.</param>
/// <param name="MaxDelay">The maximum wait.</param>
/// <param name="Jitter">The jitter distribution and bounds.</param>
internal sealed record RetryBackoffConfiguration(
    TimeSpan BaseDelay,
    TimeSpan MaxDelay,
    RetryJitterConfiguration Jitter);
