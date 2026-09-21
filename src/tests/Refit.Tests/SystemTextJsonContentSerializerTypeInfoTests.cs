// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies the overloads of <see cref="SystemTextJsonContentSerializer"/> that take explicit <see cref="JsonTypeInfo{T}"/> metadata.</summary>
public class SystemTextJsonContentSerializerTypeInfoTests
{
    /// <summary>The JSON the web-default inventory context writes for <see cref="Item"/>.</summary>
    private const string ItemJson = "{\"id\":1,\"displayName\":\"Bolt\",\"onHand\":4}";

    /// <summary>A second stock line in JSON.</summary>
    private const string SecondItemJson = "{\"id\":2,\"displayName\":\"Nut\",\"onHand\":9}";

    /// <summary>The item written and read by the tests.</summary>
    private static readonly InventoryItem Item = new(1, "Bolt", 4);

    /// <summary>The second item read by the streaming tests.</summary>
    private static readonly InventoryItem SecondItem = new(2, "Nut", 9);

    /// <summary>Gets the explicit metadata for <see cref="InventoryItem"/>.</summary>
    private static JsonTypeInfo<InventoryItem> ItemInfo => InventoryJsonContext.Default.InventoryItem;

    /// <summary>Gets a serializer that has no resolver of its own, so only the supplied metadata can describe a type.</summary>
    private static SystemTextJsonContentSerializer Serializer => new();

    /// <summary>Verifies a value is written with the supplied metadata even when the serializer holds none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToHttpContentWritesWithTheSuppliedMetadata()
    {
        using var content = Serializer.ToHttpContent(Item, ItemInfo);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
        await Assert.That(content.Headers.ContentType!.MediaType).IsEqualTo("application/json");
    }

    /// <summary>Verifies the buffered writer produces UTF-8 JSON held in a byte array.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToHttpContentSynchronousWritesABufferedBody()
    {
        using var content = Serializer.ToHttpContentSynchronous(Item, ItemInfo);

        await Assert.That(content).IsTypeOf<ByteArrayContent>();
        await Assert.That(content.Headers.ContentType!.CharSet).IsEqualTo("utf-8");
        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies the streaming writer produces the same JSON when the request sends it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ToStreamingHttpContentWritesTheBodyWhenSent()
    {
        using var content = Serializer.ToStreamingHttpContent(Item, ItemInfo);

        await Assert.That(content.Headers.ContentType!.CharSet).IsEqualTo("utf-8");
        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a body is read with the supplied metadata.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task FromHttpContentReadsWithTheSuppliedMetadata()
    {
        using var content = new StringContent(ItemJson, Encoding.UTF8, "application/json");

        var read = await Serializer.FromHttpContentAsync(content, ItemInfo, CancellationToken.None);

        await Assert.That(read).IsEqualTo(Item);
    }

    /// <summary>Verifies buffered text is read with the supplied metadata.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task DeserializeFromStringReadsWithTheSuppliedMetadata() =>
        await Assert.That(Serializer.DeserializeFromString(ItemJson, ItemInfo)).IsEqualTo(Item);

    /// <summary>Verifies each streaming format is read with the supplied metadata.</summary>
    /// <param name="format">The framing of the stream.</param>
    /// <param name="body">The streamed text.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments(StreamingContentFormat.JsonArray, $"[{ItemJson},{SecondItemJson}]")]
    [Arguments(StreamingContentFormat.JsonLines, $"{ItemJson}\n{SecondItemJson}\n")]
    [Arguments(StreamingContentFormat.ServerSentEvents, $"data: {ItemJson}\n\ndata: {SecondItemJson}\n\n")]
    public async Task DeserializeStreamReadsWithTheSuppliedMetadata(StreamingContentFormat format, string body)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        List<InventoryItem> read = [];

        await foreach (var item in Serializer.DeserializeStreamAsync(stream, format, ItemInfo, CancellationToken.None))
        {
            read.Add(item!);
        }

        await Assert.That(read).IsEquivalentTo([Item, SecondItem]);
    }

    /// <summary>Verifies the overloads reject missing arguments.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task OverloadsRejectMissingArguments()
    {
        var serializer = Serializer;
        using var content = new StringContent(ItemJson);
        await using var stream = new MemoryStream();

        await Assert.That(() => serializer.ToHttpContent(Item, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.ToHttpContentSynchronous(Item, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.ToStreamingHttpContent(Item, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.DeserializeFromString<InventoryItem>(ItemJson, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.DeserializeFromString(null!, ItemInfo)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.FromHttpContentAsync<InventoryItem>(content, null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
        await Assert.That(() => serializer.FromHttpContentAsync(null!, ItemInfo, CancellationToken.None)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.DeserializeStreamAsync<InventoryItem>(stream, StreamingContentFormat.JsonArray, null!, CancellationToken.None))
            .Throws<ArgumentNullException>();
    }
}
