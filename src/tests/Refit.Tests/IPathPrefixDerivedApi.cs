// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>
/// A derived API with its own <see cref="PathPrefixAttribute"/>. Its prefix - not the base interface's - applies to
/// every method the client exposes, including <see cref="IPathPrefixBaseApi.Ping"/> inherited from the base; the two
/// prefixes are never concatenated.
/// </summary>
[PathPrefix("/api/v2")]
public interface IPathPrefixDerivedApi : IPathPrefixBaseApi
{
    /// <summary>A route defined directly on the derived interface.</summary>
    /// <returns>The response body.</returns>
    [Get("/own")]
    Task<string> Own();
}
