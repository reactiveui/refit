// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies how a source-generated context is registered on <see cref="RefitSettings"/>.</summary>
public class RefitSettingsJsonContextTests
{
    /// <summary>Verifies new settings run on the options of the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForJsonContextUsesTheContextOptions()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        var serializer = await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
    }

    /// <summary>Verifies new settings can allow the reflection fallback.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForJsonContextWithReflectionFallbackAppendsAReflectionResolver()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default, true);

        var serializer = await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions.TypeInfoResolverChain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies adding a context keeps the settings and the serializer's naming and converters.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UseJsonContextKeepsTheSettingsAndTheSerializerOptions()
    {
        var settings = RefitSettings.SnakeCase();
        var original = (SystemTextJsonContentSerializer)settings.ContentSerializer;

        var returned = settings.UseJsonContext(InventoryJsonContext.Default);

        var composed = await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(returned).IsSameReferenceAs(settings);
        await Assert.That(composed).IsNotSameReferenceAs(original);
        await Assert.That(composed!.SerializerOptions.PropertyNamingPolicy).IsSameReferenceAs(original.SerializerOptions.PropertyNamingPolicy);
        await Assert.That(composed.SerializerOptions.TypeInfoResolverChain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
    }

    /// <summary>Verifies adding a context with the reflection fallback appends a reflection resolver.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UseJsonContextWithReflectionFallbackAppendsAReflectionResolver()
    {
        var settings = new RefitSettings().UseJsonContext(InventoryJsonContext.Default, true);

        var serializer = await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(serializer!.SerializerOptions.TypeInfoResolverChain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies settings that use another serializer refuse a JSON context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UseJsonContextRejectsSettingsThatDoNotUseSystemTextJson()
    {
        var settings = new RefitSettings(new UnsupportedContentSerializer());

        await Assert.That(() => settings.UseJsonContext(InventoryJsonContext.Default)).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies adding a context rejects a missing context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task UseJsonContextRejectsANullContext()
    {
        var settings = new RefitSettings();

        await Assert.That(() => settings.UseJsonContext(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>A content serializer that is not System.Text.Json.</summary>
    private sealed class UnsupportedContentSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => throw new NotSupportedException();

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => null;
    }
}
