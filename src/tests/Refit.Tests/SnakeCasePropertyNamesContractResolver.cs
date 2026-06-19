// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Contract resolver that renders property names in snake_case.</summary>
[RequiresUnreferencedCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
[RequiresDynamicCode("Refit's reflection-based serialization and request building are exercised by these tests.")]
public class SnakeCasePropertyNamesContractResolver
    : DeliminatorSeparatedPropertyNamesContractResolver
{
    /// <summary>Initializes a new instance of the <see cref="SnakeCasePropertyNamesContractResolver"/> class.</summary>
    public SnakeCasePropertyNamesContractResolver()
        : base('_') { }
}
