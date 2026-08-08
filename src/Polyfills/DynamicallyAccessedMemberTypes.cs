// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if !NET
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies the member types that are dynamically accessed and must be preserved.</summary>
[Flags]
[SuppressMessage(
    "Design",
    "SST2303:An enum marked [Flags] has members that are not distinct bit values",
    Justification = "Mirrors the BCL flags enum; several members intentionally combine bits (e.g. PublicConstructors = 1 | 2).")]
internal enum DynamicallyAccessedMemberTypes
{
    /// <summary>No members are dynamically accessed.</summary>
    None = 0,

    /// <summary>The public parameterless constructor.</summary>
    PublicParameterlessConstructor = 1 << 0,

    /// <summary>All public constructors.</summary>
    PublicConstructors = 3,

    /// <summary>All non-public constructors.</summary>
    NonPublicConstructors = 1 << 2,

    /// <summary>All public methods.</summary>
    PublicMethods = 1 << 3,

    /// <summary>All non-public methods.</summary>
    NonPublicMethods = 1 << 4,

    /// <summary>All public fields.</summary>
    PublicFields = 1 << 5,

    /// <summary>All non-public fields.</summary>
    NonPublicFields = 1 << 6,

    /// <summary>All public nested types.</summary>
    PublicNestedTypes = 1 << 7,

    /// <summary>All non-public nested types.</summary>
    NonPublicNestedTypes = 1 << 8,

    /// <summary>All public properties.</summary>
    PublicProperties = 1 << 9,

    /// <summary>All non-public properties.</summary>
    NonPublicProperties = 1 << 10,

    /// <summary>All public events.</summary>
    PublicEvents = 1 << 11,

    /// <summary>All non-public events.</summary>
    NonPublicEvents = 1 << 12,

    /// <summary>All interfaces implemented by the type.</summary>
    Interfaces = 1 << 13,

    /// <summary>All members.</summary>
    All = -1,
}
#endif
