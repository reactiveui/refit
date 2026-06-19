// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Benchmarks;

/// <summary>Represents a GitHub user used in the benchmarks.</summary>
public class User
{
    /// <summary>Gets or sets the user identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the user name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Gets or sets the user biography.</summary>
    public string Bio { get; set; } = null!;

    /// <summary>Gets or sets the number of followers.</summary>
    public int Followers { get; set; }

    /// <summary>Gets or sets the number of users being followed.</summary>
    public int Following { get; set; }

    /// <summary>Gets or sets the user profile URL.</summary>
    public string Url { get; set; } = null!;
}
