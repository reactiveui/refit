// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Refit.Tests;

/// <summary>Route object with one readable and one non-public-getter property.</summary>
[SuppressMessage(
    "Design",
    "CA1044:Properties should not be write only",
    Justification = "This fixture intentionally exposes a property that Refit must not read as a route value.")]
public sealed class RouteObjectWithUnreadableProperty
{
    /// <summary>Gets or sets the visible route value.</summary>
    public string Visible { get; set; } = string.Empty;

    /// <summary>Gets or sets a route value that cannot be read publicly.</summary>
    public string Hidden { private get; set; } = string.Empty;
}
