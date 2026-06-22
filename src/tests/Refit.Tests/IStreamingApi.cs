// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Refit.Tests;

/// <summary>Fixture exercising <see cref="IAsyncEnumerable{T}"/> streaming responses.</summary>
public interface IStreamingApi
{
    /// <summary>Streams the elements of a JSON array response.</summary>
    /// <returns>The streamed items.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Refit endpoint method fixture; must remain a method to exercise the generator.")]
    [Get("/array")]
    IAsyncEnumerable<StreamItem> GetArray();

    /// <summary>Streams the elements of a newline-delimited JSON response.</summary>
    /// <returns>The streamed items.</returns>
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Refit endpoint method fixture; must remain a method to exercise the generator.")]
    [Get("/lines")]
    IAsyncEnumerable<StreamItem> GetLines();

    /// <summary>Streams a JSON array response, observing the supplied cancellation token.</summary>
    /// <param name="cancellationToken">A token to cancel streaming.</param>
    /// <returns>The streamed items.</returns>
    [Get("/array")]
    IAsyncEnumerable<StreamItem> GetArrayCancellable(CancellationToken cancellationToken);

    /// <summary>Streams a JSON array from a dynamic route, exercising the reflection (non-inline) path.</summary>
    /// <param name="group">The route segment value.</param>
    /// <returns>The streamed items.</returns>
    [Get("/groups/{group}/array")]
    IAsyncEnumerable<StreamItem> GetGroupArray(string group);
}
