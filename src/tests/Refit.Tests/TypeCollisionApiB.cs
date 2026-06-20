// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Factory for the <see cref="ITypeCollisionApiB"/> Refit client.</summary>
public static class TypeCollisionApiB
{
    /// <summary>Creates a Refit client for <see cref="ITypeCollisionApiB"/>.</summary>
    /// <returns>A configured <see cref="ITypeCollisionApiB"/> instance.</returns>
    public static ITypeCollisionApiB Create() => RestService.For<ITypeCollisionApiB>("http://somewhere.com");
}
