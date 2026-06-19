// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Refit.Tests;

/// <summary>A test enum exercising the EnumMember attribute.</summary>
public enum FooWithEnumMember
{
    /// <summary>The A value.</summary>
    A,

    /// <summary>The B value, serialized as "b".</summary>
    [EnumMember(Value = "b")]
    B
}
