// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Tests;

/// <summary>Factory for the <see cref="INamespaceOverlapApi"/> Refit client.</summary>
public static class NamespaceOverlapApi
{
    /// <summary>Creates a Refit client for <see cref="INamespaceOverlapApi"/>.</summary>
    /// <returns>A configured <see cref="INamespaceOverlapApi"/> instance.</returns>
    public static INamespaceOverlapApi Create() => RestService.For<INamespaceOverlapApi>("http://somewhere.com");
}
