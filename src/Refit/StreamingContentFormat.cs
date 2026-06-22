// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Describes how a streamed response body is framed when deserializing an <see cref="IAsyncEnumerable{T}"/>.</summary>
public enum StreamingContentFormat
{
    /// <summary>The body is a single JSON array whose elements are yielded as they are read.</summary>
    JsonArray,

    /// <summary>The body is newline-delimited JSON (JSON Lines): one JSON value per line. See <see href="https://jsonlines.org"/>.</summary>
    JsonLines
}
