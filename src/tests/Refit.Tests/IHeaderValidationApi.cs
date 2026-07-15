// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A minimal Refit interface whose single method carries a strictly-parsed header, used by the header-validation tests.</summary>
public interface IHeaderValidationApi
{
    /// <summary>Issues a GET whose <c>If-Modified-Since</c> header is supplied dynamically.</summary>
    /// <param name="value">The header value applied to the request.</param>
    /// <returns>A task whose result is the fetched string.</returns>
    [Get("/foo")]
    Task<string> Get([Header("If-Modified-Since")] string value);
}
