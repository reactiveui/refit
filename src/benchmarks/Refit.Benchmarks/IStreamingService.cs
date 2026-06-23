// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Benchmarks;

/// <summary>Service used to benchmark streaming versus buffered response handling.</summary>
public interface IStreamingService
{
    /// <summary>Streams the users of a JSON array response as they are read.</summary>
    /// <returns>The streamed users.</returns>
    [Get("/items")]
    IAsyncEnumerable<User> StreamItemsAsync();

    /// <summary>Buffers the whole JSON array response into a list.</summary>
    /// <returns>The deserialized users.</returns>
    [Get("/items")]
    Task<List<User>> GetItemsAsync();
}
