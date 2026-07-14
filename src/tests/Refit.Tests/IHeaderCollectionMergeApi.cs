// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Refit API surface exercising header-collection merge behavior.</summary>
public interface IHeaderCollectionMergeApi
{
    /// <summary>Posts using only a dynamic header collection as its header source.</summary>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>The response body.</returns>
    [Post("/merge")]
    Task<string> PostWithOnlyHeaderCollection(
        [HeaderCollection] IDictionary<string, string> headers);

    /// <summary>Posts using a static header alongside a dynamic header collection.</summary>
    /// <param name="headers">The dynamic header collection.</param>
    /// <returns>The response body.</returns>
    [Headers("X-Static: static-value")]
    [Post("/merge")]
    Task<string> PostWithStaticHeaderAndCollection(
        [HeaderCollection] IDictionary<string, string> headers);
}
