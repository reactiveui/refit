// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with an invalid parameter substitution token, used to verify Refit rejects it.</summary>
public interface IInvalidParamSubstitution
{
    /// <summary>Endpoint with an invalid substitution token in its URL.</summary>
    /// <param name="path">The path value.</param>
    /// <returns>The response body.</returns>
    [Get("/{/path}")]
    Task<string> GetValue(string path);
}
