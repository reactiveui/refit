// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface that mirrors a subset of the GitHub REST API for use in the Refit tests.</summary>
[Headers("User-Agent: Refit Integration Tests")]
public interface IGitHubApi
{
    /// <summary>Gets a single user by login.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <returns>The matching user.</returns>
    [Get("/users/{username}")]
    Task<User> GetUser(string userName);

    /// <summary>Gets a single user by login as an observable stream.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <returns>An observable that yields the matching user.</returns>
    [Get("/users/{username}")]
    IObservable<User> GetUserObservable(string userName);

    /// <summary>Gets a single user using a camel-cased route template parameter.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <returns>An observable that yields the matching user.</returns>
    [Get("/users/{userName}")]
    IObservable<User> GetUserCamelCase(string userName);

    /// <summary>Gets the members of an organization.</summary>
    /// <param name="orgName">The organization name.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The organization members.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "The optional CancellationToken is the idiomatic Refit signature and the generated-code snapshot depends on this exact single method shape.")]
    [Get("/orgs/{orgname}/members")]
    Task<List<User>> GetOrgMembers(string orgName, CancellationToken cancellationToken = default);

    /// <summary>Searches for users.</summary>
    /// <param name="q">The search query.</param>
    /// <returns>The search results.</returns>
    [Get("/search/users")]
    Task<UserSearchResult> FindUsers(string q);

    /// <summary>Gets the API index response.</summary>
    /// <returns>The raw HTTP response.</returns>
    [Get("/")]
    Task<HttpResponseMessage> GetIndex();

    /// <summary>Gets the API index body as an observable string stream.</summary>
    /// <returns>An observable that yields the index body.</returns>
    [SuppressMessage(
        "Design",
        "CA1024:Use properties where appropriate",
        Justification = "Refit interface members must be methods carrying an HTTP attribute; converting to a property would break code generation and the tests referencing it by name.")]
    [Get("/")]
    IObservable<string> GetIndexObservable();

    /// <summary>Calls an endpoint that intentionally returns 404 to exercise error handling.</summary>
    /// <returns>The deserialized user, when the call unexpectedly succeeds.</returns>
    [Get("/give-me-some-404-action")]
    Task<User> NothingToSeeHere();

    /// <summary>Calls a 404 endpoint and returns the full API response metadata.</summary>
    /// <returns>The API response, including status and headers.</returns>
    [Get("/give-me-some-404-action")]
    Task<ApiResponse<User>> NothingToSeeHereWithMetadata();

    /// <summary>Gets a user and returns the full API response metadata.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The API response, including status and headers.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S2360:Optional parameters should not be used",
        Justification = "The optional CancellationToken is the idiomatic Refit signature and the generated-code snapshot depends on this exact single method shape.")]
    [Get("/users/{username}")]
    Task<ApiResponse<User>> GetUserWithMetadata(string userName, CancellationToken cancellationToken = default);

    /// <summary>Gets a user with response metadata as an observable stream.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <returns>An observable that yields the API response.</returns>
    [Get("/users/{username}")]
    IObservable<ApiResponse<User>> GetUserObservableWithMetadata(string userName);

    /// <summary>Gets a user as an observable of the <see cref="IApiResponse{T}"/> interface.</summary>
    /// <param name="userName">The user's login handle.</param>
    /// <returns>An observable that yields the API response.</returns>
    [Get("/users/{username}")]
    IObservable<IApiResponse<User>> GetUserIApiResponseObservableWithMetadata(string userName);

    /// <summary>Creates a user.</summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The created user.</returns>
    [Post("/users")]
    Task<User> CreateUser(User user);

    /// <summary>Creates a user and returns the full API response metadata.</summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The API response, including status and headers.</returns>
    [Post("/users")]
    Task<ApiResponse<User>> CreateUserWithMetadata(User user);
}
