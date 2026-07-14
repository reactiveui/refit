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
    PublicParameterlessConstructor = 1,

    /// <summary>All public constructors.</summary>
    PublicConstructors = 3,

    /// <summary>All non-public constructors.</summary>
    NonPublicConstructors = 4,

    /// <summary>All public methods.</summary>
    PublicMethods = 8,

    /// <summary>All non-public methods.</summary>
    NonPublicMethods = 16,

    /// <summary>All public fields.</summary>
    PublicFields = 32,

    /// <summary>All non-public fields.</summary>
    NonPublicFields = 64,

    /// <summary>All public nested types.</summary>
    PublicNestedTypes = 128,

    /// <summary>All non-public nested types.</summary>
    NonPublicNestedTypes = 256,

    /// <summary>All public properties.</summary>
    PublicProperties = 512,

    /// <summary>All non-public properties.</summary>
    NonPublicProperties = 1024,

    /// <summary>All public events.</summary>
    PublicEvents = 2048,

    /// <summary>All non-public events.</summary>
    NonPublicEvents = 4096,

    /// <summary>All interfaces implemented by the type.</summary>
    Interfaces = 8192,

    /// <summary>All members.</summary>
    All = -1,
}
#endif
