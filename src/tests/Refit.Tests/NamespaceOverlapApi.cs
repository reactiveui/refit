// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Factory for the <see cref="INamespaceOverlapApi"/> Refit client.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public static class NamespaceOverlapApi
{
    /// <summary>Creates a Refit client for <see cref="INamespaceOverlapApi"/>.</summary>
    /// <returns>A configured <see cref="INamespaceOverlapApi"/> instance.</returns>
    public static INamespaceOverlapApi Create()
    {
        return RestService.For<INamespaceOverlapApi>("http://somewhere.com");
    }
}
