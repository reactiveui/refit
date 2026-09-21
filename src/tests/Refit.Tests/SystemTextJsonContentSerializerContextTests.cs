// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies how a source-generated context is registered on <see cref="SystemTextJsonContentSerializer"/>.</summary>
public class SystemTextJsonContentSerializerContextTests
{
    /// <summary>The JSON the web-default inventory context writes for <see cref="Item"/>.</summary>
    private const string ItemJson = "{\"id\":1,\"displayName\":\"Bolt\",\"onHand\":4}";

    /// <summary>The JSON the snake-case options write for <see cref="Item"/>.</summary>
    private const string SnakeItemJson = "{\"id\":1,\"display_name\":\"Bolt\",\"on_hand\":4}";

    /// <summary>The JSON a note is written as.</summary>
    private const string NoteJson = "{\"text\":\"low\"}";

    /// <summary>The JSON a warehouse location is written as when it is registered as a polymorphic type.</summary>
    private const string WarehouseJson = "{\"$type\":\"warehouse\",\"Aisle\":4}";

    /// <summary>The aisle of the warehouse location written by the polymorphic test.</summary>
    private const int AisleNumber = 4;

    /// <summary>The number of resolvers in a chain made of one existing resolver and the added context.</summary>
    private const int ExpectedResolverCount = 2;

    /// <summary>The item written and read by the tests.</summary>
    private static readonly InventoryItem Item = new(1, "Bolt", 4);

    /// <summary>Options that write snake-case names.</summary>
    private static readonly JsonSerializerOptions SnakeOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>Web-default options whose only resolver is the note context.</summary>
    private static readonly JsonSerializerOptions NoteOptions = new(JsonSerializerDefaults.Web) { TypeInfoResolver = StockNoteJsonContext.Default };

    /// <summary>Verifies a serializer made for a context runs on the context's own options.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextUsesTheContextOptions()
    {
        var serializer = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default);

        await Assert.That(serializer.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
    }

    /// <summary>Verifies a serializer made for a context writes and reads with the naming the context declares.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextWritesAndReadsWithTheContextNaming()
    {
        var serializer = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default);

        using var content = serializer.ToHttpContent(Item);
        var written = await content.ReadAsStringAsync();
        var read = await serializer.FromHttpContentAsync<InventoryItem>(content);

