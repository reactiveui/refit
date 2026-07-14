// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with a non-string round-tripping parameter formatted via <c>ToString</c>, separators preserved.</summary>
public interface IRoundTripNotString
{
    /// <summary>Endpoint with a round-tripping parameter that is not a string.</summary>
    /// <param name="value">The round-tripping value.</param>
    /// <returns>The response body.</returns>
    [Get("/repos/{**value}/contents")]
    Task<string> GetValue(RepoPath value);
}
