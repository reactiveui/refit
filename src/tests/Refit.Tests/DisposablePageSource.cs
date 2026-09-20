// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Refit.Tests;

/// <summary>Produces <see cref="DisposablePage"/> instances for a paged sequence and records every one it creates.</summary>
/// <param name="pageCount">The number of pages the listing spans.</param>
/// <param name="pageSize">The number of items on each page.</param>
internal sealed class DisposablePageSource(int pageCount, int pageSize)
{
    /// <summary>The pages created so far, in creation order.</summary>
    private readonly ConcurrentQueue<DisposablePage> _created = new();

    /// <summary>Gets the pages created so far, in creation order.</summary>
    internal IReadOnlyCollection<DisposablePage> Created => _created;

    /// <summary>Gets or sets a step that runs before each page is created, given the zero-based page index.</summary>
    internal Func<int, CancellationToken, Task>? BeforePage { get; set; }

    /// <summary>Builds a sequence over this source.</summary>
    /// <param name="next">Reads the continuation from a page and its index; <see langword="null"/> follows each page with the next until the last.</param>
    /// <returns>A sequence of pages and their items.</returns>
    internal PagedEnumerable<DisposablePage, PagedItem> Sequence(Func<DisposablePage, int, PageContinuation<int>>? next = null) =>
        PagedEnumerable.Create(0, FetchAsync, static page => page.Items, next ?? Next);

    /// <summary>Creates the page at an index.</summary>
    /// <param name="index">The zero-based page index.</param>
    /// <param name="cancellationToken">A token that cancels the call.</param>
    /// <returns>The page.</returns>
    private async Task<DisposablePage> FetchAsync(int index, CancellationToken cancellationToken)
    {
        if (BeforePage is { } step)
        {
            await step(index, cancellationToken).ConfigureAwait(false);
        }

        var items = new PagedItem[pageSize];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new() { Id = (index * pageSize) + i + 1 };
        }

        DisposablePage page = new(items);
        _created.Enqueue(page);
        return page;
    }

    /// <summary>Continues to the next page until the last.</summary>
    /// <param name="page">The page.</param>
    /// <param name="index">The zero-based index of the page.</param>
    /// <returns>The continuation.</returns>
    private PageContinuation<int> Next(DisposablePage page, int index) =>
        index + 1 < pageCount ? PageContinuation.To(index + 1) : default;
}
