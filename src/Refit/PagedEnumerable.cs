// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>
/// Wraps an ordinary Refit method that returns one page into a lazy sequence of pages or items. The page type stays the
/// API's own DTO; the caller supplies the selectors that read items and the continuation from it.
/// </summary>
/// <example>
/// <code>
/// var items = PagedEnumerable.FromCursor(api.GetItemsAsync, page => page.Items, page => page.NextCursor);
/// await foreach (var item in items) { Use(item); }
/// </code>
/// </example>
public static class PagedEnumerable
{
    /// <summary>Creates a sequence from a page-fetching call and selectors, for any token shape such as page numbers, keyset values or composite tokens.</summary>
    /// <typeparam name="TPage">The page type returned by <paramref name="fetch"/>: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <typeparam name="TToken">The continuation token type, such as a cursor string or an offset.</typeparam>
    /// <param name="fetch">
    /// Calls the Refit method for the page a token identifies, passing on the supplied cancellation token. It is called
    /// once per page, so every page is a fresh request. The first page is requested with <c>default(TToken)</c>. Declare
    /// the token parameter's type on the lambda so <typeparamref name="TToken"/> is inferred.
    /// </param>
    /// <param name="items">Reads the items from a page; a <see langword="null"/> result is an empty page.</param>
    /// <param name="next">Reads the continuation from a page and the token that fetched it.</param>
    /// <returns>A sequence that requests nothing until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException">A delegate argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// A page that is <see langword="null"/> ends the sequence. A page that is an <see cref="IApiResponse"/> and was not
    /// successful throws its error rather than ending the sequence. A page that is <see cref="IDisposable"/>, such as an
    /// <see cref="ApiResponse{T}"/>, is disposed once the consumer moves past it or stops.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PagedEnumerable<TPage, TItem> Create<TPage, TItem, TToken>(
        Func<TToken, CancellationToken, Task<TPage>> fetch,
        Func<TPage, IEnumerable<TItem>?> items,
        Func<TPage, TToken, PageContinuation<TToken>> next) =>
        Create(default!, fetch, items, next);

    /// <summary>Creates a sequence that starts from a given token, for any token shape such as page numbers, keyset values or composite tokens.</summary>
    /// <typeparam name="TPage">The page type returned by <paramref name="fetch"/>: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <typeparam name="TToken">The continuation token type, such as a cursor string or an offset.</typeparam>
    /// <param name="first">The token for the first page, such as a saved cursor or a starting offset.</param>
    /// <param name="fetch">Calls the Refit method for the page a token identifies, passing on the supplied cancellation token. It is called once per page.</param>
    /// <param name="items">Reads the items from a page; a <see langword="null"/> result is an empty page.</param>
    /// <param name="next">Reads the continuation from a page and the token that fetched it.</param>
    /// <returns>A sequence that requests nothing until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException">A delegate argument is <see langword="null"/>.</exception>
    public static PagedEnumerable<TPage, TItem> Create<TPage, TItem, TToken>(
        TToken first,
        Func<TToken, CancellationToken, Task<TPage>> fetch,
        Func<TPage, IEnumerable<TItem>?> items,
        Func<TPage, TToken, PageContinuation<TToken>> next)
    {
        ArgumentExceptionHelper.ThrowIfNull(fetch);
        ArgumentExceptionHelper.ThrowIfNull(items);
        ArgumentExceptionHelper.ThrowIfNull(next);

        return new(
            (prefetch, maxPages) => PagePump.RunAsync(first, fetch, next, prefetch, maxPages, default),
            items,
            false,
            int.MaxValue);
    }

    /// <summary>Creates a sequence for an API that returns an opaque cursor naming the next page.</summary>
    /// <typeparam name="TPage">The page type returned by <paramref name="fetch"/>: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <param name="fetch">
    /// Calls the Refit method for the page a cursor names, passing on the supplied cancellation token; the first page is
    /// requested with a <see langword="null"/> cursor. It is called once per page.
    /// </param>
    /// <param name="items">Reads the items from a page; a <see langword="null"/> result is an empty page.</param>
    /// <param name="nextCursor">Reads the cursor of the following page from a page; <see langword="null"/> or an empty string ends the sequence.</param>
    /// <returns>A sequence that requests nothing until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException">A delegate argument is <see langword="null"/>.</exception>
    public static PagedEnumerable<TPage, TItem> FromCursor<TPage, TItem>(
        Func<string?, CancellationToken, Task<TPage>> fetch,
        Func<TPage, IEnumerable<TItem>?> items,
        Func<TPage, string?> nextCursor)
    {
        ArgumentExceptionHelper.ThrowIfNull(nextCursor);
        return Create(fetch, items, (page, _) => PageContinuation.To(nextCursor(page)));
    }

    /// <summary>Creates a sequence for an API addressed by the index of the first item, starting at offset zero.</summary>
    /// <typeparam name="TPage">The page type returned by <paramref name="fetch"/>: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <param name="fetch">Calls the Refit method for the page starting at an offset, passing on the supplied cancellation token. It is called once per page.</param>
    /// <param name="items">Reads the items from a page; a <see langword="null"/> result is an empty page.</param>
    /// <param name="nextOffset">Reads the offset of the following page from a page and the offset that fetched it; <see langword="null"/> ends the sequence.</param>
    /// <returns>A sequence that requests nothing until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException">A delegate argument is <see langword="null"/>.</exception>
    public static PagedEnumerable<TPage, TItem> FromOffset<TPage, TItem>(
        Func<int, CancellationToken, Task<TPage>> fetch,
        Func<TPage, IEnumerable<TItem>?> items,
        Func<TPage, int, int?> nextOffset)
    {
        ArgumentExceptionHelper.ThrowIfNull(nextOffset);
        return Create(fetch, items, (page, offset) => nextOffset(page, offset) is { } next ? PageContinuation.To(next) : default);
    }

    /// <summary>Creates a sequence from a page-fetching call and a selector for the server's next-page link.</summary>
    /// <typeparam name="TPage">The page type returned by <paramref name="fetch"/>: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
    /// <typeparam name="TItem">The item type carried by a page.</typeparam>
    /// <param name="originPolicy">Which origins a next link may point at; a link it refuses is never requested.</param>
    /// <param name="fetch">
    /// Calls the Refit method, typically one taking a <see cref="UrlAttribute"/> parameter, for a link the policy
    /// allowed, or with <see langword="null"/> for the first page. It is called once per page.
    /// </param>
    /// <param name="items">Reads the items from a page; a <see langword="null"/> result is an empty page.</param>
    /// <param name="nextLink">Reads the next-page link from a page; <see langword="null"/> ends the sequence.</param>
    /// <returns>A sequence that requests nothing until it is enumerated.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// A relative link resolves against the request URI of a page that is an <see cref="IApiResponse"/>, otherwise
    /// against the previous link, otherwise against the first origin the policy allows. A refused link surfaces as an
    /// <see cref="InvalidOperationException"/> once the consumer has finished with the page that carried it.
    /// </remarks>
    public static PagedEnumerable<TPage, TItem> FromLinks<TPage, TItem>(
        NextLinkOriginPolicy originPolicy,
        Func<Uri?, CancellationToken, Task<TPage>> fetch,
        Func<TPage, IEnumerable<TItem>?> items,
        Func<TPage, Uri?> nextLink)
    {
        ArgumentExceptionHelper.ThrowIfNull(originPolicy);
        ArgumentExceptionHelper.ThrowIfNull(nextLink);

        return Create(
            fetch,
            items,
            (page, current) =>
            {
                var link = nextLink(page);
                if (link is null)
                {
                    return PageContinuation<Uri?>.End;
                }

                var requestUri = (page as IApiResponse)?.RequestMessage?.RequestUri;
                var referrer = requestUri is { IsAbsoluteUri: true } ? requestUri : current;
                return PageContinuation.To<Uri?>(originPolicy.Resolve(link, referrer));
            });
    }
}
