// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A GitHub user model used as a deserialization fixture in the Refit tests.</summary>
public sealed record User
{
    /// <summary>Gets or sets the user's login handle.</summary>
    public string? Login { get; init; }

    /// <summary>Gets or sets the user's numeric identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets or sets the URL of the user's avatar image.</summary>
    public string? AvatarUrl { get; init; }

    /// <summary>Gets or sets the user's Gravatar identifier.</summary>
    public string? GravatarId { get; init; }

    /// <summary>Gets or sets the API URL for the user.</summary>
    public string? Url { get; init; }

    /// <summary>Gets or sets the HTML profile URL for the user.</summary>
    public string? HtmlUrl { get; init; }

    /// <summary>Gets or sets the API URL listing the user's followers.</summary>
    public string? FollowersUrl { get; init; }

    /// <summary>Gets or sets the API URL listing who the user follows.</summary>
    public string? FollowingUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's gists.</summary>
    public string? GistsUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's starred repositories.</summary>
    public string? StarredUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's subscriptions.</summary>
    public string? SubscriptionsUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's organizations.</summary>
    public string? OrganizationsUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's repositories.</summary>
    public string? ReposUrl { get; init; }

    /// <summary>Gets or sets the API URL for the user's events.</summary>
    public string? EventsUrl { get; init; }

    /// <summary>Gets or sets the API URL for events received by the user.</summary>
    public string? ReceivedEventsUrl { get; init; }

    /// <summary>Gets or sets the account type (for example, User or Organization).</summary>
    public string? Type { get; init; }

    /// <summary>Gets or sets the user's display name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets or sets the user's company.</summary>
    public string? Company { get; init; }

    /// <summary>Gets or sets the user's blog URL.</summary>
    public string? Blog { get; init; }

    /// <summary>Gets or sets the user's location.</summary>
    public string? Location { get; init; }

    /// <summary>Gets or sets the user's email address.</summary>
    public string? Email { get; init; }

    /// <summary>Gets or sets a value indicating whether the user is available for hire.</summary>
    public bool? Hireable { get; init; }

    /// <summary>Gets or sets the user's biography.</summary>
    public string? Bio { get; init; }

    /// <summary>Gets or sets the count of the user's public repositories.</summary>
    public int PublicRepos { get; init; }

    /// <summary>Gets or sets the number of followers the user has.</summary>
    public int Followers { get; init; }

    /// <summary>Gets or sets the number of accounts the user follows.</summary>
    public int Following { get; init; }

    /// <summary>Gets or sets the account creation timestamp.</summary>
    public string? CreatedAt { get; init; }

    /// <summary>Gets or sets the account last-updated timestamp.</summary>
    public string? UpdatedAt { get; init; }

    /// <summary>Gets or sets the count of the user's public gists.</summary>
    public int PublicGists { get; init; }
}
