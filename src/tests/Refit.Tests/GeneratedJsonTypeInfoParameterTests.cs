// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>
/// Verifies generated clients whose methods take <see cref="JsonTypeInfo{T}"/> metadata as a parameter. The settings use
/// snake-case reflection-based JSON, so a camelCase reply or body only round-trips when the explicit metadata is used.
/// </summary>
public class GeneratedJsonTypeInfoParameterTests
{
    /// <summary>The base address of the clients under test.</summary>
    private const string BaseAddress = "http://inventory.test/";

    /// <summary>The identifier of the stock line the replies describe.</summary>
    private const int ItemId = 7;

    /// <summary>The number of units the stock line holds.</summary>
    private const int OnHand = 12;

    /// <summary>A stock line as a web API writes it.</summary>
    private const string ItemJson = "{\"id\":7,\"displayName\":\"Nut\",\"onHand\":12}";

    /// <summary>A second stock line as a web API writes it.</summary>
    private const string SecondItemJson = "{\"id\":8,\"displayName\":\"Bolt\",\"onHand\":3}";

    /// <summary>A note as a web API writes it.</summary>
    private const string NoteJson = "{\"text\":\"low stock\"}";

    /// <summary>The stock line the replies describe.</summary>
    private static readonly InventoryItem Item = new(ItemId, "Nut", OnHand);

    /// <summary>Verifies a reply is read with the metadata the caller passes, not the serializer's own naming.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReplyIsReadWithTheSuppliedMetadata()
    {
        var api = CreateClient(new JsonReplyHandler(ItemJson));

        var read = await api.GetItem(ItemId, InventoryJsonContext.Default.InventoryItem);

        await Assert.That(read).IsEqualTo(Item);
    }

    /// <summary>Verifies the same reply is misread when no metadata is passed, so the tests above cannot pass by accident.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ReplyIsMisreadWithTheSerializersOwnNamingWhenNoMetadataIsPassed()
    {
        var handler = new JsonReplyHandler(ItemJson);
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IInventoryApi>(client, RefitSettings.SnakeCase());

        var read = await api.GetItem(ItemId);

        await Assert.That(read.DisplayName).IsNull();
    }

    /// <summary>Verifies a request body is written with the metadata the caller passes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyIsWrittenWithTheSuppliedMetadata()
    {
        var handler = new JsonReplyHandler(ItemJson);
        var api = CreateClient(handler);

        var read = await api.AddItem(Item, InventoryJsonContext.Default.InventoryItem);

        await Assert.That(handler.RequestBody).IsEqualTo(ItemJson);
        await Assert.That(read).IsEqualTo(Item);
    }

