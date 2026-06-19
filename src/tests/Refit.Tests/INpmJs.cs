// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Refit.Tests;

/// <summary>A Refit interface targeting the npmjs registry, used by the Refit integration tests.</summary>
[Headers("User-Agent: Refit Integration Tests")]
public interface INpmJs
{
    /// <summary>Gets the congruence registry document.</summary>
    /// <returns>The registry document.</returns>
    [Get("/congruence")]
    Task<RootObject> GetCongruence();
}
