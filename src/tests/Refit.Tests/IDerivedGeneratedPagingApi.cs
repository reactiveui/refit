// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface that inherits the paged methods of <see cref="IGeneratedPagingApi"/>.</summary>
internal interface IDerivedGeneratedPagingApi : IGeneratedPagingApi
{
    /// <summary>Gets the first page of the cursor listing as a plain call.</summary>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>The page.</returns>
    [Get("/cursor/items")]
    Task<CursorPage> GetFirstPageAsync(CancellationToken cancellationToken);
}
