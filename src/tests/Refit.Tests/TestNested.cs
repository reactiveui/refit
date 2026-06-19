// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>Hosts a nested Refit interface used to verify Refit supports interfaces declared inside a containing type.</summary>
public static class TestNested
{
    /// <summary>A nested Refit interface mirroring a subset of the GitHub REST API.</summary>
    [Headers("User-Agent: Refit Integration Tests")]
    public interface INestedGitHubApi
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
        /// <returns>The organization members.</returns>
        [Get("/orgs/{orgname}/members")]
        Task<List<User>> GetOrgMembers(string orgName);

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
        /// <returns>A task that completes when the call finishes.</returns>
        [Get("/give-me-some-404-action")]
        Task NothingToSeeHere();
    }
}
