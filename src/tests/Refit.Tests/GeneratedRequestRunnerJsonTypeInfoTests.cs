// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies how <see cref="GeneratedRequestRunner"/> carries explicit <see cref="JsonTypeInfo{T}"/> metadata for a request.</summary>
public class GeneratedRequestRunnerJsonTypeInfoTests
{
    /// <summary>The JSON the inventory context writes for <see cref="Item"/>.</summary>
    private const string ItemJson = "{\"id\":1,\"displayName\":\"Bolt\",\"onHand\":4}";

    /// <summary>The value of <c>BodySerializationMethod.Json</c>, the obsolete member the generator never names.</summary>
    private const int ObsoleteJsonMethodValue = 1;

    /// <summary>A string body written by the tests.</summary>
    private const string PlainText = "plain";

    /// <summary>The stock line written by the tests.</summary>
    private static readonly InventoryItem Item = new(1, "Bolt", 4);

    /// <summary>Gets the metadata for <see cref="InventoryItem"/>.</summary>
    private static JsonTypeInfo<InventoryItem> ItemInfo => InventoryJsonContext.Default.InventoryItem;

    /// <summary>Verifies a body is written with the supplied metadata even when the serializer's own options would name it differently.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentWritesWithTheSuppliedMetadata()
    {
        var settings = RefitSettings.SnakeCase();

        using var content = GeneratedRequestRunner.CreateBodyContent(settings, Item, ItemInfo, BodySerializationMethod.Default, false);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a body falls back to the serializer's own lookup when no metadata is supplied.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentWithoutMetadataUsesTheSerializersLookup()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        using var content = GeneratedRequestRunner.CreateBodyContent(settings, Item, null, BodySerializationMethod.Default, false);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies the body kinds Refit never serializes bypass the metadata.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentKeepsTheBodyKindsRefitDoesNotSerialize()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);
        var stringInfo = InventoryJsonContext.Default.String;
        using var ready = new StringContent("ready");
        await using var stream = new MemoryStream("bytes"u8.ToArray());

        var httpContent = GeneratedRequestRunner.CreateBodyContent<HttpContent>(settings, ready, null, BodySerializationMethod.Default, false);
        using var streamContent = GeneratedRequestRunner.CreateBodyContent<Stream>(settings, stream, null, BodySerializationMethod.Default, false);
        using var stringContent = GeneratedRequestRunner.CreateBodyContent(settings, PlainText, stringInfo, BodySerializationMethod.Default, false);

        await Assert.That(httpContent).IsSameReferenceAs(ready);
        await Assert.That(streamContent).IsTypeOf<StreamContent>();
        await Assert.That(await stringContent.ReadAsStringAsync()).IsEqualTo(PlainText);
    }

    /// <summary>Verifies a string body is serialized when the method asks for serialization.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentSerializesAStringWhenTheMethodAsksForIt()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        using var content = GeneratedRequestRunner.CreateBodyContent(
            settings,
            PlainText,
            InventoryJsonContext.Default.String,
            BodySerializationMethod.Serialized,
            false);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo("\"plain\"");
    }

    /// <summary>Verifies the obsolete JSON method value is still a serialized body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentAcceptsTheObsoleteJsonMethodValue()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        using var content = GeneratedRequestRunner.CreateBodyContent(
            settings,
            Item,
            ItemInfo,
            (BodySerializationMethod)ObsoleteJsonMethodValue,
            false);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a form-encoded method is refused for a serialized body.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentRejectsAMethodThatIsNotSerialization()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        await Assert.That(() => GeneratedRequestRunner.CreateBodyContent(settings, Item, ItemInfo, BodySerializationMethod.UrlEncoded, false))
            .Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>Verifies each body serialization mode writes the supplied metadata.</summary>
    /// <param name="mode">The request body serialization mode.</param>
    /// <param name="streamBody">Whether the caller asked for a streamed body.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(RequestBodySerializationMode.Default, false)]
    [Arguments(RequestBodySerializationMode.Default, true)]
    [Arguments(RequestBodySerializationMode.Buffered, false)]
    [Arguments(RequestBodySerializationMode.Buffered, true)]
    [Arguments(RequestBodySerializationMode.Streamed, false)]
    public async Task CreateBodyContentWritesInEveryMode(RequestBodySerializationMode mode, bool streamBody)
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);
        settings.RequestBodySerialization = mode;

        using var content = GeneratedRequestRunner.CreateBodyContent(settings, Item, ItemInfo, BodySerializationMethod.Default, streamBody);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a streamed body is pushed to the request when the mode does not already buffer it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentStreamsTheBodyWhenAsked()
    {
        var settings = RefitSettings.ForJsonContext(InventoryJsonContext.Default);

        using var content = GeneratedRequestRunner.CreateBodyContent(settings, Item, ItemInfo, BodySerializationMethod.Default, true);

        await Assert.That(content).IsNotTypeOf<StringContent>();
        await Assert.That(content.GetType().Name).IsEqualTo("PushStreamContent");
    }

    /// <summary>Verifies a serializer without the capability is refused with a message that names the interface.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CreateBodyContentRejectsASerializerWithoutTheCapability()
    {
        RefitSettings settings = new(new NonMetadataContentSerializer());

        var failure = await Assert.That(() => GeneratedRequestRunner.CreateBodyContent(settings, Item, ItemInfo, BodySerializationMethod.Default, false))
            .Throws<InvalidOperationException>();

        await Assert.That(failure!.Message).Contains(nameof(IJsonTypeInfoContentSerializer));
    }

    /// <summary>Verifies metadata stored on a request is found by the type it describes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestJsonTypeInfoRoundTripsThroughTheRequest()
    {
        using HttpRequestMessage request = new();

        GeneratedRequestRunner.SetRequestJsonTypeInfo(request, ItemInfo);

        await Assert.That(GeneratedRequestRunner.GetRequestJsonTypeInfo<InventoryItem>(request)).IsSameReferenceAs(ItemInfo);
        await Assert.That(GeneratedRequestRunner.GetRequestJsonTypeInfo<StockNote>(request)).IsNull();
    }

    /// <summary>Verifies a request that was given no metadata reports none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task RequestWithoutMetadataReportsNone()
    {
        using HttpRequestMessage request = new();

        GeneratedRequestRunner.SetRequestJsonTypeInfo<InventoryItem>(request, null);

        await Assert.That(GeneratedRequestRunner.GetRequestJsonTypeInfo<InventoryItem>(request)).IsNull();
    }

    /// <summary>A content serializer that does not implement <see cref="IJsonTypeInfoContentSerializer"/>.</summary>
    private sealed class NonMetadataContentSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(System.Reflection.PropertyInfo propertyInfo) => null;
    }
}
