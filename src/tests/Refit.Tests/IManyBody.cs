// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with multiple body parameters, used to verify Refit rejects it.</summary>
public interface IManyBody
{
    /// <summary>Endpoint declaring two body parameters.</summary>
    /// <param name="body0">First body content.</param>
    /// <param name="body1">Second body content.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetValue([Body] string body0, [Body] string body1);
}
