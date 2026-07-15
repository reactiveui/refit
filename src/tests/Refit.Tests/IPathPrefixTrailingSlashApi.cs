// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose <see cref="PathPrefixAttribute"/> ends with a slash, which the join must not double up.</summary>
[PathPrefix("/api/v2/")]
public interface IPathPrefixTrailingSlashApi
{
    /// <summary>A route joined to a trailing-slash prefix without producing a double slash.</summary>
    /// <returns>The response body.</returns>
    [Get("/users")]
    Task<string> GetUsers();
}
