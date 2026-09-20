// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Says whether another page follows a fetched page and, if so, which token requests it.</summary>
/// <typeparam name="TToken">The token type: a cursor, an offset, a link, or any value the page-fetching call needs.</typeparam>
[System.Diagnostics.DebuggerDisplay("HasNext = {HasNext}, Token = {Token}")]
public readonly record struct PageContinuation<TToken>
{
    /// <summary>Initializes a new instance of the <see cref="PageContinuation{TToken}"/> struct that requests another page.</summary>
    /// <param name="token">The token passed to the next page-fetching call.</param>
    internal PageContinuation(TToken token)
    {
        HasNext = true;
        Token = token;
    }

    /// <summary>Gets the continuation of a page with no successor; <c>default</c> is equivalent.</summary>
    public static PageContinuation<TToken> End => default;

    /// <summary>Gets a value indicating whether another page follows.</summary>
    public bool HasNext { get; }

    /// <summary>Gets the token that requests the next page; meaningful only when <see cref="HasNext"/> is <see langword="true"/>.</summary>
    public TToken? Token { get; }
}
