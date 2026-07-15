// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>An API whose empty <see cref="PathPrefixAttribute"/> is a no-op, leaving each route unchanged.</summary>
[PathPrefix("")]
public interface IPathPrefixEmptyApi
{
    /// <summary>A route left unchanged by the empty prefix.</summary>
    /// <returns>The response body.</returns>
    [Get("/users")]
    Task<string> GetUsers();
}
