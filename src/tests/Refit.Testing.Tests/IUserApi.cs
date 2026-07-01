// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Testing.Tests;

/// <summary>A minimal Refit interface used to exercise the route-table features end to end.</summary>
public interface IUserApi
{
    /// <summary>Gets a user by identifier.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The user.</returns>
    [Get("/users/{id}")]
    Task<User> GetUser(int id);

    /// <summary>Creates a user.</summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The created user.</returns>
    [Post("/users")]
    Task<User> CreateUser([Body] NewUser user);

    /// <summary>Creates a user, returning the raw response.</summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The HTTP response.</returns>
    [Post("/users")]
    Task<HttpResponseMessage> CreateUserResponse([Body] NewUser user);
}
