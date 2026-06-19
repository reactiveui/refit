// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Refit.Generator;

/// <summary>The set of well-known constraints that can be applied to a generic type parameter.</summary>
[Flags]
[SuppressMessage(
    "Minor Code Smell",
    "S2342:Enumeration types should comply with a naming convention",
    Justification = "Name retained; renaming the type to a plural would require changes to Emitter.cs and Parser.cs which are outside the scope of this change.")]
internal enum KnownTypeConstraint
{
    /// <summary>No well-known constraint.</summary>
    None = 0,

    /// <summary>The reference type (<c>class</c>) constraint.</summary>
    Class = 1 << 0,

    /// <summary>The <c>unmanaged</c> constraint.</summary>
    Unmanaged = 1 << 1,

    /// <summary>The value type (<c>struct</c>) constraint.</summary>
    Struct = 1 << 2,

    /// <summary>The <c>notnull</c> constraint.</summary>
    NotNull = 1 << 3,

    /// <summary>The parameterless constructor (<c>new()</c>) constraint.</summary>
    New = 1 << 4
}
