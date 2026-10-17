// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Defines the random variation applied to fallback exponential backoff.</summary>
public enum RetryJitterMode
{
    /// <summary>Chooses an interval from zero to the bounded exponential backoff. This is the default.</summary>
    Full = 0,

    /// <summary>Varies the backoff above or below its nominal value by JitterRatio, bounded by MaxDelay.</summary>
    Symmetric = 1,
}
