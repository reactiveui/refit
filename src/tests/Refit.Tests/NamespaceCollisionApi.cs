// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Factory helper for the namespace-collision Refit fixture interface.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public static class NamespaceCollisionApi
{
    /// <summary>Creates a Refit implementation of <see cref="INamespaceCollisionApi"/>.</summary>
    /// <returns>A Refit-backed <see cref="INamespaceCollisionApi"/> instance.</returns>
    public static INamespaceCollisionApi Create() => RestService.For<INamespaceCollisionApi>("http://somewhere.com");
}
