// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A base API declaring its own <see cref="PathPrefixAttribute"/>, applied when it is itself the client type.</summary>
[PathPrefix("/root")]
public interface IPathPrefixBaseApi
{
    /// <summary>A route defined on the base interface.</summary>
    /// <returns>The response body.</returns>
    [Get("/ping")]
    Task<string> Ping();
}
