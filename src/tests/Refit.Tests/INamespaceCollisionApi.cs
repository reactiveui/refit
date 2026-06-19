// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using SomeType = CollisionA.SomeType;

namespace Refit.Tests;

/// <summary>Refit fixture interface whose response type collides by name across namespaces.</summary>
public interface INamespaceCollisionApi
{
    /// <summary>Performs a GET request returning the collision response type.</summary>
    /// <returns>The collision response type.</returns>
    [Get("/")]
    Task<SomeType> SomeRequest();
}
