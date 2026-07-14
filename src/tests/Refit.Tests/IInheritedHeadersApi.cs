// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A Refit interface inheriting static headers from a base interface.</summary>
public interface IInheritedHeadersApi : IHeaderBearingBase
{
    /// <summary>Sends a request that inherits the base interface headers.</summary>
    /// <returns>The response body.</returns>
    [Get("/h")]
    Task<string> Get();
}
