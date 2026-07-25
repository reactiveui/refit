// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.EscapedIdentifierLive;

/// <summary>Exercises generated bindings for escaped C# identifiers.</summary>
internal interface IEscapedIdentifierLiveApi
{
    /// <summary>Uses an escaped identifier as a path parameter.</summary>
    /// <param name="namespace">The path value.</param>
    /// <returns>The response body.</returns>
    [Get("/lookup/{namespace}")]
    Task<string> ByNamespace(string @namespace);

    /// <summary>Uses escaped identifiers as query parameters.</summary>
    /// <param name="class">The class value.</param>
    /// <param name="event">The event value.</param>
    /// <returns>The response body.</returns>
    [Get("/query")]
    Task<string> Query(string @class, string @event);

    /// <summary>Uses an escaped identifier as a header parameter.</summary>
    /// <param name="internal">The header value.</param>
    /// <returns>The response body.</returns>
    [Get("/header")]
    Task<string> WithHeader([Header("X-Value")] string @internal);
}
