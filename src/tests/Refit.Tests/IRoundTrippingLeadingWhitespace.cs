// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture whose round-tripping parameter name has leading whitespace, used to verify Refit rejects it.</summary>
public interface IRoundTrippingLeadingWhitespace
{
    /// <summary>Endpoint with a round-tripping parameter name preceded by whitespace.</summary>
    /// <param name="path">The path value.</param>
    /// <returns>The response body.</returns>
    [Get("/{ **path}")]
    Task<string> GetValue(string path);
}
