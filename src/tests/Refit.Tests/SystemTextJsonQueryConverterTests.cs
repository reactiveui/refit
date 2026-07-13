// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Refit.Tests;

/// <summary>Verifies the edge cases of <see cref="SystemTextJsonQueryConverter{T}"/>: null values, an incompatible
/// serializer, getter-less properties, collections and the recursion cap.</summary>
public sealed class SystemTextJsonQueryConverterTests
{
    /// <summary>The relative path the converter appends its query onto.</summary>
    private const string Path = "/x";

    /// <summary>The name of the property whose getter is removed by the resolver modifier.</summary>
    private const string HiddenPropertyName = "Hidden";

    /// <summary>The depth of the self-referential chain used to trip the recursion cap.</summary>
    private const int OverflowDepth = 40;

    /// <summary>Verifies a null value appends nothing.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NullValueAppendsNothing()
    {
        var result = Flatten<StjFilter>(null, StjSettings());

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Verifies flattening throws when the configured serializer is not a System.Text.Json serializer.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task NonSystemTextJsonSerializerThrows()
    {
        var settings = new RefitSettings(new NonJsonSerializer());
        var filter = new StjFilter { Query = "ada" };

        await Assert
            .That(() =>
            {
                var builder = new GeneratedQueryStringBuilder(Path);
                new SystemTextJsonQueryConverter<StjFilter>().Flatten(filter, string.Empty, ref builder, settings);
            })
            .Throws<NotSupportedException>();
    }

    /// <summary>Verifies a property without a getter is skipped while readable ones are emitted.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PropertyWithoutGetterIsSkipped()
    {
        var result = Flatten(new GetterlessProbe { Readable = "r", Hidden = "ignored" }, GetterlessSettings());

        await Assert.That(result).IsEqualTo("/x?Readable=r");
    }

    /// <summary>Verifies a collection property is expanded, omitting null elements under the multi format.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task CollectionPropertyIsExpanded()
    {
        var settings = StjSettings();
        settings.CollectionFormat = CollectionFormat.Multi;

        var result = Flatten(new CollectionProbe { Tags = ["a", null, "b"] }, settings);

        await Assert.That(result).IsEqualTo("/x?Tags=a&Tags=b");
    }

    /// <summary>Verifies the recursion cap stops before appending a property nested beyond the limit.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task RecursionCapStopsAtMaxDepth()
    {
        var result = Flatten(BuildChain(OverflowDepth), StjSettings());

        await Assert.That(result).IsEqualTo(Path);
    }

    /// <summary>Flattens a value with a fresh builder and returns the built relative path.</summary>
    /// <typeparam name="T">The declared parameter type.</typeparam>
    /// <param name="value">The value to flatten, or null.</param>
    /// <param name="settings">The settings supplying the serializer and formatter.</param>
    /// <returns>The built relative path.</returns>
    private static string Flatten<T>(T? value, RefitSettings settings)
    {
        var builder = new GeneratedQueryStringBuilder(Path);
        new SystemTextJsonQueryConverter<T>().Flatten(value!, string.Empty, ref builder, settings);
        return builder.Build();
    }

    /// <summary>Builds settings backed by a reflection-based System.Text.Json serializer.</summary>
    /// <returns>The configured settings.</returns>
    private static RefitSettings StjSettings() => SettingsFor(new DefaultJsonTypeInfoResolver());

    /// <summary>Builds settings whose resolver removes the getter of the <see cref="GetterlessProbe.Hidden"/> property.</summary>
    /// <returns>The configured settings.</returns>
    private static RefitSettings GetterlessSettings()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(static typeInfo =>
        {
            if (typeInfo.Type != typeof(GetterlessProbe))
            {
                return;
            }

            foreach (var property in typeInfo.Properties)
            {
                if (property.Name == HiddenPropertyName)
                {
                    property.Get = null;
                }
            }
        });

        return SettingsFor(resolver);
    }

    /// <summary>Builds settings backed by a System.Text.Json serializer using the given type-info resolver.</summary>
    /// <param name="resolver">The type-info resolver to use.</param>
    /// <returns>The configured settings.</returns>
    [SuppressMessage(
        "Performance",
        "PSH1416:Cache the serializer options instead of building them per call",
        Justification = "The options carry the per-call type-info resolver, so a shared cached instance would leak one caller's resolver into another.")]
    private static RefitSettings SettingsFor(IJsonTypeInfoResolver resolver) =>
        new()
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions { TypeInfoResolver = resolver })
        };

    /// <summary>Builds a self-referential chain whose deepest node carries a leaf value.</summary>
    /// <param name="depth">The number of nested nodes to create.</param>
    /// <returns>The head of the chain.</returns>
    private static RecursiveNode BuildChain(int depth)
    {
        var node = new RecursiveNode { Leaf = "deep" };
        for (var i = 0; i < depth; i++)
        {
            node = new RecursiveNode { Next = node };
        }

        return node;
    }

    /// <summary>A probe whose <see cref="Hidden"/> property has its getter removed by a resolver modifier.</summary>
    private sealed class GetterlessProbe
    {
        /// <summary>Gets or sets the readable property.</summary>
        public string? Readable { get; set; }

        /// <summary>Gets or sets the property whose getter is removed before flattening.</summary>
        public string? Hidden { get; set; }
    }

    /// <summary>A probe with a collection property flattened by the converter.</summary>
    private sealed class CollectionProbe
    {
        /// <summary>Gets or sets the collection of tags.</summary>
        [SuppressMessage(
            "Usage",
            "CA2227:Collection properties should be read only",
            Justification = "The converter walks the deserialized shape; a settable collection matches System.Text.Json models.")]
        public List<string?>? Tags { get; set; }
    }

    /// <summary>A self-referential node used to exercise the converter's recursion cap.</summary>
    private sealed class RecursiveNode
    {
        /// <summary>Gets or sets the next node in the chain.</summary>
        public RecursiveNode? Next { get; set; }

        /// <summary>Gets or sets the leaf value.</summary>
        public string? Leaf { get; set; }
    }

    /// <summary>A minimal non-System.Text.Json serializer used to trip the converter's serializer guard.</summary>
    private sealed class NonJsonSerializer : IHttpContentSerializer
    {
        /// <inheritdoc/>
        public HttpContent ToHttpContent<T>(T item) => new StringContent(string.Empty);

        /// <inheritdoc/>
        [SuppressMessage(
            "Major Code Smell",
            "S4018:Generic methods should provide type parameters",
            Justification = "The method implements Refit's published serializer interface.")]
        public Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default) =>
            Task.FromResult<T?>(default);

        /// <inheritdoc/>
        public string? GetFieldNameForProperty(PropertyInfo propertyInfo) => propertyInfo.Name;
    }
}