        await Assert.That(written).IsEqualTo(ItemJson);
        await Assert.That(read).IsEqualTo(Item);
    }

    /// <summary>Verifies a type the context does not describe fails instead of falling back to reflection.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextRejectsATypeTheContextDoesNotDescribe()
    {
        var serializer = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default);

        await Assert.That(() => serializer.ToHttpContent(new StockNote("low"))).Throws<NotSupportedException>();
    }

    /// <summary>Verifies refusing the reflection fallback is the same as not mentioning it.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextWithoutReflectionFallbackUsesTheContextOptions()
    {
        var serializer = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default, false);

        await Assert.That(serializer.SerializerOptions).IsSameReferenceAs(InventoryJsonContext.Default.Options);
    }

    /// <summary>Verifies the reflection fallback describes types the context does not and keeps the context first.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextWithReflectionFallbackDescribesTypesTheContextDoesNot()
    {
        var serializer = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default, true);
        var chain = serializer.SerializerOptions.TypeInfoResolverChain;

        using var noteContent = serializer.ToHttpContent(new StockNote("low"));
        using var itemContent = serializer.ToHttpContent(Item);

        await Assert.That(await noteContent.ReadAsStringAsync()).IsEqualTo(NoteJson);
        await Assert.That(await itemContent.ReadAsStringAsync()).IsEqualTo(ItemJson);
        await Assert.That(chain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
        await Assert.That(chain[^1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies the static factories reject a missing context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task ForContextRejectsANullContext()
    {
        await Assert.That(static () => SystemTextJsonContentSerializer.ForContext(null!)).Throws<ArgumentNullException>();
        await Assert.That(static () => SystemTextJsonContentSerializer.ForContext(null!, true)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies adding a context keeps the naming policy and converters of the serializer it is added to.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextKeepsTheNamingAndConvertersOfTheExistingOptions()
    {
        var original = new SystemTextJsonContentSerializer();

        var composed = original.WithContext(InventoryJsonContext.Default);

        await Assert.That(composed).IsNotSameReferenceAs(original);
        await Assert.That(composed.SerializerOptions.PropertyNamingPolicy).IsSameReferenceAs(original.SerializerOptions.PropertyNamingPolicy);
        await Assert.That(composed.SerializerOptions.Converters.Count).IsEqualTo(original.SerializerOptions.Converters.Count);
        await Assert.That(composed.SerializerOptions.TypeInfoResolverChain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
    }

    /// <summary>Verifies the naming policy of the existing options wins over the naming the context declares.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextWritesWithTheNamingOfTheExistingOptions()
    {
        var snake = new SystemTextJsonContentSerializer(SnakeOptions);

        using var content = snake.WithContext(InventoryJsonContext.Default).ToHttpContent(Item);

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(SnakeItemJson);
    }

    /// <summary>Verifies adding a context leaves the original serializer's options untouched.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextDoesNotChangeTheOriginalOptions()
    {
        var original = new SystemTextJsonContentSerializer();

        _ = original.WithContext(InventoryJsonContext.Default);

        await Assert.That(original.SerializerOptions.TypeInfoResolver).IsNull();
        await Assert.That(original.SerializerOptions.TypeInfoResolverChain.Count).IsEqualTo(0);
    }

    /// <summary>Verifies resolvers already registered stay ahead of the added context and keep their order.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextKeepsExistingResolversFirst()
    {
        var existing = new SystemTextJsonContentSerializer(NoteOptions);

        var composed = existing.WithContext(InventoryJsonContext.Default);
        var chain = composed.SerializerOptions.TypeInfoResolverChain;
        using var note = composed.ToHttpContent(new StockNote("low"));
        using var item = composed.ToHttpContent(Item);

        await Assert.That(chain.Count).IsEqualTo(ExpectedResolverCount);
        await Assert.That(chain[0]).IsSameReferenceAs(StockNoteJsonContext.Default);
        await Assert.That(chain[1]).IsSameReferenceAs(InventoryJsonContext.Default);
        await Assert.That(await note.ReadAsStringAsync()).IsEqualTo(NoteJson);
        await Assert.That(await item.ReadAsStringAsync()).IsEqualTo(ItemJson);
    }

    /// <summary>Verifies adding a context that is already registered returns the same serializer.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextIsIdempotent()
    {
        var composed = new SystemTextJsonContentSerializer().WithContext(InventoryJsonContext.Default);
        var forContext = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default);

        await Assert.That(composed.WithContext(InventoryJsonContext.Default)).IsSameReferenceAs(composed);
        await Assert.That(forContext.WithContext(InventoryJsonContext.Default)).IsSameReferenceAs(forContext);
    }

    /// <summary>Verifies a reflection resolver is removed when the fallback is refused and its contract modifiers move onto the context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextRemovesAReflectionResolverAndCarriesItsModifiers()
    {
        var reflective = new DefaultJsonTypeInfoResolver();
        reflective.Modifiers.Add(static typeInfo =>
        {
            if (typeInfo.Type == typeof(InventoryItem))
            {
                typeInfo.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            }
        });
        var existing = new SystemTextJsonContentSerializer(new JsonSerializerOptions { TypeInfoResolver = reflective });

        var composed = existing.WithContext(InventoryJsonContext.Default);
        var options = composed.SerializerOptions;

        await Assert.That(options.TypeInfoResolverChain.Any(static resolver => resolver is DefaultJsonTypeInfoResolver)).IsFalse();
        await Assert.That(SystemTextJsonContentSerializer.GetJsonTypeInfo<InventoryItem>(options).UnmappedMemberHandling).IsEqualTo(JsonUnmappedMemberHandling.Disallow);
        await Assert.That(() => SystemTextJsonContentSerializer.GetJsonTypeInfo<StockNote>(options)).Throws<NotSupportedException>();
    }

    /// <summary>Verifies a polymorphic registration made by a reflection resolver's contract modifier still applies to the context's types.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextKeepsPolymorphicRegistrationsMadeByAContractModifier()
    {
        var reflective = new DefaultJsonTypeInfoResolver();
        reflective.Modifiers.Add(static typeInfo =>
        {
            if (typeInfo.Type == typeof(StockLocation))
            {
                typeInfo.PolymorphismOptions = new() { DerivedTypes = { new(typeof(WarehouseLocation), "warehouse") } };
            }
        });
        var existing = new SystemTextJsonContentSerializer(new JsonSerializerOptions { TypeInfoResolver = reflective });

        using var content = existing.WithContext(StockLocationJsonContext.Default).ToHttpContent<StockLocation>(new WarehouseLocation(AisleNumber));

        await Assert.That(await content.ReadAsStringAsync()).IsEqualTo(WarehouseJson);
    }

    /// <summary>Verifies an existing reflection resolver stays, after the context, when the fallback is allowed.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextKeepsAnExistingReflectionResolverAfterTheContextWhenTheFallbackIsAllowed()
    {
        var reflective = new DefaultJsonTypeInfoResolver();
        var existing = new SystemTextJsonContentSerializer(new JsonSerializerOptions { TypeInfoResolver = reflective });

        var chain = existing.WithContext(InventoryJsonContext.Default, true).SerializerOptions.TypeInfoResolverChain;

        await Assert.That(chain.Count).IsEqualTo(ExpectedResolverCount);
        await Assert.That(chain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
        await Assert.That(chain[1]).IsSameReferenceAs(reflective);
    }

    /// <summary>Verifies the fallback adds a reflection resolver when the options carry none.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextAddsAReflectionResolverWhenTheFallbackIsAllowed()
    {
        var chain = new SystemTextJsonContentSerializer()
            .WithContext(InventoryJsonContext.Default, true)
            .SerializerOptions.TypeInfoResolverChain;

        await Assert.That(chain.Count).IsEqualTo(ExpectedResolverCount);
        await Assert.That(chain[0]).IsSameReferenceAs(InventoryJsonContext.Default);
        await Assert.That(chain[1]).IsTypeOf<DefaultJsonTypeInfoResolver>();
    }

    /// <summary>Verifies a serializer that already allows reflection is rebuilt when the fallback is refused.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextRebuildsTheOptionsWhenTheFallbackChoiceChanges()
    {
        var allowed = SystemTextJsonContentSerializer.ForContext(InventoryJsonContext.Default, true);

        var refused = allowed.WithContext(InventoryJsonContext.Default, false);

        await Assert.That(refused).IsNotSameReferenceAs(allowed);
        await Assert.That(refused.SerializerOptions.TypeInfoResolverChain.Count).IsEqualTo(1);
        await Assert.That(allowed.WithContext(InventoryJsonContext.Default, true)).IsSameReferenceAs(allowed);
    }

    /// <summary>Verifies adding a context rejects a missing context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Test]
    public async Task WithContextRejectsANullContext()
    {
        var serializer = new SystemTextJsonContentSerializer();

        await Assert.That(() => serializer.WithContext(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => serializer.WithContext(null!, true)).Throws<ArgumentNullException>();
    }
}
