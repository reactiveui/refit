// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
namespace Refit.Generator;

/// <summary>Describes the nullable reference type context of an interface.</summary>
internal enum Nullability
{
    /// <summary>The nullable reference type context is enabled.</summary>
    Enabled,

    /// <summary>The nullable reference type context is disabled.</summary>
    Disabled,

    /// <summary>No nullable reference type context is specified.</summary>
    None
}
