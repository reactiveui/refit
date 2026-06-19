// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture with multiple header collections, used to verify Refit rejects it.</summary>
public interface IManyHeaderCollections
{
    /// <summary>Endpoint declaring two header collections.</summary>
    /// <param name="collection0">First header collection.</param>
    /// <param name="collection1">Second header collection.</param>
    /// <returns>The response body.</returns>
    [Get("/")]
    Task<string> GetValue(
        [HeaderCollection] IDictionary<string, string> collection0,
        [HeaderCollection] IDictionary<string, string> collection1);
}