    /// <summary>Verifies a buffered body is written with the metadata the caller passes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BufferedBodyIsWrittenWithTheSuppliedMetadata()
    {
        var handler = new JsonReplyHandler(ItemJson);
        var settings = RefitSettings.SnakeCase();
        settings.RequestBodySerialization = RequestBodySerializationMode.Buffered;
        var api = CreateClient(handler, settings);

        _ = await api.AddItem(Item, InventoryJsonContext.Default.InventoryItem);

        await Assert.That(handler.RequestBody).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a streamed body is written with the metadata the caller passes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task StreamedBodyIsWrittenWithTheSuppliedMetadata()
    {
        var handler = new JsonReplyHandler(ItemJson);
        var settings = RefitSettings.SnakeCase();
        settings.RequestBodySerialization = RequestBodySerializationMode.Streamed;
        var api = CreateClient(handler, settings);

        _ = await api.AddItem(Item, InventoryJsonContext.Default.InventoryItem);

        await Assert.That(handler.RequestBody).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies one call writes the body with one piece of metadata and reads the reply with another.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyAndReplyUseTheirOwnMetadata()
    {
        var handler = new JsonReplyHandler(NoteJson);
        var api = CreateClient(handler);

        var note = await api.AddItemNote(
            Item,
            InventoryJsonContext.Default.InventoryItem,
            StockNoteJsonContext.Default.StockNote);

        await Assert.That(handler.RequestBody).IsEqualTo(ItemJson);
        await Assert.That(note).IsEqualTo(new("low stock"));
    }

    /// <summary>Verifies an API response body is read with the metadata the caller passes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ApiResponseBodyIsReadWithTheSuppliedMetadata()
    {
        var api = CreateClient(new JsonReplyHandler(ItemJson));

        using var response = await api.GetItemResponse(ItemId, InventoryJsonContext.Default.InventoryItem, CancellationToken.None);

        await Assert.That(response.Content).IsEqualTo(Item);
    }

    /// <summary>Verifies a collection reply is read with metadata for the collection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task CollectionReplyIsReadWithTheSuppliedMetadata()
    {
        var api = CreateClient(new JsonReplyHandler($"[{ItemJson},{SecondItemJson}]"));

        var read = await api.ListItems(InventoryJsonContext.Default.ListInventoryItem);

        await Assert.That(read.Select(static item => item.DisplayName).ToArray()).IsEquivalentTo(["Nut", "Bolt"]);
    }

    /// <summary>Verifies each streamed element is read with the metadata the caller passes.</summary>
    /// <param name="mediaType">The media type that selects the streaming format.</param>
    /// <param name="body">The streamed text.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    [Arguments("application/json", $"[{ItemJson},{SecondItemJson}]")]
    [Arguments("application/x-ndjson", $"{ItemJson}\n{SecondItemJson}\n")]
    [Arguments("text/event-stream", $"data: {ItemJson}\n\ndata: {SecondItemJson}\n\n")]
    public async Task StreamedElementsAreReadWithTheSuppliedMetadata(string mediaType, string body)
    {
        var api = CreateClient(new JsonReplyHandler(body, mediaType));
        List<string> names = [];

        await foreach (var item in api.StreamItems(InventoryJsonContext.Default.InventoryItem))
        {
            names.Add(item.DisplayName);
        }

        await Assert.That(names).IsEquivalentTo(["Nut", "Bolt"]);
    }

    /// <summary>Verifies a cold observable reads its reply with the metadata the caller passes.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ObservableReplyIsReadWithTheSuppliedMetadata()
    {
        var api = CreateClient(new JsonReplyHandler(ItemJson));
        var completion = new TaskCompletionSource<InventoryItem>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = api.WatchItem(ItemId, InventoryJsonContext.Default.InventoryItem)
            .Subscribe(new ValueObserver(completion));

        await Assert.That(await completion.Task).IsEqualTo(Item);
    }

    /// <summary>Verifies a serializer without the metadata capability fails the call with a message that names the fix.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task SerializerWithoutTheCapabilityFailsTheCall()
    {
        var handler = new JsonReplyHandler(ItemJson);
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var api = RestService.ForGenerated<IInventoryTypeInfoApi>(client, CreatePlainSettings());

        var failure = await Assert.That(async () => _ = await api.GetItem(ItemId, InventoryJsonContext.Default.InventoryItem))
            .Throws<Exception>();

        await Assert.That(failure!.GetBaseException()).IsTypeOf<InvalidOperationException>();
        await Assert.That(failure.GetBaseException().Message).Contains("IJsonTypeInfoContentSerializer");
    }

    /// <summary>Verifies a body that cannot use the metadata fails when the serializer lacks the capability.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task BodyWithoutTheCapabilityFailsTheCall()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(ItemJson), new(BaseAddress));
        var api = RestService.ForGenerated<IInventoryTypeInfoApi>(client, CreatePlainSettings());

        var failure = await Assert.That(async () => _ = await api.AddItem(Item, InventoryJsonContext.Default.InventoryItem))
            .Throws<Exception>();

        await Assert.That(failure!.GetBaseException()).IsTypeOf<InvalidOperationException>();
    }

    /// <summary>Creates a client whose settings write and read snake-case reflection-based JSON.</summary>
    /// <param name="handler">The handler that answers the calls.</param>
    /// <returns>The generated client.</returns>
    private static IInventoryTypeInfoApi CreateClient(HttpMessageHandler handler) =>
        CreateClient(handler, RefitSettings.SnakeCase());

    /// <summary>Creates a client on the supplied settings.</summary>
    /// <param name="handler">The handler that answers the calls.</param>
    /// <param name="settings">The settings the client uses.</param>
    /// <returns>The generated client.</returns>
    private static IInventoryTypeInfoApi CreateClient(HttpMessageHandler handler, RefitSettings settings) =>
        RestService.ForGenerated<IInventoryTypeInfoApi>(HttpClientTestFactory.Create(handler, new(BaseAddress)), settings);

    /// <summary>Creates settings whose serializer does not implement <see cref="IJsonTypeInfoContentSerializer"/>.</summary>
    /// <returns>The settings.</returns>
    private static RefitSettings CreatePlainSettings() => new(new PlainContentSerializer());

    /// <summary>A content serializer that does not implement <see cref="IJsonTypeInfoContentSerializer"/>.</summary>
    private sealed class PlainContentSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => null;
    }

    /// <summary>An observer that completes a task with the first value it receives.</summary>
    /// <param name="completion">The task source to complete.</param>
    private sealed class ValueObserver(TaskCompletionSource<InventoryItem> completion) : IObserver<InventoryItem>
    {
        /// <inheritdoc/>
        public void OnCompleted()
        {
        }

        /// <inheritdoc/>
        public void OnError(Exception error) => completion.TrySetException(error);

        /// <inheritdoc/>
        public void OnNext(InventoryItem value) => completion.TrySetResult(value);
    }
}
