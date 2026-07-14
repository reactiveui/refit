// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>A polymorphic interface annotated with <see cref="JsonPolymorphicAttribute"/> and a custom discriminator name.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PolymorphicRectangle), "rectangle")]
public interface IPolymorphicShape
{
    /// <summary>Gets or sets the shape label shared by every derived shape.</summary>
    string? Label { get; set; }
}
