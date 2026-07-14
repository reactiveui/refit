// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>
/// Verifies that <see cref="SystemTextJsonContentSerializer"/> keeps the declared interface type when serializing a
/// value whose declared type is configured for polymorphic serialization, instead of switching to the runtime type.
/// The declared type must be an interface or abstract type for the serializer to consult its polymorphism signals.
/// </summary>
public sealed class PolymorphicContentSerializationTests
{
    /// <summary>The rectangle width serialized by the polymorphic-attribute fixture.</summary>
    private const int RectangleWidth = 5;

    /// <summary>The circle radius serialized by the derived-type-attribute fixture.</summary>
    private const int CircleRadius = 3;

    /// <summary>Verifies a declared interface annotated with <see cref="JsonPolymorphicAttribute"/> is serialized polymorphically.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonPolymorphicAttributeKeepsDeclaredType()
    {
        var serializer = new SystemTextJsonContentSerializer();

        using var content = serializer.ToHttpContent<IPolymorphicShape>(new PolymorphicRectangle { Width = RectangleWidth });
        var body = await content.ReadAsStringAsync();

        await Assert.That(body).Contains("\"kind\":\"rectangle\"");
        await Assert.That(body).Contains("\"width\":5");
    }

    /// <summary>Verifies a declared interface carrying only <see cref="JsonDerivedTypeAttribute"/> is serialized polymorphically.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task JsonDerivedTypeAttributeWithoutPolymorphicKeepsDeclaredType()
    {
        var serializer = new SystemTextJsonContentSerializer();

        using var content = serializer.ToHttpContent<IDiscriminatedShape>(new DiscriminatedCircle { Radius = CircleRadius });
        var body = await content.ReadAsStringAsync();

        await Assert.That(body).Contains("\"$type\":\"circle\"");
        await Assert.That(body).Contains("\"radius\":3");
    }
}
