// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Refit;

/// <summary>The outcome of one page-fetching call: a page with the continuation computed from it, the end of the sequence, or a failure.</summary>
/// <typeparam name="TPage">The page type returned by the page-fetching call.</typeparam>
/// <typeparam name="TToken">The continuation token type.</typeparam>
[System.Diagnostics.DebuggerDisplay("HasPage = {HasPage}, FetchError = {FetchError}")]
internal sealed class FetchedPage<TPage, TToken>
{
    /// <summary>Initializes a new instance of the <see cref="FetchedPage{TPage, TToken}"/> class.</summary>
    /// <param name="hasPage">Whether the outcome holds a page.</param>
    /// <param name="page">The fetched page.</param>
    /// <param name="continuation">The continuation computed from the page.</param>
    /// <param name="continuationError">The exception the continuation selector threw, or <see langword="null"/>.</param>
    /// <param name="fetchError">The exception the call threw, or <see langword="null"/>.</param>
    private FetchedPage(
        bool hasPage,
        TPage? page,
        PageContinuation<TToken> continuation,
        Exception? continuationError,
        Exception? fetchError)
    {
        HasPage = hasPage;
        Page = page;
        Continuation = continuation;
        ContinuationError = continuationError;
        FetchError = fetchError;
    }

    /// <summary>Gets the outcome that marks the end of the sequence.</summary>
    internal static FetchedPage<TPage, TToken> End { get; } = new(false, default, default, null, null);

    /// <summary>Gets a value indicating whether this holds a page.</summary>
    internal bool HasPage { get; }

    /// <summary>Gets the fetched page; default when <see cref="HasPage"/> is <see langword="false"/>.</summary>
    internal TPage? Page { get; }

    /// <summary>Gets the continuation computed from <see cref="Page"/>.</summary>
    internal PageContinuation<TToken> Continuation { get; }

    /// <summary>Gets the exception the continuation selector threw, surfaced when the sequence advances past the page.</summary>
    internal Exception? ContinuationError { get; }

    /// <summary>Gets the exception the page-fetching call threw, surfaced when the consumer reaches the page.</summary>
    internal Exception? FetchError { get; }

    /// <summary>Creates the outcome of a page that was fetched.</summary>
    /// <param name="page">The fetched page.</param>
    /// <param name="continuation">The continuation computed from the page.</param>
    /// <param name="continuationError">The exception the continuation selector threw, or <see langword="null"/>.</param>
    /// <returns>The outcome holding the page.</returns>
    internal static FetchedPage<TPage, TToken> Of(TPage page, PageContinuation<TToken> continuation, Exception? continuationError) =>
        new(true, page, continuation, continuationError, null);

    /// <summary>Creates the outcome of a call that failed.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>The outcome holding the failure.</returns>
    internal static FetchedPage<TPage, TToken> Failed(Exception error) =>
        new(false, default, default, null, error);

    /// <summary>Gets a value indicating whether a following page may be read ahead: the limit is not reached and the continuation is intact.</summary>
    /// <param name="fetched">The number of pages fetched so far.</param>
    /// <param name="maxPages">The most pages fetched.</param>
    /// <returns><see langword="true"/> when the continuation names a following page that the limit permits.</returns>
    internal bool CanAdvance(int fetched, int maxPages) => fetched < maxPages && ContinuationError is null && Continuation.HasNext;

    /// <summary>Decides whether the sequence goes on to a following page, surfacing a failure of the continuation selector.</summary>
    /// <param name="fetched">The number of pages fetched so far.</param>
    /// <param name="maxPages">The most pages fetched.</param>
    /// <returns><see langword="true"/> when the continuation names a following page that the limit permits.</returns>
    internal bool ShouldAdvance(int fetched, int maxPages)
    {
        if (fetched >= maxPages)
        {
            return false;
        }

        ThrowIfContinuationFailed();
        return Continuation.HasNext;
    }

    /// <summary>Rethrows the failure of the page-fetching call, preserving its stack trace.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ThrowIfFailed() => Rethrow(FetchError);

    /// <summary>Rethrows the failure of the continuation selector, preserving its stack trace.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ThrowIfContinuationFailed() => Rethrow(ContinuationError);

    /// <summary>Rethrows a captured failure with its original stack trace, doing nothing when there is none.</summary>
    /// <param name="error">The failure, or <see langword="null"/>.</param>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // The closing brace after a call that never returns is unreachable.
    private static void Rethrow(Exception? error)
    {
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
