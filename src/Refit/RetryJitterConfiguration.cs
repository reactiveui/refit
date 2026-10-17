// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>The captured jitter distribution.</summary>
/// <param name="Ratio">The symmetric variation.</param>
/// <param name="ServerDelayRatio">The positive server hint variation.</param>
/// <param name="Mode">The fallback distribution.</param>
/// <param name="Enabled">Whether fallback jitter is enabled.</param>
internal readonly record struct RetryJitterConfiguration(
    double Ratio,
    double ServerDelayRatio,
    RetryJitterMode Mode,
    bool Enabled);
