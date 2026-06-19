// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using CollisionA;

namespace Refit.Tests;

/// <summary>Refit API used to verify type-name collisions across namespaces (variant A).</summary>
public interface ITypeCollisionApiA
{
    /// <summary>Sends a request returning the collision type from namespace A.</summary>
    /// <returns>A task that resolves to the namespace-A collision type.</returns>
    [Get("")]
    Task<SomeType> SomeARequest();
}
