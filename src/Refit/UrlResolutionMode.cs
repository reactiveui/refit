// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Defines how Refit combines the client's base address with a method's relative URL.</summary>
public enum UrlResolutionMode
{
    /// <summary>
    /// The historical Refit behavior: relative paths must start with <c>/</c>, the base address path is prepended,
    /// and a trailing slash on the base address is trimmed to avoid a double slash. This is the default.
    /// </summary>
    RefitLegacy,

    /// <summary>
    /// RFC 3986 / <see cref="System.Net.Http.HttpClient"/> resolution: the relative URL is merged with the base
    /// address using <see cref="System.Uri"/> rules. A relative path without a leading slash is appended to the
    /// base address path, while a leading slash replaces it. The leading-slash requirement is relaxed in this mode.
    /// </summary>
    Rfc3986
}
