// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A page that owns a resource and records whether it has been released.</summary>
internal sealed class DisposablePage : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="DisposablePage"/> class.</summary>
    /// <param name="items">The items on the page.</param>
    internal DisposablePage(IReadOnlyList<PagedItem> items) => Items = items;

    /// <summary>Gets the items on the page.</summary>
    internal IReadOnlyList<PagedItem> Items { get; }

    /// <summary>Gets a value indicating whether the page was disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public void Dispose() => IsDisposed = true;
}
