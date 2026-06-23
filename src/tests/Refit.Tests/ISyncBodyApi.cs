// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Fixture used to verify synchronous request-body serialization.</summary>
public interface ISyncBodyApi
{
    /// <summary>Posts a body through the inline generated request path.</summary>
    /// <param name="item">The body to post.</param>
    /// <returns>A task representing the request.</returns>
    [Post("/items")]
    Task PostItem([Body] StreamItem item);

    /// <summary>Posts a body through the reflection request path (generic methods are not generated inline).</summary>
    /// <typeparam name="T">The body type.</typeparam>
    /// <param name="item">The body to post.</param>
    /// <returns>A task representing the request.</returns>
    [Post("/items")]
    Task PostItemReflected<T>([Body] T item);
}
