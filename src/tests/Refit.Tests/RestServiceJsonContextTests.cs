// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;

namespace Refit.Tests;

/// <summary>Verifies generated clients created on a source-generated JSON context.</summary>
public class RestServiceJsonContextTests
{
    /// <summary>The base address of the clients under test.</summary>
    private const string BaseAddress = "http://inventory.test/";

    /// <summary>The identifier of the stock line the replies describe.</summary>
    private const int ItemId = 7;

    /// <summary>A stock line as a web API writes it.</summary>
    private const string ItemJson = "{\"id\":7,\"displayName\":\"Nut\",\"onHand\":12}";

    /// <summary>The same stock line with snake-case names.</summary>
    private const string SnakeItemJson = "{\"id\":7,\"display_name\":\"Nut\",\"on_hand\":12}";

    /// <summary>A note as a web API writes it.</summary>
    private const string NoteJson = "{\"text\":\"low stock\"}";

    /// <summary>The stock line the replies describe.</summary>
    private static readonly InventoryItem Item = new(ItemId, "Nut", 12);

    /// <summary>Verifies the client reads a reply with the naming the context declares.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedReadsAReplyWithTheContextNaming()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(ItemJson), new(BaseAddress));

        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default);

        await Assert.That(await api.GetItem(ItemId)).IsEqualTo(Item);
    }

    /// <summary>Verifies the client writes a request body with the naming the context declares.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedWritesARequestBodyWithTheContextNaming()
    {
        var handler = new JsonReplyHandler(ItemJson);
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));

        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default);
        _ = await api.AddItem(Item);

        await Assert.That(handler.RequestBody).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies a type the context does not describe fails the call instead of using reflection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedRefusesAReplyTypeTheContextDoesNotDescribe()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(NoteJson), new(BaseAddress));
        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default);

        var failure = await Assert.That(async () => _ = await api.GetNote()).Throws<Exception>();

        await Assert.That(failure!.GetBaseException()).IsTypeOf<NotSupportedException>();
    }

    /// <summary>Verifies the reflection fallback lets the call read a type the context does not describe.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedWithReflectionFallbackReadsAReplyTypeTheContextDoesNotDescribe()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(NoteJson), new(BaseAddress));

        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default, true);

        await Assert.That(await api.GetNote()).IsEqualTo(new("low stock"));
    }

    /// <summary>Verifies settings passed with a context keep their naming and gain the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedWithSettingsWritesWithTheNamingOfTheSettings()
    {
        var handler = new JsonReplyHandler(SnakeItemJson);
        using var client = HttpClientTestFactory.Create(handler, new(BaseAddress));
        var settings = RefitSettings.SnakeCase();

        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default, settings);
        var read = await api.AddItem(Item);

        var serializer = await Assert.That(settings.ContentSerializer).IsTypeOf<SystemTextJsonContentSerializer>();
        await Assert.That(handler.RequestBody).IsEqualTo(SnakeItemJson);
        await Assert.That(read).IsEqualTo(Item);
        await Assert.That(serializer!.SerializerOptions.TypeInfoResolverChain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
    }

    /// <summary>Verifies settings passed with a context can allow the reflection fallback.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedWithSettingsAndReflectionFallbackReadsAnUndescribedType()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(NoteJson), new(BaseAddress));
        var settings = new RefitSettings();

        var api = RestService.ForGenerated<IInventoryApi>(client, InventoryJsonContext.Default, settings, true);

        await Assert.That(await api.GetNote()).IsEqualTo(new("low stock"));
    }

    /// <summary>Verifies the base-address overloads create a client on the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedCreatesAClientFromABaseAddress()
    {
        var plain = RestService.ForGenerated<IInventoryApi>(BaseAddress, InventoryJsonContext.Default);
        var withFallback = RestService.ForGenerated<IInventoryApi>(BaseAddress, InventoryJsonContext.Default, true);
        RefitSettings settings = new();
        RefitSettings fallbackSettings = new();
        var withSettings = RestService.ForGenerated<IInventoryApi>(BaseAddress, InventoryJsonContext.Default, settings);
        var withSettingsAndFallback = RestService.ForGenerated<IInventoryApi>(
            BaseAddress,
            InventoryJsonContext.Default,
            fallbackSettings,
            true);

        await Assert.That(plain).IsNotNull();
        await Assert.That(withFallback).IsNotNull();
        await Assert.That(withSettings).IsNotNull();
        await Assert.That(withSettingsAndFallback).IsNotNull();
    }

    /// <summary>Verifies the base-address overload reads through the handler the settings supply.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedFromABaseAddressUsesTheSettingsHandler()
    {
        var settings = new RefitSettings { HttpMessageHandlerFactory = static () => new JsonReplyHandler(ItemJson) };

        var api = RestService.ForGenerated<IInventoryApi>(BaseAddress, InventoryJsonContext.Default, settings);

        await Assert.That(await api.GetItem(ItemId)).IsEqualTo(Item);
    }

    /// <summary>Verifies the overloads reject missing arguments.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForGeneratedRejectsMissingArguments()
    {
        using var client = HttpClientTestFactory.Create(new JsonReplyHandler(ItemJson), new(BaseAddress));
        var context = InventoryJsonContext.Default;

        await Assert.That(() => RestService.ForGenerated<IInventoryApi>(client, (JsonSerializerContext)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => RestService.ForGenerated<IInventoryApi>(client, context, (RefitSettings)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => RestService.ForGenerated<IInventoryApi>(client, context, null!, true)).Throws<ArgumentNullException>();
        await Assert.That(() => RestService.ForGenerated<IInventoryApi>(BaseAddress, context, (RefitSettings)null!)).Throws<ArgumentNullException>();
        await Assert.That(() => RestService.ForGenerated<IInventoryApi>(BaseAddress, context, null!, true)).Throws<ArgumentNullException>();
    }
}
