// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Refit;

/// <summary>
/// A lazily fetched sequence of pages, enumerable as items or as the API's own page objects. Every enumeration starts
/// again from the first page and invokes the page-fetching call once per page; nothing is requested until it is enumerated.
/// </summary>
/// <typeparam name="TPage">The page type returned by the page-fetching call: the API's page DTO or an <see cref="ApiResponse{T}"/> of it.</typeparam>
/// <typeparam name="TItem">The item type carried by a page.</typeparam>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public sealed class PagedEnumerable<TPage, TItem> : IAsyncEnumerable<TItem>
{
    /// <summary>Starts an enumeration of pages given whether to read ahead and the page limit.</summary>
    private readonly Func<bool, int, IAsyncEnumerable<TPage>> _pages;

    /// <summary>Selects the items of a page.</summary>
    private readonly Func<TPage, IEnumerable<TItem>?> _items;

    /// <summary>Whether the next page is requested while the consumer still holds the current one.</summary>
    private readonly bool _prefetch;

    /// <summary>The most pages fetched by one enumeration.</summary>
    private readonly int _maxPages;

    /// <summary>Initializes a new instance of the <see cref="PagedEnumerable{TPage, TItem}"/> class.</summary>
    /// <param name="pages">Starts an enumeration of pages given whether to read ahead and the page limit.</param>
    /// <param name="items">Selects the items of a page.</param>
    /// <param name="prefetch">Whether the next page is requested while the consumer still holds the current one.</param>
    /// <param name="maxPages">The most pages fetched by one enumeration.</param>
    internal PagedEnumerable(
        Func<bool, int, IAsyncEnumerable<TPage>> pages,
        Func<TPage, IEnumerable<TItem>?> items,
        bool prefetch,
        int maxPages)
    {
        _pages = pages;
        _items = items;
        _prefetch = prefetch;
        _maxPages = maxPages;
    }

    /// <inheritdoc/>
    public override string ToString() => $"PagedEnumerable: Prefetch = {_prefetch}, MaxPages = {_maxPages}";

    /// <summary>Gets the pages as the API returned them, each disposed when the consumer moves past it.</summary>
    /// <returns>An asynchronous sequence of pages.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TPage> AsPages() => _pages(_prefetch, _maxPages);

    /// <summary>Returns a sequence that requests the next page while the consumer is still working through the current one.</summary>
    /// <returns>A new sequence with read-ahead enabled.</returns>
    /// <remarks>At most one page is ever requested ahead. When the consumer stops, that request is cancelled and its page disposed.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PagedEnumerable<TPage, TItem> WithPrefetch() => new(_pages, _items, true, _maxPages);

    /// <summary>Returns a sequence that fetches at most <paramref name="pages"/> pages, however many the API has.</summary>
    /// <param name="pages">The page limit.</param>
    /// <returns>A new sequence with the page limit applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pages"/> is not positive.</exception>
    public PagedEnumerable<TPage, TItem> WithMaxPages(int pages)
    {
        ArgumentOutOfRangeExceptionHelper.ThrowIfNegativeOrZero(pages);
        return new(_pages, _items, _prefetch, pages);
    }

    /// <summary>Gets the items as an observable that enumerates from the first page on every subscription.</summary>
    /// <returns>A cold observable; disposing a subscription cancels the request in flight and fetches no further page.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<TItem> ToObservable() => new AsyncEnumerableObservable<TItem>(this);

    /// <summary>Gets the pages as an observable that enumerates from the first page on every subscription.</summary>
    /// <returns>A cold observable whose pages are disposed once <see cref="IObserver{T}.OnNext"/> returns, so an observer must not keep one.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IObservable<TPage> ToPageObservable() => new AsyncEnumerableObservable<TPage>(AsPages());

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerator<TItem> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        EnumerateItemsAsync(default).GetAsyncEnumerator(cancellationToken);

    /// <summary>Enumerates the items of each page, keeping a page alive while its items are being consumed.</summary>
    /// <param name="cancellationToken">A token that cancels the enumeration.</param>
    /// <returns>An asynchronous sequence of items.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // async-iterator dispose-mode epilogue: the compiler-generated <>w__disposeMode false-edge cannot be exercised or removed.
    private async IAsyncEnumerable<TItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var page in AsPages().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var items = _items(page);
            if (items is null)
            {
                continue;
            }

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}
