// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Defines how a server header expresses a retry interval.</summary>
public enum RetryHeaderFormat
{
    /// <summary>A nonnegative integer number of seconds.</summary>
    Seconds = 0,

    /// <summary>A nonnegative integer number of milliseconds.</summary>
    Milliseconds = 1,

    /// <summary>An absolute Unix timestamp in seconds.</summary>
    UnixTimeSeconds = 2,

    /// <summary>An HTTP date, or a nonnegative integer number of seconds.</summary>
    RetryAfter = 3,

    /// <summary>An absolute ISO 8601 timestamp with a UTC offset.</summary>
    Iso8601Date = 4,
}
