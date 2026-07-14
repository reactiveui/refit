// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>A concrete shape used to exercise the polymorphic-attribute signal.</summary>
public sealed class PolymorphicRectangle : IPolymorphicShape
{
    /// <inheritdoc/>
    public string? Label { get; set; }

    /// <summary>Gets or sets the rectangle width.</summary>
    public int Width { get; set; }
}
