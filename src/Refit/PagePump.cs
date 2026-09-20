// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>Drives page-fetching calls lazily, one page per call, with optional single-page read-ahead.</summary>
internal static class PagePump
{
    /// <summary>Fetches pages on demand and yields each until the continuation ends, the page limit is reached, or the consumer stops.</summary>
    /// <typeparam name="TPage">The page type returned by the page-fetching call.</typeparam>
    /// <typeparam name="TToken">The continuation token type.</typeparam>
    /// <param name="first">The token for the first page.</param>
    /// <param name="fetch">Invokes the page-fetching call for a token; called once per page.</param>
    /// <param name="next">Computes the continuation from a page and the token that fetched it.</param>
    /// <param name="prefetch">Whether the next page is requested while the consumer still holds the current one.</param>
    /// <param name="maxPages">The most pages fetched.</param>
    /// <param name="cancellationToken">A token that cancels the sequence, including a call in flight.</param>
    /// <returns>An asynchronous sequence of the fetched pages; a page is disposed when the consumer moves past it.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // async-iterator dispose-mode epilogue: the compiler-generated <>w__disposeMode false-edge cannot be exercised or removed.
    internal static async IAsyncEnumerable<TPage> RunAsync<TPage, TToken>(
        TToken first,
        Func<TToken, CancellationToken, Task<TPage>> fetch,
        Func<TPage, TToken, PageContinuation<TToken>> next,
        bool prefetch,
        int maxPages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<FetchedPage<TPage, TToken>>? readAhead = null;
        try
        {
            var current = await FetchAsync(first, fetch, next, stop.Token).ConfigureAwait(false);
            var fetched = 1;
            while (true)
            {
                current.ThrowIfFailed();
                if (!current.HasPage)
                {
                    break;
                }

                if (prefetch && current.CanAdvance(fetched, maxPages))
                {
                    readAhead = FetchAsync(current.Continuation.Token!, fetch, next, stop.Token);
                }

                try
                {
                    yield return current.Page!;
                }
                finally
                {
                    Dispose(current.Page);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!current.ShouldAdvance(fetched, maxPages))
                {
                    break;
                }

                fetched++;
                current = readAhead is null
                    ? await FetchAsync(current.Continuation.Token!, fetch, next, stop.Token).ConfigureAwait(false)
                    : await readAhead.ConfigureAwait(false);
                readAhead = null;
            }
        }
        finally
        {
#if NET8_0_OR_GREATER
            await stop.CancelAsync().ConfigureAwait(false);
#else
            stop.Cancel();
#endif
            if (readAhead is not null)
            {
                Dispose((await readAhead.ConfigureAwait(false)).Page);
            }

            stop.Dispose();
        }
    }

    /// <summary>Invokes the page-fetching call and computes the continuation from the result.</summary>
    /// <typeparam name="TPage">The page type returned by the page-fetching call.</typeparam>
    /// <typeparam name="TToken">The continuation token type.</typeparam>
    /// <param name="token">The token identifying the page to fetch.</param>
    /// <param name="fetch">Invokes the page-fetching call.</param>
    /// <param name="next">Computes the continuation from the page.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The fetched page, the end of the sequence when the call returned <see langword="null"/>, or the failure of the call; the task itself never faults.</returns>
    internal static async Task<FetchedPage<TPage, TToken>> FetchAsync<TPage, TToken>(
        TToken token,
        Func<TToken, CancellationToken, Task<TPage>> fetch,
        Func<TPage, TToken, PageContinuation<TToken>> next,
        CancellationToken cancellationToken)
    {
        TPage page;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            page = await fetch(token, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return FetchedPage<TPage, TToken>.Failed(ex);
        }

        if (page is null)
        {
            return FetchedPage<TPage, TToken>.End;
        }

        // A response that carries an error would otherwise read as an empty final page and truncate the sequence silently.
        if (page is IApiResponse { IsSuccessful: false } failed)
        {
            var error = failed.GetError();
            Dispose(page);
            return FetchedPage<TPage, TToken>.Failed(error);
        }

        try
        {
            return FetchedPage<TPage, TToken>.Of(page, next(page, token), null);
        }
        catch (Exception ex)
        {
            return FetchedPage<TPage, TToken>.Of(page, default, ex);
        }
    }

    /// <summary>Disposes a page that owns resources, such as an <see cref="ApiResponse{T}"/>.</summary>
    /// <typeparam name="TPage">The page type.</typeparam>
    /// <param name="page">The page to dispose; ignored when it is not disposable.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Dispose<TPage>(TPage? page) => (page as IDisposable)?.Dispose();
}
