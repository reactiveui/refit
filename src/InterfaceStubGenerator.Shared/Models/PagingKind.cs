// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Classifies how a paged method identifies the page to request.</summary>
internal enum PagingKind
{
    /// <summary>A token parameter carries the continuation, such as a cursor, an offset or a page number.</summary>
    Token = 0,

    /// <summary>The server names the next page with a link, which replaces the request URI.</summary>
    Link = 1,
}
