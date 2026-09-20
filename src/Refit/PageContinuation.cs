// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit;

/// <summary>Creates <see cref="PageContinuation{TToken}"/> values for a continuation selector.</summary>
public static class PageContinuation
{
    /// <summary>Creates a continuation that requests the next page using the supplied token.</summary>
    /// <typeparam name="TToken">The token type: a cursor, an offset, a link, or any value the page-fetching call needs.</typeparam>
    /// <param name="next">The value passed to the next page-fetching call; <see langword="null"/> or an empty string ends the sequence, so a nullable cursor can be passed as read.</param>
    /// <returns>A continuation with a next page, or <see cref="PageContinuation{TToken}.End"/> when <paramref name="next"/> is <see langword="null"/> or an empty string.</returns>
    public static PageContinuation<TToken> To<TToken>(TToken next) => next is null or string { Length: 0 } ? default : new(next);
}
