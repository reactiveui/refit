// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Reflection.Tests;

/// <summary>An API declaring an <c>Accept</c> media type whose parameter separator carries no space, once on its own and
/// once beside a second header, so the number of declared headers is the only difference between the two calls.</summary>
public interface IMultipleStaticHeaderApi
{
    /// <summary>Declares the raw media type as the method's only header.</summary>
    /// <returns>The response body.</returns>
    [Get("/report")]
    [Headers("Accept: application/vnd.api.json;version=3.4.1")]
    Task<string> OneHeader();

    /// <summary>Declares the same raw media type alongside a second header.</summary>
    /// <returns>The response body.</returns>
    [Get("/report")]
    [Headers("Accept: application/vnd.api.json;version=3.4.1", "X-Trace: abc")]
    Task<string> TwoHeaders();
}
