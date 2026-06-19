// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Fixture whose URL references a parameter with no matching method argument, used to verify Refit rejects it.</summary>
public interface IUrlNoMatchingParameters
{
    /// <summary>Endpoint whose URL token has no corresponding method parameter.</summary>
    /// <returns>The response body.</returns>
    [Get("/{value}")]
    Task<string> GetValue();
}
