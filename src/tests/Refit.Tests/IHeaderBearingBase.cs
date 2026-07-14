// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A base interface carrying static headers, including a blank entry the parser must ignore.</summary>
[Headers("", "X-Base: base")]
public interface IHeaderBearingBase
{
    /// <summary>Sends a request declared on the header-bearing base interface.</summary>
    /// <returns>The response body.</returns>
    [Get("/base")]
    Task<string> BaseGet();
}
