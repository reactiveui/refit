// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Refit;

namespace CollisionA;

/// <summary>Refit fixture interface in the CollisionA namespace used to test name collisions.</summary>
public interface INamespaceCollisionApi
{
    /// <summary>Performs a GET request returning the CollisionA response type.</summary>
    /// <returns>The CollisionA response type.</returns>
    [Get("/")]
    Task<SomeType> SomeRequest();
}
