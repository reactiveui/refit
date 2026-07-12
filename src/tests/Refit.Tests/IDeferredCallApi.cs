// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface whose method surfaces the custom <see cref="DeferredCall{T}"/> return shape.</summary>
public interface IDeferredCallApi
{
    /// <summary>Gets a user by id as a deferred call.</summary>
    /// <param name="id">The user id.</param>
    /// <returns>A deferred call producing the user.</returns>
    [Get("/users/{id}")]
    DeferredCall<AdapterUser> GetUser(int id);
}
