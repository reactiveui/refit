// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Fixture whose round-tripping parameter is nullable, used to verify ignores it.</summary>
public interface IRoundTrippingNullString
{
    /// <summary>Endpoint with a round-tripping parameter name followed by whitespace.</summary>
    /// <param name="path">The nullable path value.</param>
    /// <returns>The response body.</returns>
    [Get("/{**path}")]
    Task<string> GetValue(string? path);
}
