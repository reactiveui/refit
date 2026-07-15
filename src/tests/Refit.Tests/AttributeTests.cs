// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Refit.Tests;

/// <summary>Tests for simple public attribute types.</summary>
public class AttributeTests
{
    /// <summary>The headers expected from the populated headers-attribute fixture.</summary>
    private static readonly string[] ExpectedHeaders = ["Accept: application/json", "X-Trace: 1"];

    /// <summary>Verifies body attribute constructors preserve serialization method and buffering values.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task BodyAttributeConstructorsSetExpectedProperties()
    {
        var defaultAttribute = new BodyAttribute();
        var bufferedAttribute = new BodyAttribute(true);
        var methodAttribute = new BodyAttribute(BodySerializationMethod.UrlEncoded);
        var methodAndBufferedAttribute = new BodyAttribute(BodySerializationMethod.Serialized, false);

        await Assert.That(defaultAttribute.SerializationMethod).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(defaultAttribute.Buffered).IsNull();
        await Assert.That(bufferedAttribute.SerializationMethod).IsEqualTo(BodySerializationMethod.Default);
        await Assert.That(bufferedAttribute.Buffered).IsTrue();
        await Assert.That(methodAttribute.SerializationMethod).IsEqualTo(BodySerializationMethod.UrlEncoded);
        await Assert.That(methodAttribute.Buffered).IsNull();
        await Assert.That(methodAndBufferedAttribute.SerializationMethod).IsEqualTo(BodySerializationMethod.Serialized);
        await Assert.That(methodAndBufferedAttribute.Buffered).IsFalse();
    }

    /// <summary>Verifies the legacy attachment-name attribute stores the supplied name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AttachmentNameAttributeStoresName()
    {
#pragma warning disable CS0618 // Public API retained for compatibility and covered intentionally.
        var attribute = new AttachmentNameAttribute("payload");
#pragma warning restore CS0618

        await Assert.That(attribute.Name).IsEqualTo("payload");
    }

    /// <summary>Verifies the HEAD method attribute exposes the HEAD verb and its supplied path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeadAttributeExposesHeadMethodAndPath()
    {
        var attribute = new HeadAttribute("/resource");

        await Assert.That(attribute.Method).IsEqualTo(System.Net.Http.HttpMethod.Head);
        await Assert.That(attribute.Path).IsEqualTo("/resource");
    }

    /// <summary>Verifies the headers attribute stores supplied headers and substitutes an empty array for null.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HeadersAttributeStoresHeadersAndDefaultsNullToEmpty()
    {
        var withHeaders = new HeadersAttribute("Accept: application/json", "X-Trace: 1");
        var withNull = new HeadersAttribute(null!);

        await Assert.That(withHeaders.Headers).IsEquivalentTo(ExpectedHeaders);
        await Assert.That(withNull.Headers).IsEmpty();
    }
}
